using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Admin_Tasks.Services;
using Admin_Tasks.Models;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Admin_Tasks.ViewModels;

public class MainViewModel : BaseViewModel, IDisposable
{
    private readonly IAuthenticationService _authService;
    private readonly ITaskService _taskService;
    private readonly IThemeService _themeService;
    private readonly ICustomFolderService _customFolderService;
    private User? _currentUser;
    private TaskItem? _selectedTask;
    private string _searchText = string.Empty;
    private Models.TaskStatus? _filterStatus;
    private bool _isDarkMode;
    private bool _isRefreshing;
    private string? _selectedFolder;
    private CustomFolder? _selectedCustomFolder;
    private ObservableCollection<TaskItem> _filteredTasks;
    private string _selectedTaskDetails = string.Empty;

    public MainViewModel(IAuthenticationService authService, ITaskService taskService, IThemeService themeService, ICustomFolderService customFolderService)
    {
        _authService = authService;
        _taskService = taskService;
        _themeService = themeService;
        _customFolderService = customFolderService;
        
        Title = "Admin Tasks - Aufgabenverwaltung";
        
        Tasks = new ObservableCollection<TaskItem>();
        Users = new ObservableCollection<User>();
        _filteredTasks = new ObservableCollection<TaskItem>();
        Folders = new ObservableCollection<string> { "Alle Aufgaben", "Erstellte Aufgaben", "Zugewiesene Aufgaben", "Ohne Besitzer", "Abgeschlossene Aufgaben", "Offene Aufgaben" };
        CustomFolders = new ObservableCollection<CustomFolder>();
        SelectedFolder = "Alle Aufgaben";
        
        // Subscribe to Tasks collection changes to update count properties
        Tasks.CollectionChanged += (s, e) => NotifyTaskCountsChanged();
        
        // Commands
        LoadTasksCommand = new AsyncRelayCommand(LoadTasksAsync);
        RefreshCommand = new AsyncRelayCommand(LoadTasksAsync); // Alias für LoadTasksCommand
        CreateTaskCommand = new AsyncRelayCommand(CreateTaskAsync);
        EditTaskCommand = new AsyncRelayCommand<TaskItem>(EditTaskAsync);
        DeleteTaskCommand = new AsyncRelayCommand<TaskItem>(DeleteTaskAsync);
        CompleteTaskCommand = new AsyncRelayCommand<TaskItem>(CompleteTaskAsync);
        AssignTaskCommand = new AsyncRelayCommand<(TaskItem task, User user)>(AssignTaskAsync);
        SelfAssignTaskCommand = new AsyncRelayCommand<TaskItem>(SelfAssignTaskAsync);
        SearchCommand = new AsyncRelayCommand(SearchTasksAsync);
        ClearSearchCommand = new RelayCommand(ClearSearch);
        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        DisableAutoLoginCommand = new AsyncRelayCommand(DisableAutoLoginAsync);
        ManageUsersCommand = new AsyncRelayCommand(ManageUsersAsync);
        CreateFolderCommand = new AsyncRelayCommand<string>(CreateFolderAsync);
        CreateCustomFolderCommand = new AsyncRelayCommand<CustomFolder>(CreateCustomFolderAsync);
        DeleteFolderCommand = new AsyncRelayCommand<CustomFolder>(DeleteFolderAsync);
        AddTaskToFolderCommand = new AsyncRelayCommand<(CustomFolder folder, TaskItem task)>(AddTaskToFolderAsync);
        RemoveTaskFromFolderCommand = new AsyncRelayCommand<(CustomFolder folder, TaskItem task)>(RemoveTaskFromFolderAsync);
        OpenChatOverviewCommand = new RelayCommand(OpenChatOverview);
        
        // Subscribe to authentication changes
        _authService.UserChanged += OnUserChanged;
        _themeService.ThemeChanged += OnThemeChanged;
        _customFolderService.FolderStructureChanged += OnFolderStructureChanged;
        CurrentUser = _authService.CurrentUser;
        
        // Theme-Status synchronisieren
        IsDarkMode = _themeService.IsDarkMode;
        
        // Load initial data
        _ = LoadInitialDataAsync();
    }

