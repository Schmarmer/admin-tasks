using System.Text.Json.Serialization;

namespace Admin_Tasks.Models;

/// <summary>
/// Repräsentiert die gesamte Ordnerstruktur für die JSON-Serialisierung
/// </summary>
public class FolderStructure
{
    /// <summary>
    /// Version der Datenstruktur für zukünftige Migrationen
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Liste aller benutzerdefinierten Ordner
    /// </summary>
    [JsonPropertyName("customFolders")]
    public List<CustomFolder> CustomFolders { get; set; } = new();

    /// <summary>
    /// Metadaten über die Struktur
    /// </summary>
    [JsonPropertyName("metadata")]
    public FolderMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Fügt einen neuen Ordner hinzu
    /// </summary>
    /// <param name="folder">Der hinzuzufügende Ordner</param>
    /// <returns>True wenn erfolgreich hinzugefügt</returns>
    public bool AddFolder(CustomFolder folder)
    {
        if (CustomFolders.Any(f => f.Id == folder.Id || f.Name.Equals(folder.Name, StringComparison.OrdinalIgnoreCase)))
            return false;

        // Sortierreihenfolge setzen
        folder.SortOrder = CustomFolders.Count;
        CustomFolders.Add(folder);
        Metadata.LastModified = DateTime.Now;
        return true;
    }

    /// <summary>
    /// Entfernt einen Ordner
    /// </summary>
    /// <param name="folderId">Die ID des zu entfernenden Ordners</param>
    /// <returns>True wenn erfolgreich entfernt</returns>
    public bool RemoveFolder(Guid folderId)
    {
        var folder = CustomFolders.FirstOrDefault(f => f.Id == folderId);
        if (folder == null)
            return false;

        CustomFolders.Remove(folder);
        Metadata.LastModified = DateTime.Now;
        
        // Sortierreihenfolge neu ordnen
        for (int i = 0; i < CustomFolders.Count; i++)
        {
            CustomFolders[i].SortOrder = i;
        }
        
        return true;
    }

    /// <summary>
    /// Findet einen Ordner anhand der ID
    /// </summary>
    /// <param name="folderId">Die ID des Ordners</param>
    /// <returns>Der Ordner oder null</returns>
    public CustomFolder? GetFolder(Guid folderId)
    {
        return CustomFolders.FirstOrDefault(f => f.Id == folderId);
    }

    /// <summary>
    /// Findet einen Ordner anhand des Namens
    /// </summary>
    /// <param name="name">Der Name des Ordners</param>
    /// <returns>Der Ordner oder null</returns>
    public CustomFolder? GetFolderByName(string name)
    {
        return CustomFolders.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gibt alle Ordner sortiert nach SortOrder zurück
    /// </summary>
    /// <returns>Sortierte Liste der Ordner</returns>
    public List<CustomFolder> GetSortedFolders()
    {
        return CustomFolders.OrderBy(f => f.SortOrder).ToList();
    }

    /// <summary>
    /// Findet alle Ordner, die eine bestimmte Task enthalten
    /// </summary>
    /// <param name="taskId">Die ID der Task</param>
    /// <returns>Liste der Ordner, die die Task enthalten</returns>
    public List<CustomFolder> GetFoldersContainingTask(int taskId)
    {
        return CustomFolders.Where(f => f.ContainsTask(taskId)).ToList();
    }

    /// <summary>
    /// Entfernt eine Task aus allen Ordnern
    /// </summary>
    /// <param name="taskId">Die ID der zu entfernenden Task</param>
    /// <returns>Anzahl der Ordner, aus denen die Task entfernt wurde</returns>
    public int RemoveTaskFromAllFolders(int taskId)
    {
        int removedCount = 0;
        foreach (var folder in CustomFolders)
        {
            if (folder.RemoveTask(taskId))
                removedCount++;
        }
        
        if (removedCount > 0)
            Metadata.LastModified = DateTime.Now;
            
        return removedCount;
    }
}

/// <summary>
/// Metadaten für die Ordnerstruktur
/// </summary>
public class FolderMetadata
{
    /// <summary>
    /// Erstellungsdatum der Struktur
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Letztes Änderungsdatum
    /// </summary>
    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.Now;

    /// <summary>
    /// Anzahl der Ordner
    /// </summary>
    [JsonPropertyName("folderCount")]
    public int FolderCount { get; set; }

    /// <summary>
    /// Benutzer, der die Struktur erstellt hat
    /// </summary>
    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }
}