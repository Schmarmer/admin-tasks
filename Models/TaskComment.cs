using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Admin_Tasks.Models;

public class TaskComment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Typ des Kommentars (Normal, System, Urgent)
    /// </summary>
    public CommentType Type { get; set; } = CommentType.Normal;

    /// <summary>
    /// Ob der Kommentar als gelesen markiert wurde
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// Ob der Kommentar bearbeitet wurde
    /// </summary>
    public bool IsEdited { get; set; } = false;

    /// <summary>
    /// Referenz auf einen anderen Kommentar (für Antworten)
    /// </summary>
    public int? ParentCommentId { get; set; }

    // Foreign Keys
    [Required]
    public int TaskId { get; set; }

    [Required]
    public int UserId { get; set; }

    // Navigation Properties
    [ForeignKey("TaskId")]
    public virtual TaskItem Task { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("ParentCommentId")]
    public virtual TaskComment? ParentComment { get; set; }

    public virtual ICollection<TaskComment> Replies { get; set; } = new List<TaskComment>();
}

public enum CommentType
{
    Normal = 1,
    System = 2,
    Urgent = 3,
    Question = 4,
    Answer = 5
}