    public void Dispose()
    {
        // Unsubscribe from events to prevent memory leaks
        _authService.UserChanged -= OnUserChanged;
        _themeService.ThemeChanged -= OnThemeChanged;
        _customFolderService.FolderStructureChanged -= OnFolderStructureChanged;
    }

    public ObservableCollection<TaskItem> Tasks { get; }
    public ObservableCollection<User> Users { get; }
    public ObservableCollection<string> Folders { get; }
    public ObservableCollection<CustomFolder> CustomFolders { get; }
    
    // Task count properties for standard folders
    public int AllTasksCount => Tasks?.Count ?? 0;
    public int CreatedTasksCount => Tasks?.Count(t => t.CreatedByUserId == CurrentUser?.Id) ?? 0;
    public int AssignedTasksCount => Tasks?.Count(t => t.AssignedToUserId == CurrentUser?.Id && t.AssignedToUserId != null) ?? 0;
    public int UnassignedTasksCount => Tasks?.Count(t => t.AssignedToUserId == null) ?? 0;
    public int CompletedTasksCount => Tasks?.Count(t => t.Status == Models.TaskStatus.Completed) ?? 0;
    public int OpenTasksCount => Tasks?.Count(t => t.Status != Models.TaskStatus.Completed) ?? 0;
    
    private void NotifyTaskCountsChanged()
    {
        OnPropertyChanged(nameof(AllTasksCount));
        OnPropertyChanged(nameof(CreatedTasksCount));
        OnPropertyChanged(nameof(AssignedTasksCount));
        OnPropertyChanged(nameof(UnassignedTasksCount));
        OnPropertyChanged(nameof(CompletedTasksCount));
        OnPropertyChanged(nameof(OpenTasksCount));
    }
    
    public ObservableCollection<TaskItem> FilteredTasks
    {
        get => _filteredTasks;
        private set => SetProperty(ref _filteredTasks, value);
    }

    public User? CurrentUser
    {
        get => _currentUser;
        private set => SetProperty(ref _currentUser, value);
    }

    public TaskItem? SelectedTask
    {
        get => _selectedTask;
        set
        {
            SetProperty(ref _selectedTask, value);
            UpdateTaskDetails();
        }
    }
    
