using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Admin_Tasks.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsDirectory;
        private readonly string _settingsFilePath;
        private readonly ILogger<SettingsService>? _logger;
        private Dictionary<string, object> _settings;
        private readonly object _lock = new object();
        
        public SettingsService(ILogger<SettingsService>? logger = null)
        {
            _logger = logger;
            
            // AppData-Verzeichnis für die Anwendung erstellen
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _settingsDirectory = Path.Combine(appDataPath, "Administrator", "Tasks");
            _settingsFilePath = Path.Combine(_settingsDirectory, "settings.json");
            
            _settings = new Dictionary<string, object>();
            
            // Verzeichnis erstellen falls es nicht existiert
            EnsureDirectoryExists();
        }
        
        public async Task InitializeAsync()
        {
            await LoadSettingsAsync();
        }

        private void EnsureDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_settingsDirectory))
                {
                    Directory.CreateDirectory(_settingsDirectory);
                    _logger?.LogInformation($"Settings-Verzeichnis erstellt: {_settingsDirectory}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Fehler beim Erstellen des Settings-Verzeichnisses: {_settingsDirectory}");
            }
        }
        
        public async Task<T> GetSettingAsync<T>(string key, T defaultValue = default(T))
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key darf nicht null oder leer sein", nameof(key));
            
            try
            {
                lock (_lock)
                {
                    if (_settings.TryGetValue(key, out var value))
                    {
                        if (value is JsonElement jsonElement)
                        {
                            return DeserializeJsonElement<T>(jsonElement);
                        }
                        
                        if (value is T directValue)
                        {
                            return directValue;
                        }
                        
                        // Versuche Konvertierung
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                }
                
                _logger?.LogDebug($"Einstellung '{key}' nicht gefunden, verwende Standardwert: {defaultValue}");
                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Fehler beim Laden der Einstellung '{key}'");
                return defaultValue;
            }
        }
        
        public async Task SetSettingAsync<T>(string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key darf nicht null oder leer sein", nameof(key));
            
            try
            {
                lock (_lock)
                {
                    _settings[key] = value;
                }
                
                await SaveSettingsAsync();
                _logger?.LogDebug($"Einstellung '{key}' gespeichert: {value}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Fehler beim Speichern der Einstellung '{key}'");
                throw;
            }
        }
        
        public async Task RemoveSettingAsync(string key)
        {
            if (_settings.ContainsKey(key))
            {
                _settings.Remove(key);
                await SaveSettingsAsync();
            }
        }
        
        public async Task<bool> HasSettingAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;
            
            lock (_lock)
            {
                return _settings.ContainsKey(key);
            }
        }
        
        public async Task ClearAllSettingsAsync()
        {
            try
            {
                lock (_lock)
                {
                    _settings.Clear();
                }
                
                await SaveSettingsAsync();
                _logger?.LogInformation("Alle Einstellungen gelöscht");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fehler beim Löschen aller Einstellungen");
                throw;
            }
        }
        
        private async Task LoadSettingsAsync()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    _logger?.LogDebug($"Settings-Datei nicht gefunden: {_settingsFilePath}");
                    return;
                }
                
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }
                
                var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (loadedSettings != null)
                {
                    lock (_lock)
                    {
                        _settings.Clear();
                        foreach (var kvp in loadedSettings)
                        {
                            _settings[kvp.Key] = kvp.Value;
                        }
                    }
                    
                    _logger?.LogInformation($"Einstellungen geladen: {_settings.Count} Einträge");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Fehler beim Laden der Einstellungen aus {_settingsFilePath}");
            }
        }
        
        public async Task SaveSettingsAsync()
        {
            try
            {
                EnsureDirectoryExists();
                
                Dictionary<string, object> settingsToSave;
                lock (_lock)
                {
                    settingsToSave = new Dictionary<string, object>(_settings);
                }
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = JsonSerializer.Serialize(settingsToSave, options);
                await File.WriteAllTextAsync(_settingsFilePath, json);
                
                _logger?.LogDebug($"Einstellungen gespeichert in: {_settingsFilePath}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Fehler beim Speichern der Einstellungen in {_settingsFilePath}");
                throw;
            }
        }
        
        private T DeserializeJsonElement<T>(JsonElement jsonElement)
        {
            try
            {
                return jsonElement.Deserialize<T>();
            }
            catch
            {
                // Fallback für primitive Typen
                if (typeof(T) == typeof(bool) && jsonElement.ValueKind == JsonValueKind.True || jsonElement.ValueKind == JsonValueKind.False)
                {
                    return (T)(object)jsonElement.GetBoolean();
                }
                if (typeof(T) == typeof(string) && jsonElement.ValueKind == JsonValueKind.String)
                {
                    return (T)(object)jsonElement.GetString();
                }
                if (typeof(T) == typeof(int) && jsonElement.ValueKind == JsonValueKind.Number)
                {
                    return (T)(object)jsonElement.GetInt32();
                }
                
                throw;
            }
        }
    }
}