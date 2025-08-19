using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Admin_Tasks.Models;

[Table("TaskCategories")]
public class TaskCategory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(7)] // Hex color code like #FF5733
    public string? Color { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation property
    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

    public override string ToString()
    {
        return Name;
    }
}