    public string? SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            SetProperty(ref _selectedFolder, value);
            // Wenn ein Standard-Ordner gewählt wurde, benutzerdefinierte Auswahl zurücksetzen
            if (value != null)
            {
                SelectedCustomFolder = null;
            }
            _ = ApplyFolderFilterAsync();
        }
    }
    
    public CustomFolder? SelectedCustomFolder
    {
        get => _selectedCustomFolder;
        set
        {
            SetProperty(ref _selectedCustomFolder, value);
            // Bei Auswahl eines benutzerdefinierten Ordners Standard-Ordner-Auswahl aufheben
            if (value != null)
            {
                SelectedFolder = null;
            }
            _ = ApplyFolderFilterAsync();
        }
    }
    
    public string SelectedTaskDetails
    {
        get => _selectedTaskDetails;
        private set => SetProperty(ref _selectedTaskDetails, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            _ = ApplyFolderFilterAsync();
        }
    }

    public Models.TaskStatus? FilterStatus
    {
        get => _filterStatus;
        set
        {
            SetProperty(ref _filterStatus, value);
            _ = ApplyFolderFilterAsync();
        }
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set => SetProperty(ref _isDarkMode, value);
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
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
    public IAsyncRelayCommand<TaskItem> SelfAssignTaskCommand { get; }
    public IAsyncRelayCommand SearchCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public IAsyncRelayCommand LogoutCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public IAsyncRelayCommand DisableAutoLoginCommand { get; }
    public IAsyncRelayCommand ManageUsersCommand { get; }
    public IAsyncRelayCommand<string> CreateFolderCommand { get; }
    public IAsyncRelayCommand<CustomFolder> CreateCustomFolderCommand { get; }
    public IAsyncRelayCommand<CustomFolder> DeleteFolderCommand { get; }
    public IAsyncRelayCommand<(CustomFolder folder, TaskItem task)> AddTaskToFolderCommand { get; }
    public IAsyncRelayCommand<(CustomFolder folder, TaskItem task)> RemoveTaskFromFolderCommand { get; }
    public ICommand OpenChatOverviewCommand { get; }

    public event EventHandler? LogoutRequested;
    public event EventHandler<TaskItem>? TaskEditRequested;
    public event EventHandler? TaskCreateRequested;
    public event EventHandler? ChatOverviewRequested;

    private async Task LoadInitialDataAsync()
    {
        await LoadUsersAsync();
        await LoadCustomFoldersAsync();
        await LoadTasksAsync();
    }

    private async Task LoadTasksAsync()
    {
        if (CurrentUser == null) return;

        IsRefreshing = true;
        try
        {
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
                    // Subscribe to PropertyChanged events for individual tasks
                    task.PropertyChanged += (s, e) => {
                        if (e.PropertyName == nameof(TaskItem.Status) || 
                            e.PropertyName == nameof(TaskItem.AssignedToUserId) ||
                            e.PropertyName == nameof(TaskItem.CreatedByUserId))
                        {
                            NotifyTaskCountsChanged();
                        }
                    };
                    Tasks.Add(task);
                }
            }
            
            // Notify that task counts have changed
            NotifyTaskCountsChanged();
            
            // Apply folder filter after loading tasks
            await ApplyFolderFilterAsync();
        }
        finally
        {
            IsRefreshing = false;
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

    private async Task SelfAssignTaskAsync(TaskItem? task)
    {
        if (task == null || CurrentUser == null) 
        {
            System.Diagnostics.Debug.WriteLine("[SelfAssignTaskAsync] Task oder CurrentUser ist null");
            return;
        }
        
        // Only allow self-assign if task has no owner
        if (task.AssignedToUserId != null) 
        {
            System.Diagnostics.Debug.WriteLine($"[SelfAssignTaskAsync] Task {task.Id} hat bereits einen Besitzer: {task.AssignedToUserId}");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[SelfAssignTaskAsync] Versuche Task {task.Id} an Benutzer {CurrentUser.Id} ({CurrentUser.Username}) zuzuweisen");
        
        var success = await ExecuteAsync(() => _taskService.AssignTaskAsync(task.Id, CurrentUser.Id));
        
        System.Diagnostics.Debug.WriteLine($"[SelfAssignTaskAsync] AssignTaskAsync Ergebnis: {success}");
        
        if (success)
        {
            System.Diagnostics.Debug.WriteLine("[SelfAssignTaskAsync] Lade Tasks neu...");
            await LoadTasksAsync();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[SelfAssignTaskAsync] Zuweisung fehlgeschlagen!");
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
            NotifyTaskCountsChanged();
        }
    }

    public async Task RefreshAsync()
    {
        await LoadTasksAsync();
    }
    
    private async Task ApplyFolderFilterAsync()
    {
        if (CurrentUser == null)
        {
            FilteredTasks.Clear();
            return;
        }
        
        IEnumerable<TaskItem> filteredTasks;
        if (SelectedCustomFolder != null)
        {
            // Filter nach benutzerdefiniertem Ordner
            filteredTasks = Tasks.Where(t => SelectedCustomFolder.ContainsTask(t.Id));
        }
        else
        {
            filteredTasks = SelectedFolder switch
            {
                "Erstellte Aufgaben" => Tasks.Where(t => t.CreatedByUserId == CurrentUser.Id),
                "Zugewiesene Aufgaben" => Tasks.Where(t => t.AssignedToUserId == CurrentUser.Id && t.AssignedToUserId != null),
                "Ohne Besitzer" => Tasks.Where(t => t.AssignedToUserId == null),
                "Abgeschlossene Aufgaben" => Tasks.Where(t => t.Status == Models.TaskStatus.Completed),
                "Offene Aufgaben" => Tasks.Where(t => t.Status != Models.TaskStatus.Completed),
                _ => Tasks // "Alle Aufgaben" or default
            };
        }
        
        // Apply additional search and status filters
        if (FilterStatus.HasValue)
        {
            filteredTasks = filteredTasks.Where(t => t.Status == FilterStatus.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLower();
            filteredTasks = filteredTasks.Where(t => 
                t.Title.ToLower().Contains(searchLower) ||
                t.Description.ToLower().Contains(searchLower) ||
                t.CreatedByUser.FullName.ToLower().Contains(searchLower) ||
                (t.AssignedToUser?.FullName.ToLower().Contains(searchLower) ?? false));
        }
        
        FilteredTasks.Clear();
        foreach (var task in filteredTasks.OrderByDescending(t => t.UpdatedAt))
        {
            FilteredTasks.Add(task);
        }
    }
    
    private void UpdateTaskDetails()
    {
        if (SelectedTask == null)
        {
            SelectedTaskDetails = "Keine Aufgabe ausgewählt.";
            return;
        }
        
        var task = SelectedTask;
        var details = $"Titel: {task.Title}\n\n" +
                     $"Beschreibung: {task.Description}\n\n" +
                     $"Status: {GetStatusDisplayName(task.Status)}\n" +
                     $"Priorität: {GetPriorityDisplayName(task.Priority)}\n\n" +
                     $"Erstellt von: {task.CreatedByUser.FullName}\n" +
                     $"Erstellt am: {task.CreatedAt:dd.MM.yyyy HH:mm}\n\n";
        
        if (task.AssignedToUser != null)
        {
            details += $"Zugewiesen an: {task.AssignedToUser.FullName}\n";
        }
        
        if (task.DueDate.HasValue)
        {
            details += $"Fälligkeitsdatum: {task.DueDate.Value:dd.MM.yyyy}\n";
        }
        
        details += $"Zuletzt aktualisiert: {task.UpdatedAt:dd.MM.yyyy HH:mm}";
        
        SelectedTaskDetails = details;
    }
    
    private void OpenChatOverview()
    {
        ChatOverviewRequested?.Invoke(this, EventArgs.Empty);
    }
    
    private static string GetStatusDisplayName(Models.TaskStatus status)
    {
        return status switch
        {
            Models.TaskStatus.Open => "Offen",
            Models.TaskStatus.InProgress => "In Bearbeitung",
            Models.TaskStatus.Completed => "Abgeschlossen",
            Models.TaskStatus.Cancelled => "Abgebrochen",
            _ => status.ToString()
        };
    }
    
    private static string GetPriorityDisplayName(Models.TaskPriority priority)
    {
        return priority switch
        {
            Models.TaskPriority.Low => "Niedrig",
            Models.TaskPriority.Medium => "Mittel",
            Models.TaskPriority.High => "Hoch",
            Models.TaskPriority.Critical => "Kritisch",
            _ => priority.ToString()
        };
    }

    private async Task LoadCustomFoldersAsync()
    {
        try
        {
            var folders = await _customFolderService.GetAllFoldersAsync();
            
            CustomFolders.Clear();
            foreach (var folder in folders)
            {
                CustomFolders.Add(folder);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash the application
            System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der benutzerdefinierten Ordner: {ex.Message}");
        }
    }

    private async Task CreateFolderAsync(string? folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return;

        try
        {
            var folder = await _customFolderService.CreateFolderAsync(folderName.Trim());
            if (folder != null)
            {
                CustomFolders.Add(folder);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Erstellen des Ordners: {ex.Message}");
        }
    }

    private async Task CreateCustomFolderAsync(CustomFolder? customFolder)
    {
        if (customFolder == null || string.IsNullOrWhiteSpace(customFolder.Name))
            return;

        try
        {
            var folder = await _customFolderService.CreateCustomFolderAsync(customFolder);
            if (folder != null)
            {
                CustomFolders.Add(folder);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Erstellen des benutzerdefinierten Ordners: {ex.Message}");
        }
    }

    private async Task DeleteFolderAsync(CustomFolder? folder)
    {
        if (folder == null)
            return;

        try
        {
            var success = await _customFolderService.DeleteFolderAsync(folder.Id);
            if (success)
            {
                CustomFolders.Remove(folder);
                
                // Falls der gelöschte Ordner ausgewählt war, auf "Alle Aufgaben" wechseln
                if (SelectedFolder == folder.Name)
                {
                    SelectedFolder = "Alle Aufgaben";
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Löschen des Ordners: {ex.Message}");
        }
    }

    private async Task AddTaskToFolderAsync((CustomFolder folder, TaskItem task) parameter)
    {
        try
        {
            var success = await _customFolderService.AddTaskToFolderAsync(parameter.folder.Id, parameter.task.Id);
            if (success)
            {
                // Ordner in der UI aktualisieren
                var folderIndex = CustomFolders.IndexOf(parameter.folder);
                if (folderIndex >= 0)
                {
                    CustomFolders[folderIndex] = await _customFolderService.GetFolderAsync(parameter.folder.Id) ?? parameter.folder;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Hinzufügen der Task zum Ordner: {ex.Message}");
        }
    }

    private async Task RemoveTaskFromFolderAsync((CustomFolder folder, TaskItem task) parameter)
    {
        try
        {
            var success = await _customFolderService.RemoveTaskFromFolderAsync(parameter.folder.Id, parameter.task.Id);
            if (success)
            {
                // Ordner in der UI aktualisieren
                var folderIndex = CustomFolders.IndexOf(parameter.folder);
                if (folderIndex >= 0)
                {
                    CustomFolders[folderIndex] = await _customFolderService.GetFolderAsync(parameter.folder.Id) ?? parameter.folder;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Entfernen der Task aus dem Ordner: {ex.Message}");
        }
    }

    private void OnFolderStructureChanged(object? sender, FolderStructure structure)
    {
        // Ordner-Liste aktualisieren wenn sich die Struktur ändert
        _ = LoadCustomFoldersAsync();
    }

    /// <summary>
    /// Filtert Tasks basierend auf dem ausgewählten benutzerdefinierten Ordner
    /// </summary>
    /// <param name="folder">Der ausgewählte Ordner</param>
    /// <returns>Gefilterte Tasks</returns>
    public async Task<IEnumerable<TaskItem>> GetTasksForCustomFolderAsync(CustomFolder folder)
    {
        return Tasks.Where(t => folder.ContainsTask(t.Id));
    }

    /// <summary>
    /// Prüft ob eine Task einem benutzerdefinierten Ordner zugeordnet ist
    /// </summary>
    /// <param name="taskId">ID der Task</param>
    /// <returns>Liste der Ordner, die die Task enthalten</returns>
    public async Task<List<CustomFolder>> GetFoldersForTaskAsync(int taskId)
    {
        try
        {
            return await _customFolderService.GetFoldersContainingTaskAsync(taskId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Suchen von Ordnern für Task: {ex.Message}");
            return new List<CustomFolder>();
        }
    }
    
    private async Task DisableAutoLoginAsync()
    {
        try
        {
            // SettingsService verwenden, um Auto-Login zu deaktivieren
            var settingsService = App.ServiceProvider.GetRequiredService<ISettingsService>();
            
            // Die richtigen Schlüssel verwenden, die auch in LoginViewModel verwendet werden
            await settingsService.SetSettingAsync("rememberMe", false);
            await settingsService.SetSettingAsync("rememberUsername", "");
            await settingsService.SetSettingAsync("rememberPassword", "");
            
            // Alte/falsche Schlüssel entfernen falls vorhanden
            await settingsService.RemoveSettingAsync("RememberMe");
            await settingsService.RemoveSettingAsync("SavedUsername");
            await settingsService.RemoveSettingAsync("SavedPassword");
            
            MessageBox.Show(
                "Auto-Login wurde erfolgreich deaktiviert. Bei der nächsten Anmeldung müssen Sie Ihre Daten erneut eingeben.",
                "Auto-Login deaktiviert",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Deaktivieren des Auto-Logins: {ex.Message}");
            MessageBox.Show(
                "Fehler beim Deaktivieren des Auto-Logins. Bitte versuchen Sie es erneut.",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    
    private async Task ManageUsersAsync()
    {
        try
        {
            // Überprüfen, ob der aktuelle Benutzer Admin-Rechte hat
            if (!IsAdmin)
            {
                MessageBox.Show(
                    "Sie haben keine Berechtigung, Benutzer zu verwalten. Nur Administratoren können diese Funktion verwenden.",
                    "Keine Berechtigung",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            
            // UserManagementViewModel und View erstellen
            var userManagementViewModel = App.ServiceProvider?.GetService<UserManagementViewModel>();
            if (userManagementViewModel == null)
            {
                MessageBox.Show(
                    "Fehler beim Laden der Benutzerverwaltung. Service nicht verfügbar.",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            
            var userManagementView = new Admin_Tasks.Views.UserManagementView(userManagementViewModel)
            {
                Owner = Application.Current.MainWindow
            };
            
            userManagementView.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Öffnen der Benutzerverwaltung: {ex.Message}");
            MessageBox.Show(
                "Fehler beim Öffnen der Benutzerverwaltung. Bitte versuchen Sie es erneut.",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}