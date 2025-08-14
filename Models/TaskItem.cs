using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Admin_Tasks.Models;

public class TaskItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public TaskStatus Status { get; set; } = TaskStatus.Open;

    [Required]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DueDate { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    [Required]
    public int CreatedByUserId { get; set; }

    public int? AssignedToUserId { get; set; }

    // Navigation Properties
    [ForeignKey("CreatedByUserId")]
    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("AssignedToUserId")]
    public virtual User? AssignedToUser { get; set; }

    public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
}

public enum TaskStatus
{
    Open = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3,
    OnHold = 4
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}