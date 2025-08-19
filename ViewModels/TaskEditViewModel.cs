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
    private readonly IAttachmentService _attachmentService;
    private readonly ICategoryService _categoryService;
    private TaskItem? _originalTask;
    private ObservableCollection<TaskAttachment> _attachments;
    private int _taskId;
    private string _taskTitle = string.Empty;
    private string _description = string.Empty;
    private Models.TaskStatus _status;
    private TaskPriority _priority;
    private DateTime? _dueDate;
    private User? _assignedUser;
    private int? _assignedToUserId;
    private TaskCategory? _selectedCategory;
    private int? _categoryId;
    private string _newCategoryName = string.Empty;
    private bool _isCreatingNewCategory;
    private string _errorMessage = string.Empty;
    private bool _isEditMode;

    public TaskEditViewModel(ITaskService taskService, IAuthenticationService authService, IAttachmentService attachmentService, ICategoryService categoryService)
    {
        _taskService = taskService;
        _authService = authService;
        _attachmentService = attachmentService;
        _categoryService = categoryService;
        
        AvailableUsers = new ObservableCollection<User>();
        AvailableUsersWithNone = new ObservableCollection<User?>();
        AvailableCategories = new ObservableCollection<TaskCategory>();
        _attachments = new ObservableCollection<TaskAttachment>();
        TemporaryAttachments = new ObservableCollection<TaskAttachment>();
        
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CancelCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(Cancel);
        CreateCategoryCommand = new AsyncRelayCommand(CreateNewCategoryAsync, CanCreateNewCategory);
        
        // Load users and categories
        _ = LoadUsersAsync();
        _ = LoadCategoriesAsync();
    }

    public ObservableCollection<User> AvailableUsers { get; }
    public ObservableCollection<User?> AvailableUsersWithNone { get; }
    public ObservableCollection<TaskCategory> AvailableCategories { get; }
    
    public ObservableCollection<TaskAttachment> Attachments
    {
        get => _attachments;
        set => SetProperty(ref _attachments, value);
    }

    public ObservableCollection<TaskAttachment> TemporaryAttachments { get; set; }

    public bool IsEditMode
    {
        get => _isEditMode;
        private set
        {
            SetProperty(ref _isEditMode, value);
            Title = value ? "Aufgabe bearbeiten" : "Neue Aufgabe erstellen";
            OnPropertyChanged(nameof(SaveButtonText));
        }
    }

    public string SaveButtonText => IsEditMode ? "Aktualisieren" : "Erstellen";

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
        set 
        {
            // Ensure DateTime is always UTC for PostgreSQL compatibility
            DateTime? utcValue = null;
            if (value.HasValue)
            {
                utcValue = value.Value.Kind == DateTimeKind.Utc 
                    ? value.Value 
                    : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
            }
            SetProperty(ref _dueDate, utcValue);
        }
    }

    public User? AssignedUser
    {
        get => _assignedUser;
        set 
        {
            SetProperty(ref _assignedUser, value);
            // If "Ohne Besitzer" is selected (Id = 0), set AssignedToUserId to null
            AssignedToUserId = (value?.Id == 0) ? null : value?.Id;
        }
    }

    public int? AssignedToUserId
    {
        get => _assignedToUserId;
        set => SetProperty(ref _assignedToUserId, value);
    }

    public TaskCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            SetProperty(ref _selectedCategory, value);
            CategoryId = value?.Id;
            // Reset new category creation when selecting existing category
            if (value != null)
            {
                IsCreatingNewCategory = false;
                NewCategoryName = string.Empty;
            }
        }
    }

    public int? CategoryId
    {
        get => _categoryId;
        set => SetProperty(ref _categoryId, value);
    }

    public string NewCategoryName
    {
        get => _newCategoryName;
        set
        {
            SetProperty(ref _newCategoryName, value);
            CreateCategoryCommand.NotifyCanExecuteChanged();
        }
    }

    public bool IsCreatingNewCategory
    {
        get => _isCreatingNewCategory;
        set
        {
            SetProperty(ref _isCreatingNewCategory, value);
            if (value)
            {
                SelectedCategory = null;
            }
            else
            {
                NewCategoryName = string.Empty;
            }
        }
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

    public bool CanAssignUser => _authService.CurrentUser?.Role is "Admin" or "Manager" || 
                                 (IsEditMode && _originalTask?.AssignedToUserId == null); // Allow assignment for "Ohne Besitzer" tasks
    
    public bool CanDeleteAttachments => _authService.CurrentUser?.Role is "Admin" or "Manager" || 
                                       _originalTask?.CreatedByUserId == _authService.CurrentUser?.Id;
    
    public TaskItem? Task => _originalTask;

    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IAsyncRelayCommand CreateCategoryCommand { get; }

    public event EventHandler<TaskItem>? TaskSaved;
    public event EventHandler? Cancelled;

    public void SetTask(TaskItem? task)
    {
        _originalTask = task;
        IsEditMode = task != null;

        // Kategorien und Benutzer bei jedem Öffnen des Fensters neu laden
        _ = LoadCategoriesAsync();
        _ = LoadUsersAsync();

        if (task != null)
        {
            TaskId = task.Id;
            TaskTitle = task.Title;
            Description = task.Description;
            Status = task.Status;
            Priority = task.Priority;
            DueDate = task.DueDate;
            CategoryId = task.CategoryId;
            
            // Set selected category if task has one
            if (task.CategoryId.HasValue)
            {
                SelectedCategory = AvailableCategories.FirstOrDefault(c => c.Id == task.CategoryId.Value);
            }
            else
            {
                SelectedCategory = null;
            }
            
            // If task has no assigned user, select "Ohne Besitzer" option
            if (task.AssignedToUser == null)
            {
                AssignedUser = AvailableUsersWithNone.FirstOrDefault(u => u?.Id == 0);
            }
            else
            {
                AssignedUser = task.AssignedToUser;
            }
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
            CategoryId = null;
            SelectedCategory = null;
            IsCreatingNewCategory = false;
            NewCategoryName = string.Empty;
            // For new tasks, default to "Ohne Besitzer"
            AssignedUser = AvailableUsersWithNone.FirstOrDefault(u => u?.Id == 0);
        }

        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(CanEditStatus));
        OnPropertyChanged(nameof(CanAssignUser));
        OnPropertyChanged(nameof(CanDeleteAttachments));
        SaveCommand.NotifyCanExecuteChanged();
        
        // Load attachments for existing task
        if (IsEditMode && task?.Id > 0)
        {
            _ = LoadAttachmentsAsync();
        }
        else
        {
            Attachments.Clear();
        }
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[TaskEditViewModel] LoadUsersAsync gestartet");
            
            var users = await ExecuteAsync(() => _taskService.GetAvailableUsersAsync());
            
            System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] {users?.Count() ?? 0} Benutzer geladen");
            
            AvailableUsers.Clear();
            AvailableUsersWithNone.Clear();
            
            // Add "Ohne Besitzer" option first
            var noneUser = new User 
            { 
                Id = 0, 
                Username = "Ohne Besitzer", 
                Role = null,
                IsActive = true
            };
            AvailableUsersWithNone.Add(noneUser);
            
            if (users != null && users.Any())
            {
                foreach (var user in users)
                {
                    System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] Benutzer hinzugefügt: {user.Username} (Role: {user.Role})");
                    AvailableUsers.Add(user);
                    AvailableUsersWithNone.Add(user);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[TaskEditViewModel] Keine Benutzer verfügbar - Fallback hinzugefügt");
                // Add a default "no users" entry if no users are available
                AvailableUsers.Add(new User 
                { 
                    Id = -1, 
                    Username = "Keine Benutzer verfügbar", 
                    Role = "System",
                    IsActive = false
                });
            }
            
            System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] AvailableUsers.Count: {AvailableUsers.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] FEHLER beim Laden der Benutzer: {ex}");
            ErrorMessage = $"Fehler beim Laden der Benutzer: {ex.Message}";
            AvailableUsers.Clear();
            AvailableUsers.Add(new User 
            { 
                Id = 0, 
                Username = "Fehler beim Laden", 
                Role = "System",
                IsActive = false
            });
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
            try
            {
                System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] Saving task - IsEditMode: {IsEditMode}");
                System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] AssignedUser: {AssignedUser?.Username ?? "null"}, ID: {AssignedUser?.Id}");
                System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] CurrentUser: {currentUser?.Username}, ID: {currentUser?.Id}");
                
                if (IsEditMode && _originalTask != null)
                {
                    // Update existing task
                    // Validate AssignedUser exists in AvailableUsersWithNone if assigned
                    int? validAssignedUserId = null;
                    if (AssignedUser != null)
                    {
                        // Special handling for "Ohne Besitzer" option (ID = 0)
                        if (AssignedUser.Id == 0)
                        {
                            validAssignedUserId = null; // "Ohne Besitzer" means no assignment
                        }
                        else
                        {
                            var userExists = AvailableUsersWithNone.Any(u => u.Id == AssignedUser.Id);
                            if (userExists)
                            {
                                validAssignedUserId = AssignedUser.Id;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] Warning: AssignedUser ID {AssignedUser.Id} not found in AvailableUsersWithNone");
                            }
                        }
                    }
                    
                    var updatedTask = new TaskItem
                    {
                        Id = TaskId,
                        Title = TaskTitle.Trim(),
                        Description = Description?.Trim() ?? string.Empty,
                        Status = Status,
                        Priority = Priority,
                        DueDate = DueDate,
                        AssignedToUserId = validAssignedUserId,
                        CategoryId = CategoryId,
                        CreatedByUserId = _originalTask.CreatedByUserId
                    };

                    System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] Updating task ID: {updatedTask.Id}, AssignedToUserId: {updatedTask.AssignedToUserId}");
                    var success = await _taskService.UpdateTaskAsync(updatedTask);
                    
                    // Process temporary attachments for updated task
                    if (success && TemporaryAttachments.Any())
                    {
                        foreach (var tempAttachment in TemporaryAttachments)
                        {
                            tempAttachment.TaskId = TaskId; // Assign the existing TaskId
                            await _attachmentService.SaveAttachmentAsync(tempAttachment);
                        }
                        TemporaryAttachments.Clear();
                        await LoadAttachmentsAsync(); // Refresh attachments
                    }
                    
                    return success;
                }
                else
                {
                    // Create new task
                    // Validate AssignedUser exists in AvailableUsersWithNone if assigned
                    int? validAssignedUserId = null;
                    if (AssignedUser != null)
                    {
                        // Special handling for "Ohne Besitzer" option (ID = 0)
                        if (AssignedUser.Id == 0)
                        {
                            validAssignedUserId = null; // "Ohne Besitzer" means no assignment
                        }
                        else
                        {
                            var userExists = AvailableUsersWithNone.Any(u => u.Id == AssignedUser.Id);
                            if (userExists)
                            {
                                validAssignedUserId = AssignedUser.Id;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] Warning: AssignedUser ID {AssignedUser.Id} not found in AvailableUsersWithNone");
                            }
                        }
                    }
                    
                    var newTask = new TaskItem
                    {
                        Title = TaskTitle.Trim(),
                        Description = Description?.Trim() ?? string.Empty,
                        Status = Status,
                        Priority = Priority,
                        DueDate = DueDate,
                        AssignedToUserId = validAssignedUserId,
                        CategoryId = CategoryId,
                        CreatedByUserId = currentUser.Id
                    };

                    System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] Creating new task, AssignedToUserId: {newTask.AssignedToUserId}, CreatedByUserId: {newTask.CreatedByUserId}");
                    var createdTask = await _taskService.CreateTaskAsync(newTask);
                    if (createdTask != null)
                    {
                        // Set TaskId so that subsequent retrieval works and events can pass the correct entity
                        TaskId = createdTask.Id;
                        _originalTask = createdTask;

                        // Process temporary attachments
                        if (TemporaryAttachments.Any())
                        {
                            foreach (var tempAttachment in TemporaryAttachments)
                            {
                                tempAttachment.TaskId = createdTask.Id; // Assign the new TaskId
                                await _attachmentService.SaveAttachmentAsync(tempAttachment);
                            }
                            TemporaryAttachments.Clear();
                            await LoadAttachmentsAsync(); // Refresh attachments
                        }
                        
                        return true;
                    }
                    return false;
                }
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] DbUpdateException: {dbEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] Inner Exception: {dbEx.InnerException?.Message}");
                if (dbEx.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] Inner Exception Details: {dbEx.InnerException}");
                }
                throw new InvalidOperationException($"Datenbankfehler beim Speichern: {dbEx.InnerException?.Message ?? dbEx.Message}", dbEx);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] General Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] Exception Details: {ex}");
                throw;
            }
        });

        if (success)
        {
            // Get the updated/created task for the event
            var savedTask = await _taskService.GetTaskByIdAsync(TaskId);
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
        
        // Kategorien und Benutzer bei jedem Öffnen des Fensters neu laden
        _ = LoadCategoriesAsync();
        _ = LoadUsersAsync();
        
        ClearForm();
    }

    public void LoadTask(TaskItem task)
    {
        IsEditMode = true;
        
        // Kategorien und Benutzer bei jedem Öffnen des Fensters neu laden
        _ = LoadCategoriesAsync();
        _ = LoadUsersAsync();
        
        TaskTitle = task.Title;
        Description = task.Description;
        Status = task.Status;
        Priority = task.Priority;
        DueDate = task.DueDate;
        AssignedToUserId = task.AssignedToUserId;
        
        // Find and set the assigned user
        if (task.AssignedToUserId.HasValue)
        {
            AssignedUser = AvailableUsers.FirstOrDefault(u => u.Id == task.AssignedToUserId.Value);
        }
        else
        {
            AssignedUser = null;
        }
        
        ClearError();
        
        // Load attachments for existing task
        if (IsEditMode && task.Id > 0)
        {
            _ = LoadAttachmentsAsync();
        }
    }
    
    public async Task LoadAttachmentsAsync()
    {
        try
        {
            if (_originalTask?.Id > 0)
            {
                var attachments = await _attachmentService.GetTaskAttachmentsAsync(_originalTask.Id);
                Attachments.Clear();
                
                if (attachments?.Any() == true)
                {
                    foreach (var attachment in attachments)
                    {
                        Attachments.Add(attachment);
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] {Attachments.Count} Anhänge geladen");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] Error loading attachments: {ex}");
        }
    }
    
    public async Task<bool> DeleteAttachmentAsync(TaskAttachment attachment)
    {
        try
        {
            var success = await _attachmentService.DeleteAttachmentAsync(attachment.Id);
            if (success)
            {
                Attachments.Remove(attachment);
                System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] Anhang gelöscht: {attachment.FileName}");
            }
            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] Error deleting attachment: {ex}");
            return false;
        }
    }
    
    public void AddAttachment(TaskAttachment attachment)
    {
        if (attachment != null)
        {
            // Always add to TemporaryAttachments for processing during save
            if (!TemporaryAttachments.Any(a => a.FileName == attachment.FileName && 
                (a.FileData?.SequenceEqual(attachment.FileData ?? new byte[0]) ?? false)))
            {
                TemporaryAttachments.Add(attachment);
                System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] Temporärer Anhang hinzugefügt: {attachment.FileName}");
            }
            
            // Also add to Attachments for immediate UI display
            if (IsEditMode && !Attachments.Contains(attachment))
            {
                Attachments.Add(attachment);
                System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] Anhang zur UI-Anzeige hinzugefügt: {attachment.FileName}");
            }
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await _categoryService.GetAllActiveCategoriesAsync();
            AvailableCategories.Clear();
            
            foreach (var category in categories)
            {
                AvailableCategories.Add(category);
            }
            
            System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] {AvailableCategories.Count} Kategorien geladen");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] Error loading categories: {ex}");
        }
    }
    
    private async Task CreateNewCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName))
            return;

        await ExecuteAsync(async () =>
        {
            var newCategory = await _categoryService.CreateCategoryAsync(NewCategoryName.Trim());
            
            // Add to available categories
            AvailableCategories.Add(newCategory);
            
            // Select the new category
            SelectedCategory = newCategory;
            
            // Reset creation mode
            IsCreatingNewCategory = false;
            NewCategoryName = string.Empty;
            
            System.Diagnostics.Debug.WriteLine($"[TaskEditViewModel] Neue Kategorie erstellt: {newCategory.Name}");
        });
    }
    
    private bool CanCreateNewCategory()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(NewCategoryName) && IsCreatingNewCategory;
    }

    protected override void OnBusyChanged()
    {
        SaveCommand.NotifyCanExecuteChanged();
        CreateCategoryCommand.NotifyCanExecuteChanged();
    }
}