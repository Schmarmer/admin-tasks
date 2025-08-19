using System.Text.Json.Serialization;

namespace Admin_Tasks.Models;

/// <summary>
/// Repräsentiert einen benutzerdefinierten Ordner für die Aufgabenorganisation
/// </summary>
public class CustomFolder
{
    /// <summary>
    /// Eindeutige ID des Ordners
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Name des Ordners (z.B. Kundenname)
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optionale Beschreibung des Ordners
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Farbe für die Ordner-Darstellung (Hex-Code)
    /// </summary>
    [JsonPropertyName("color")]
    public string Color { get; set; } = "#2563EB"; // Standard: Blau

    /// <summary>
    /// Liste der Task-IDs, die diesem Ordner zugeordnet sind
    /// </summary>
    [JsonPropertyName("taskIds")]
    public List<int> TaskIds { get; set; } = new();

    /// <summary>
    /// Erstellungsdatum des Ordners
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Letztes Änderungsdatum
    /// </summary>
    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.Now;

    /// <summary>
    /// Sortierreihenfolge für die Anzeige
    /// </summary>
    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }

    /// <summary>
    /// Ob der Ordner erweitert/eingeklappt ist (UI-State)
    /// </summary>
    [JsonPropertyName("isExpanded")]
    public bool IsExpanded { get; set; } = true;

    /// <summary>
    /// Fügt eine Task-ID zu diesem Ordner hinzu
    /// </summary>
    /// <param name="taskId">Die ID der Task</param>
    /// <returns>True wenn hinzugefügt, False wenn bereits vorhanden</returns>
    public bool AddTask(int taskId)
    {
        if (TaskIds.Contains(taskId))
            return false;

        TaskIds.Add(taskId);
        LastModified = DateTime.Now;
        return true;
    }

    /// <summary>
    /// Entfernt eine Task-ID aus diesem Ordner
    /// </summary>
    /// <param name="taskId">Die ID der Task</param>
    /// <returns>True wenn entfernt, False wenn nicht vorhanden</returns>
    public bool RemoveTask(int taskId)
    {
        if (!TaskIds.Contains(taskId))
            return false;

        TaskIds.Remove(taskId);
        LastModified = DateTime.Now;
        return true;
    }

    /// <summary>
    /// Prüft ob eine Task diesem Ordner zugeordnet ist
    /// </summary>
    /// <param name="taskId">Die ID der Task</param>
    /// <returns>True wenn zugeordnet</returns>
    public bool ContainsTask(int taskId)
    {
        return TaskIds.Contains(taskId);
    }

    /// <summary>
    /// Anzahl der Tasks in diesem Ordner
    /// </summary>
    [JsonIgnore]
    public int TaskCount => TaskIds.Count;

    public override string ToString()
    {
        return $"{Name} ({TaskCount} Tasks)";
    }

    public override bool Equals(object? obj)
    {
        return obj is CustomFolder folder && Id.Equals(folder.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}