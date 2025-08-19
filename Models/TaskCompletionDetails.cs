using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Admin_Tasks.Models;

public class TaskCompletionDetails
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TaskId { get; set; }

    /// <summary>
    /// Zeit in Minuten, die für die Aufgabe aufgewendet wurde
    /// </summary>
    public int TimeSpentMinutes { get; set; }

    /// <summary>
    /// Abschlusstext/Zusammenfassung der Aufgabe
    /// </summary>
    [MaxLength(2000)]
    public string ConclusionText { get; set; } = string.Empty;

    /// <summary>
    /// Optionale Bewertung der Aufgabenschwierigkeit (1-5)
    /// </summary>
    public int? DifficultyRating { get; set; }

    /// <summary>
    /// Optionale Bewertung der Aufgabenzufriedenheit (1-5)
    /// </summary>
    public int? SatisfactionRating { get; set; }

    /// <summary>
    /// Zusätzliche Notizen oder Kommentare
    /// </summary>
    [MaxLength(1000)]
    public string AdditionalNotes { get; set; } = string.Empty;

    /// <summary>
    /// Pfad zu optionalen Abschluss-Bildern oder Dokumenten
    /// </summary>
    [MaxLength(500)]
    public string? CompletionImagePath { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int CompletedByUserId { get; set; }

    // Navigation Properties
    [ForeignKey("TaskId")]
    public virtual TaskItem Task { get; set; } = null!;

    [ForeignKey("CompletedByUserId")]
    public virtual User CompletedByUser { get; set; } = null!;
}