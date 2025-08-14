using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Admin_Tasks.Services;
using Admin_Tasks.Models;
using System.ComponentModel;

namespace Admin_Tasks.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;
    private readonly ITaskService _taskService;
    private readonly IThemeService _themeService;
    private User? _currentUser;
    private TaskItem? _selectedTask;
    private string _searchText = string.Empty;
    private Models.TaskStatus? _filterStatus;
    private bool _isDarkMode;

    public MainViewModel(IAuthenticationService authService, ITaskService taskService, IThemeService themeService)
    {
        _authService = authService;
        _taskService = taskService;
        _themeService = themeService;
        
        Title = "Admin Tasks - Aufgabenverwaltung";
        
        Tasks = new ObservableCollection<TaskItem>();
        Users = new ObservableCollection<User>();
        
        // Commands
        LoadTasksCommand = new AsyncRelayCommand(LoadTasksAsync);
        RefreshCommand = new AsyncRelayCommand(LoadTasksAsync); // Alias für LoadTasksCommand
        CreateTaskCommand = new AsyncRelayCommand(CreateTaskAsync);
        EditTaskCommand = new AsyncRelayCommand<TaskItem>(EditTaskAsync);
        DeleteTaskCommand = new AsyncRelayCommand<TaskItem>(DeleteTaskAsync);
        CompleteTaskCommand = new AsyncRelayCommand<TaskItem>(CompleteTaskAsync);
        AssignTaskCommand = new AsyncRelayCommand<(TaskItem task, User user)>(AssignTaskAsync);
        SearchCommand = new AsyncRelayCommand(SearchTasksAsync);
        ClearSearchCommand = new RelayCommand(ClearSearch);
        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        
        // Subscribe to authentication changes
        _authService.UserChanged += OnUserChanged;
        _themeService.ThemeChanged += OnThemeChanged;
        CurrentUser = _authService.CurrentUser;
        
        // Theme-Status synchronisieren
        IsDarkMode = _themeService.IsDarkMode;
        
        // Load initial data
        _ = LoadInitialDataAsync();
    }

    public ObservableCollection<TaskItem> Tasks { get; }
    public ObservableCollection<User> Users { get; }

    public User? CurrentUser
    {
        get => _currentUser;
        private set => SetProperty(ref _currentUser, value);
    }

    public TaskItem? SelectedTask
    {
        get => _selectedTask;
        set => SetProperty(ref _selectedTask, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            if (string.IsNullOrWhiteSpace(value))
            {
                _ = LoadTasksAsync();
            }
        }
    }

    public Models.TaskStatus? FilterStatus
    {
        get => _filterStatus;
        set
        {
            SetProperty(ref _filterStatus, value);
            _ = LoadTasksAsync();
        }
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set => SetProperty(ref _isDarkMode, value);
    }

    public bool IsThemeDark => _themeService.IsDarkMode;

    public bool IsAdmin => CurrentUser?.Role == "Admin";
    public bool IsManagerOrAdmin => CurrentUser?.Role is "Admin" or "Manager";

    // Commands
    public IAsyncRelayCommand LoadTasksCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand CreateTaskCommand { get; }
    public IAsyncRelayCommand<TaskItem> EditTaskCommand { get; }
    public IAsyncRelayCommand<TaskItem> DeleteTaskCommand { get; }
    public IAsyncRelayCommand<TaskItem> CompleteTaskCommand { get; }
    public IAsyncRelayCommand<(TaskItem task, User user)> AssignTaskCommand { get; }
    public IAsyncRelayCommand SearchCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public IAsyncRelayCommand LogoutCommand { get; }
    public ICommand ToggleThemeCommand { get; }

    public event EventHandler? LogoutRequested;
    public event EventHandler<TaskItem>? TaskEditRequested;
    public event EventHandler? TaskCreateRequested;

    private async Task LoadInitialDataAsync()
    {
        await LoadUsersAsync();
        await LoadTasksAsync();
    }

    private async Task LoadTasksAsync()
    {
        if (CurrentUser == null) return;

        var tasks = await ExecuteAsync(async () =>
        {
            IEnumerable<TaskItem> allTasks;
            
            if (IsManagerOrAdmin)
            {
                allTasks = await _taskService.GetAllTasksAsync();
            }
            else
            {
                var createdTasks = await _taskService.GetTasksByUserAsync(CurrentUser.Id);
                var assignedTasks = await _taskService.GetTasksAssignedToUserAsync(CurrentUser.Id);
                allTasks = createdTasks.Union(assignedTasks).Distinct();
            }

            // Apply filters
            if (FilterStatus.HasValue)
            {
                allTasks = allTasks.Where(t => t.Status == FilterStatus.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                allTasks = allTasks.Where(t => 
                    t.Title.ToLower().Contains(searchLower) ||
                    t.Description.ToLower().Contains(searchLower) ||
                    t.CreatedByUser.FullName.ToLower().Contains(searchLower) ||
                    (t.AssignedToUser?.FullName.ToLower().Contains(searchLower) ?? false));
            }

            return allTasks.OrderByDescending(t => t.UpdatedAt);
        });

        Tasks.Clear();
        if (tasks != null)
        {
            foreach (var task in tasks)
            {
                Tasks.Add(task);
            }
        }
    }

    private async Task LoadUsersAsync()
    {
        var users = await ExecuteAsync(() => _taskService.GetAvailableUsersAsync());
        
        Users.Clear();
        if (users != null)
        {
            foreach (var user in users)
            {
                Users.Add(user);
            }
        }
    }

    private async Task CreateTaskAsync()
    {
        TaskCreateRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task EditTaskAsync(TaskItem? task)
    {
        if (task != null)
        {
            TaskEditRequested?.Invoke(this, task);
        }
    }

    private async Task DeleteTaskAsync(TaskItem? task)
    {
        if (task == null || CurrentUser == null) return;

        // Only allow deletion by creator, admin, or manager
        if (task.CreatedByUserId != CurrentUser.Id && !IsManagerOrAdmin)
            return;

        var success = await ExecuteAsync(() => _taskService.DeleteTaskAsync(task.Id));
        if (success)
        {
            await LoadTasksAsync();
        }
    }

    private async Task CompleteTaskAsync(TaskItem? task)
    {
        if (task == null || CurrentUser == null) return;

        // Only allow completion by assigned user, creator, admin, or manager
        if (task.AssignedToUserId != CurrentUser.Id && 
            task.CreatedByUserId != CurrentUser.Id && 
            !IsManagerOrAdmin)
            return;

        var success = await ExecuteAsync(() => _taskService.CompleteTaskAsync(task.Id));
        if (success)
        {
            await LoadTasksAsync();
        }
    }

    private async Task AssignTaskAsync((TaskItem task, User user) parameter)
    {
        if (CurrentUser == null || !IsManagerOrAdmin) return;

        var success = await ExecuteAsync(() => _taskService.AssignTaskAsync(parameter.task.Id, parameter.user.Id));
        if (success)
        {
            await LoadTasksAsync();
        }
    }

    private async Task SearchTasksAsync()
    {
        await LoadTasksAsync();
    }

    private void ClearSearch()
    {
        SearchText = string.Empty;
        FilterStatus = null;
    }

    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
    }

    private void OnThemeChanged(object? sender, bool isDarkMode)
    {
        IsDarkMode = isDarkMode;
        OnPropertyChanged(nameof(IsThemeDark));
    }

    private void OnUserChanged(object? sender, User? user)
    {
        CurrentUser = user;
        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(IsManagerOrAdmin));
        
        if (user != null)
        {
            _ = LoadInitialDataAsync();
        }
        else
        {
            Tasks.Clear();
            Users.Clear();
        }
    }

    public async Task RefreshAsync()
    {
        await LoadTasksAsync();
    }
}