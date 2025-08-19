using System;
using System.Threading.Tasks;

namespace Admin_Tasks.Services
{
    public interface ISettingsService
    {
        /// <summary>
        /// Initialisiert den Einstellungsdienst und lädt die Daten
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Lädt eine Einstellung vom persistenten Speicher
        /// </summary>
        /// <typeparam name="T">Typ der Einstellung</typeparam>
        /// <param name="key">Schlüssel der Einstellung</param>
        /// <param name="defaultValue">Standardwert falls Einstellung nicht existiert</param>
        /// <returns>Wert der Einstellung oder Standardwert</returns>
        Task<T> GetSettingAsync<T>(string key, T defaultValue = default(T));
        
        /// <summary>
        /// Speichert eine Einstellung im persistenten Speicher
        /// </summary>
        /// <typeparam name="T">Typ der Einstellung</typeparam>
        /// <param name="key">Schlüssel der Einstellung</param>
        /// <param name="value">Wert der Einstellung</param>
        Task SetSettingAsync<T>(string key, T value);
        
        /// <summary>
        /// Entfernt eine Einstellung aus dem persistenten Speicher
        /// </summary>
        /// <param name="key">Schlüssel der Einstellung</param>
        Task RemoveSettingAsync(string key);
        
        /// <summary>
        /// Speichert alle Einstellungen
        /// </summary>
        Task SaveSettingsAsync();
        
        /// <summary>
        /// Prüft ob eine Einstellung existiert
        /// </summary>
        /// <param name="key">Schlüssel der Einstellung</param>
        /// <returns>True wenn Einstellung existiert</returns>
        Task<bool> HasSettingAsync(string key);
        
        /// <summary>
        /// Löscht alle Einstellungen
        /// </summary>
        Task ClearAllSettingsAsync();
    }
}