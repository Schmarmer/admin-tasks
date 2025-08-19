using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Admin_Tasks.Models;

public class TaskItem : INotifyPropertyChanged
{
    private int _id;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private TaskStatus _status = TaskStatus.Open;
    private TaskPriority _priority = TaskPriority.Unspecified;
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _dueDate;
    private DateTime? _completedAt;
    private DateTime _updatedAt = DateTime.UtcNow;
    private int _createdByUserId;
    private int? _assignedToUserId;
    private int? _categoryId;

    [Key]
    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    [Required]
    [MaxLength(200)]
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    [MaxLength(2000)]
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    [Required]
    public TaskStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    [Required]
    public TaskPriority Priority
    {
        get => _priority;
        set => SetProperty(ref _priority, value);
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    public DateTime? DueDate
    {
        get => _dueDate;
        set => SetProperty(ref _dueDate, value);
    }

    public DateTime? CompletedAt
    {
        get => _completedAt;
        set => SetProperty(ref _completedAt, value);
    }

    public DateTime UpdatedAt
    {
        get => _updatedAt;
        set => SetProperty(ref _updatedAt, value);
    }

    // Foreign Keys
    [Required]
    public int CreatedByUserId
    {
        get => _createdByUserId;
        set => SetProperty(ref _createdByUserId, value);
    }

    public int? AssignedToUserId
    {
        get => _assignedToUserId;
        set => SetProperty(ref _assignedToUserId, value);
    }

    public int? CategoryId
    {
        get => _categoryId;
        set => SetProperty(ref _categoryId, value);
    }

    // Navigation Properties
    [ForeignKey("CreatedByUserId")]
    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("AssignedToUserId")]
    public virtual User? AssignedToUser { get; set; }

    [ForeignKey("CategoryId")]
    public virtual TaskCategory? TaskCategory { get; set; }

    public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    public virtual ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
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
    Unspecified = 0, // Sentinel value
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}