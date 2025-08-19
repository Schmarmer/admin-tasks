using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Admin_Tasks.Models;

public class Notification
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    public NotificationType Type { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    // Foreign Keys
    [Required]
    public int UserId { get; set; }

    public int? TaskId { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("TaskId")]
    public virtual TaskItem? Task { get; set; }
}

public enum NotificationType
{
    TaskAssigned = 1,
    TaskCompleted = 2,
    TaskForwarded = 3,
    TaskStatusChanged = 4,
    TaskCommentAdded = 5,
    TaskDueDateApproaching = 6,
    TaskOverdue = 7,
    General = 8
}