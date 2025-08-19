using Admin_Tasks.Models;

namespace Admin_Tasks.Services;

/// <summary>
/// Service für die Verwaltung benutzerdefinierter Ordner
/// </summary>
public interface ICustomFolderService
{
    /// <summary>
    /// Lädt die Ordnerstruktur aus der lokalen JSON-Datei
    /// </summary>
    /// <returns>Die geladene Ordnerstruktur</returns>
    Task<FolderStructure> LoadFolderStructureAsync();

    /// <summary>
    /// Speichert die Ordnerstruktur in die lokale JSON-Datei
    /// </summary>
    /// <param name="structure">Die zu speichernde Ordnerstruktur</param>
    Task SaveFolderStructureAsync(FolderStructure structure);

    /// <summary>
    /// Erstellt einen neuen benutzerdefinierten Ordner
    /// </summary>
    /// <param name="name">Name des Ordners</param>
    /// <param name="description">Optionale Beschreibung</param>
    /// <param name="color">Farbe des Ordners (Hex-Code)</param>
    /// <returns>Der erstellte Ordner oder null bei Fehler</returns>
    Task<CustomFolder?> CreateFolderAsync(string name, string? description = null, string color = "#2563EB");

    /// <summary>
    /// Erstellt einen neuen benutzerdefinierten Ordner aus einem CustomFolder-Objekt
    /// </summary>
    /// <param name="customFolder">Das CustomFolder-Objekt mit allen Eigenschaften</param>
    /// <returns>Der erstellte Ordner oder null bei Fehler</returns>
    Task<CustomFolder?> CreateCustomFolderAsync(CustomFolder customFolder);

    /// <summary>
    /// Löscht einen benutzerdefinierten Ordner
    /// </summary>
    /// <param name="folderId">ID des zu löschenden Ordners</param>
    /// <returns>True wenn erfolgreich gelöscht</returns>
    Task<bool> DeleteFolderAsync(Guid folderId);

    /// <summary>
    /// Aktualisiert einen benutzerdefinierten Ordner
    /// </summary>
    /// <param name="folder">Der zu aktualisierende Ordner</param>
    /// <returns>True wenn erfolgreich aktualisiert</returns>
    Task<bool> UpdateFolderAsync(CustomFolder folder);

    /// <summary>
    /// Fügt eine Task zu einem Ordner hinzu
    /// </summary>
    /// <param name="folderId">ID des Ordners</param>
    /// <param name="taskId">ID der Task</param>
    /// <returns>True wenn erfolgreich hinzugefügt</returns>
    Task<bool> AddTaskToFolderAsync(Guid folderId, int taskId);

    /// <summary>
    /// Entfernt eine Task aus einem Ordner
    /// </summary>
    /// <param name="folderId">ID des Ordners</param>
    /// <param name="taskId">ID der Task</param>
    /// <returns>True wenn erfolgreich entfernt</returns>
    Task<bool> RemoveTaskFromFolderAsync(Guid folderId, int taskId);

    /// <summary>
    /// Entfernt eine Task aus allen Ordnern
    /// </summary>
    /// <param name="taskId">ID der Task</param>
    /// <returns>Anzahl der Ordner, aus denen die Task entfernt wurde</returns>
    Task<int> RemoveTaskFromAllFoldersAsync(int taskId);

    /// <summary>
    /// Gibt alle benutzerdefinierten Ordner zurück
    /// </summary>
    /// <returns>Liste aller Ordner</returns>
    Task<List<CustomFolder>> GetAllFoldersAsync();

    /// <summary>
    /// Gibt einen Ordner anhand der ID zurück
    /// </summary>
    /// <param name="folderId">ID des Ordners</param>
    /// <returns>Der Ordner oder null</returns>
    Task<CustomFolder?> GetFolderAsync(Guid folderId);

    /// <summary>
    /// Gibt alle Ordner zurück, die eine bestimmte Task enthalten
    /// </summary>
    /// <param name="taskId">ID der Task</param>
    /// <returns>Liste der Ordner</returns>
    Task<List<CustomFolder>> GetFoldersContainingTaskAsync(int taskId);

    /// <summary>
    /// Prüft ob der Pfad zur JSON-Datei existiert und erstellt ihn bei Bedarf
    /// </summary>
    /// <returns>True wenn Pfad verfügbar</returns>
    Task<bool> EnsureDataDirectoryExistsAsync();

    /// <summary>
    /// Gibt den vollständigen Pfad zur JSON-Datei zurück
    /// </summary>
    /// <returns>Pfad zur JSON-Datei</returns>
    string GetDataFilePath();

    /// <summary>
    /// Event das ausgelöst wird, wenn sich die Ordnerstruktur ändert
    /// </summary>
    event EventHandler<FolderStructure>? FolderStructureChanged;
}