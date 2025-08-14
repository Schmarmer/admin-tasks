using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Admin_Tasks.Services;
using Admin_Tasks.Models;

namespace Admin_Tasks.ViewModels;

public class TaskEditViewModel : BaseViewModel
{
    private readonly ITaskService _taskService;
    private readonly IAuthenticationService _authService;
    private TaskItem? _originalTask;
    private int _taskId;
    private string _taskTitle = string.Empty;
    private string _description = string.Empty;
    private Models.TaskStatus _status;
    private TaskPriority _priority;
    private DateTime? _dueDate;
    private User? _assignedUser;
    private int? _assignedToUserId;
    private string _errorMessage = string.Empty;
    private bool _isEditMode;

    public TaskEditViewModel(ITaskService taskService, IAuthenticationService authService)
    {
        _taskService = taskService;
        _authService = authService;
        
        AvailableUsers = new ObservableCollection<User>();
        
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CancelCommand = new RelayCommand(Cancel);
        
        // Load users
        _ = LoadUsersAsync();
    }

    public ObservableCollection<User> AvailableUsers { get; }

    public bool IsEditMode
    {
        get => _isEditMode;
        private set
        {
            SetProperty(ref _isEditMode, value);
            Title = value ? "Aufgabe bearbeiten" : "Neue Aufgabe erstellen";
        }
    }

    public int TaskId
    {
        get => _taskId;
        set => SetProperty(ref _taskId, value);
    }

    public string TaskTitle
    {
        get => _taskTitle;
        set
        {
            SetProperty(ref _taskTitle, value);
            SaveCommand.NotifyCanExecuteChanged();
            ClearError();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            SetProperty(ref _description, value);
            ClearError();
        }
    }

    public Models.TaskStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public TaskPriority Priority
    {
        get => _priority;
        set => SetProperty(ref _priority, value);
    }

    public DateTime? DueDate
    {
        get => _dueDate;
        set => SetProperty(ref _dueDate, value);
    }

    public User? AssignedUser
    {
        get => _assignedUser;
        set => SetProperty(ref _assignedUser, value);
    }

    public int? AssignedToUserId
    {
        get => _assignedToUserId;
        set => SetProperty(ref _assignedToUserId, value);
    }

    public ObservableCollection<User> Users => AvailableUsers;

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public Array TaskStatuses => Enum.GetValues(typeof(Models.TaskStatus));
    public Array TaskPriorities => Enum.GetValues(typeof(TaskPriority));

    public bool CanEditStatus => IsEditMode && (_authService.CurrentUser?.Role is "Admin" or "Manager" || 
                                               _originalTask?.CreatedByUserId == _authService.CurrentUser?.Id);

    public bool CanAssignUser => _authService.CurrentUser?.Role is "Admin" or "Manager";

    public IAsyncRelayCommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event EventHandler<TaskItem>? TaskSaved;
    public event EventHandler? Cancelled;

    public void SetTask(TaskItem? task)
    {
        _originalTask = task;
        IsEditMode = task != null;

        if (task != null)
        {
            TaskId = task.Id;
            TaskTitle = task.Title;
            Description = task.Description;
            Status = task.Status;
            Priority = task.Priority;
            DueDate = task.DueDate;
            AssignedUser = task.AssignedToUser;
        }
        else
        {
            // New task defaults
            TaskId = 0;
            TaskTitle = string.Empty;
            Description = string.Empty;
            Status = Models.TaskStatus.Open;
            Priority = TaskPriority.Medium;
            DueDate = null;
            AssignedUser = null;
        }

        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(CanEditStatus));
        OnPropertyChanged(nameof(CanAssignUser));
        SaveCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadUsersAsync()
    {
        var users = await ExecuteAsync(() => _taskService.GetAvailableUsersAsync());
        
        AvailableUsers.Clear();
        if (users != null)
        {
            foreach (var user in users)
            {
                AvailableUsers.Add(user);
            }
        }
    }

    private bool CanSave()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(TaskTitle);
    }

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        var currentUser = _authService.CurrentUser;
        
        if (currentUser == null)
        {
            ErrorMessage = "Sie müssen angemeldet sein, um Aufgaben zu speichern.";
            return;
        }

        if (string.IsNullOrWhiteSpace(TaskTitle))
        {
            ErrorMessage = "Der Titel ist erforderlich.";
            return;
        }

        var success = await ExecuteAsync(async () =>
        {
            if (IsEditMode && _originalTask != null)
            {
                // Update existing task
                var updatedTask = new TaskItem
                {
                    Id = TaskId,
                    Title = TaskTitle.Trim(),
                    Description = Description?.Trim() ?? string.Empty,
                    Status = Status,
                    Priority = Priority,
                    DueDate = DueDate,
                    AssignedToUserId = AssignedUser?.Id,
                    CreatedByUserId = _originalTask.CreatedByUserId
                };

                return await _taskService.UpdateTaskAsync(updatedTask);
            }
            else
            {
                // Create new task
                var newTask = new TaskItem
                {
                    Title = TaskTitle.Trim(),
                    Description = Description?.Trim() ?? string.Empty,
                    Status = Status,
                    Priority = Priority,
                    DueDate = DueDate,
                    AssignedToUserId = AssignedUser?.Id,
                    CreatedByUserId = currentUser.Id
                };

                var createdTask = await _taskService.CreateTaskAsync(newTask);
                return createdTask != null;
            }
        });

        if (success)
        {
            // Get the updated task for the event
            var savedTask = IsEditMode && _originalTask != null 
                ? await _taskService.GetTaskByIdAsync(TaskId)
                : await _taskService.GetTaskByIdAsync(TaskId); // For new tasks, TaskId will be set after creation
            
            if (savedTask != null)
            {
                TaskSaved?.Invoke(this, savedTask);
            }
        }
        else
        {
            ErrorMessage = IsEditMode 
                ? "Fehler beim Aktualisieren der Aufgabe. Bitte versuchen Sie es erneut."
                : "Fehler beim Erstellen der Aufgabe. Bitte versuchen Sie es erneut.";
        }
    }

    private void Cancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private void ClearError()
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
            ErrorMessage = string.Empty;
    }

    private void ClearForm()
    {
        TaskTitle = string.Empty;
        Description = string.Empty;
        Status = Models.TaskStatus.Open;
        Priority = TaskPriority.Medium;
        DueDate = null;
        AssignedUser = null;
        ClearError();
    }

    public void PrepareForNewTask()
    {
        IsEditMode = false;
        ClearForm();
    }

    public void LoadTask(TaskItem task)
    {
        IsEditMode = true;
        TaskTitle = task.Title;
        Description = task.Description;
        Status = task.Status;
        Priority = task.Priority;
        DueDate = task.DueDate;
        AssignedToUserId = task.AssignedToUserId;
        
        // Find and set the assigned user
        if (task.AssignedToUserId.HasValue)
        {
            AssignedUser = Users.FirstOrDefault(u => u.Id == task.AssignedToUserId.Value);
        }
        else
        {
            AssignedUser = null;
        }
        
        ClearError();
    }

    protected override void OnBusyChanged()
    {
        SaveCommand.NotifyCanExecuteChanged();
    }
}