using System.Text.Json;
using System.IO;
using Admin_Tasks.Models;
using Microsoft.Extensions.Logging;

namespace Admin_Tasks.Services;

/// <summary>
/// Service für die Verwaltung benutzerdefinierter Ordner mit JSON-basierter lokaler Speicherung
/// </summary>
public class CustomFolderService : ICustomFolderService
{
    private readonly ILogger<CustomFolderService> _logger;
    private readonly string _dataFilePath;
    private FolderStructure? _cachedStructure;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CustomFolderService(ILogger<CustomFolderService> logger)
    {
        _logger = logger;
        
        // Pfad: %appdata%\Administrator\Tasks\structure.json
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var adminTasksPath = Path.Combine(appDataPath, "Administrator", "Tasks");
        _dataFilePath = Path.Combine(adminTasksPath, "structure.json");
        
        _logger.LogInformation("CustomFolderService initialisiert. Datenpfad: {DataPath}", _dataFilePath);
    }

    public event EventHandler<FolderStructure>? FolderStructureChanged;

    public string GetDataFilePath() => _dataFilePath;

    public async Task<bool> EnsureDataDirectoryExistsAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(_dataFilePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Datenverzeichnis erstellt: {Directory}", directory);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Erstellen des Datenverzeichnisses");
            return false;
        }
    }

    public async Task<FolderStructure> LoadFolderStructureAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            // Cache verwenden wenn verfügbar
            if (_cachedStructure != null)
                return _cachedStructure;

            await EnsureDataDirectoryExistsAsync();

            if (!File.Exists(_dataFilePath))
            {
                _logger.LogInformation("Keine bestehende Ordnerstruktur gefunden. Erstelle neue Struktur.");
                _cachedStructure = new FolderStructure();
                await SaveFolderStructureInternalAsync(_cachedStructure);
                return _cachedStructure;
            }

            var jsonContent = await File.ReadAllTextAsync(_dataFilePath);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning("JSON-Datei ist leer. Erstelle neue Struktur.");
                _cachedStructure = new FolderStructure();
                return _cachedStructure;
            }

            _cachedStructure = JsonSerializer.Deserialize<FolderStructure>(jsonContent, JsonOptions);
            if (_cachedStructure == null)
            {
                _logger.LogError("Fehler beim Deserialisieren der JSON-Datei. Erstelle neue Struktur.");
                _cachedStructure = new FolderStructure();
            }
            else
            {
                _logger.LogInformation("Ordnerstruktur erfolgreich geladen. {FolderCount} Ordner gefunden.", 
                    _cachedStructure.CustomFolders.Count);
            }

            return _cachedStructure;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden der Ordnerstruktur");
            _cachedStructure = new FolderStructure();
            return _cachedStructure;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveFolderStructureAsync(FolderStructure structure)
    {
        await _fileLock.WaitAsync();
        try
        {
            await SaveFolderStructureInternalAsync(structure);
            _cachedStructure = structure;
            FolderStructureChanged?.Invoke(this, structure);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task SaveFolderStructureInternalAsync(FolderStructure structure)
    {
        try
        {
            await EnsureDataDirectoryExistsAsync();
            
            // Metadaten aktualisieren
            structure.Metadata.LastModified = DateTime.Now;
            structure.Metadata.FolderCount = structure.CustomFolders.Count;

            var jsonContent = JsonSerializer.Serialize(structure, JsonOptions);
            await File.WriteAllTextAsync(_dataFilePath, jsonContent);
            
            _logger.LogDebug("Ordnerstruktur erfolgreich gespeichert. {FolderCount} Ordner.", 
                structure.CustomFolders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Speichern der Ordnerstruktur");
            throw;
        }
    }

    public async Task<CustomFolder?> CreateFolderAsync(string name, string? description = null, string color = "#2563EB")
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Versuch, Ordner mit leerem Namen zu erstellen");
            return null;
        }

        try
        {
            var structure = await LoadFolderStructureAsync();
            
            // Prüfen ob Name bereits existiert
            if (structure.GetFolderByName(name) != null)
            {
                _logger.LogWarning("Ordner mit Name '{Name}' existiert bereits", name);
                return null;
            }

            var folder = new CustomFolder
            {
                Name = name.Trim(),
                Description = description?.Trim(),
                Color = color,
                CreatedAt = DateTime.Now,
                LastModified = DateTime.Now
            };

            if (structure.AddFolder(folder))
            {
                await SaveFolderStructureAsync(structure);
                _logger.LogInformation("Neuer Ordner erstellt: {Name} (ID: {Id})", folder.Name, folder.Id);
                return folder;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Erstellen des Ordners '{Name}'", name);
            return null;
        }
    }

    public async Task<CustomFolder?> CreateCustomFolderAsync(CustomFolder customFolder)
    {
        if (customFolder == null || string.IsNullOrWhiteSpace(customFolder.Name))
        {
            _logger.LogWarning("Versuch, Ordner mit ungültigen Daten zu erstellen");
            return null;
        }

        try
        {
            var structure = await LoadFolderStructureAsync();
            
            // Prüfen ob Name bereits existiert
            if (structure.GetFolderByName(customFolder.Name) != null)
            {
                _logger.LogWarning("Ordner mit Name '{Name}' existiert bereits", customFolder.Name);
                return null;
            }

            // Neue ID generieren und Zeitstempel setzen
            customFolder.Id = Guid.NewGuid();
            customFolder.Name = customFolder.Name.Trim();
            customFolder.Description = customFolder.Description?.Trim();
            customFolder.CreatedAt = DateTime.Now;
            customFolder.LastModified = DateTime.Now;

            if (structure.AddFolder(customFolder))
            {
                await SaveFolderStructureAsync(structure);
                _logger.LogInformation("Neuer benutzerdefinierter Ordner erstellt: {Name} (ID: {Id})", customFolder.Name, customFolder.Id);
                return customFolder;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Erstellen des benutzerdefinierten Ordners '{Name}'", customFolder.Name);
            return null;
        }
    }

    public async Task<bool> DeleteFolderAsync(Guid folderId)
    {
        try
        {
            var structure = await LoadFolderStructureAsync();
            var folder = structure.GetFolder(folderId);
            
            if (folder == null)
            {
                _logger.LogWarning("Ordner mit ID {FolderId} nicht gefunden", folderId);
                return false;
            }

            if (structure.RemoveFolder(folderId))
            {
                await SaveFolderStructureAsync(structure);
                _logger.LogInformation("Ordner gelöscht: {Name} (ID: {Id})", folder.Name, folderId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Löschen des Ordners {FolderId}", folderId);
            return false;
        }
    }

    public async Task<bool> UpdateFolderAsync(CustomFolder folder)
    {
        try
        {
            var structure = await LoadFolderStructureAsync();
            var existingFolder = structure.GetFolder(folder.Id);
            
            if (existingFolder == null)
            {
                _logger.LogWarning("Ordner mit ID {FolderId} nicht gefunden", folder.Id);
                return false;
            }

            // Eigenschaften aktualisieren
            existingFolder.Name = folder.Name;
            existingFolder.Description = folder.Description;
            existingFolder.Color = folder.Color;
            existingFolder.IsExpanded = folder.IsExpanded;
            existingFolder.LastModified = DateTime.Now;

            await SaveFolderStructureAsync(structure);
            _logger.LogInformation("Ordner aktualisiert: {Name} (ID: {Id})", folder.Name, folder.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Aktualisieren des Ordners {FolderId}", folder.Id);
            return false;
        }
    }

    public async Task<bool> AddTaskToFolderAsync(Guid folderId, int taskId)
    {
        try
        {
            var structure = await LoadFolderStructureAsync();
            var folder = structure.GetFolder(folderId);
            
            if (folder == null)
            {
                _logger.LogWarning("Ordner mit ID {FolderId} nicht gefunden", folderId);
                return false;
            }

            if (folder.AddTask(taskId))
            {
                await SaveFolderStructureAsync(structure);
                _logger.LogDebug("Task {TaskId} zu Ordner {FolderName} hinzugefügt", taskId, folder.Name);
                return true;
            }

            return false; // Task bereits im Ordner
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Hinzufügen der Task {TaskId} zu Ordner {FolderId}", taskId, folderId);
            return false;
        }
    }

    public async Task<bool> RemoveTaskFromFolderAsync(Guid folderId, int taskId)
    {
        try
        {
            var structure = await LoadFolderStructureAsync();
            var folder = structure.GetFolder(folderId);
            
            if (folder == null)
            {
                _logger.LogWarning("Ordner mit ID {FolderId} nicht gefunden", folderId);
                return false;
            }

            if (folder.RemoveTask(taskId))
            {
                await SaveFolderStructureAsync(structure);
                _logger.LogDebug("Task {TaskId} aus Ordner {FolderName} entfernt", taskId, folder.Name);
                return true;
            }

            return false; // Task nicht im Ordner
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Entfernen der Task {TaskId} aus Ordner {FolderId}", taskId, folderId);
            return false;
        }
    }

    public async Task<int> RemoveTaskFromAllFoldersAsync(int taskId)
    {
        try
        {
            var structure = await LoadFolderStructureAsync();
            var removedCount = structure.RemoveTaskFromAllFolders(taskId);
            
            if (removedCount > 0)
            {
                await SaveFolderStructureAsync(structure);
                _logger.LogDebug("Task {TaskId} aus {Count} Ordnern entfernt", taskId, removedCount);
            }

            return removedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Entfernen der Task {TaskId} aus allen Ordnern", taskId);
            return 0;
        }
    }

    public async Task<List<CustomFolder>> GetAllFoldersAsync()
    {
        try
        {
            var structure = await LoadFolderStructureAsync();
            return structure.GetSortedFolders();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden aller Ordner");
            return new List<CustomFolder>();
        }
    }

    public async Task<CustomFolder?> GetFolderAsync(Guid folderId)
    {
        try
        {
            var structure = await LoadFolderStructureAsync();
            return structure.GetFolder(folderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden des Ordners {FolderId}", folderId);
            return null;
        }
    }

    public async Task<List<CustomFolder>> GetFoldersContainingTaskAsync(int taskId)
    {
        try
        {
            var structure = await LoadFolderStructureAsync();
            return structure.GetFoldersContainingTask(taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Suchen von Ordnern für Task {TaskId}", taskId);
            return new List<CustomFolder>();
        }
    }
}