using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Admin_Tasks.Models;
using Admin_Tasks.Services;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace Admin_Tasks.ViewModels;

/// <summary>
/// ViewModel für die News-Seite
/// Verwaltet Benachrichtigungen und deren Anzeige
/// </summary>
public class NewsViewModel : BaseViewModel
{
    private readonly INotificationService _notificationService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ITaskService _taskService;
    
    private ObservableCollection<Notification> _allNews;
    private ObservableCollection<Notification> _filteredNews;
    private bool _isLoading;
    private bool _showAllNews = true;
    private bool _showUnreadOnly;
    private bool _showTaskNews;
    private bool _showMessageNews;
    private int _currentUserId;
    private string _searchText = string.Empty;

    public NewsViewModel(
        INotificationService notificationService, 
        IAuthenticationService authenticationService,
        ITaskService taskService)
    {
        _notificationService = notificationService;
        _authenticationService = authenticationService;
        _taskService = taskService;
        
        _allNews = new ObservableCollection<Notification>();
        _filteredNews = new ObservableCollection<Notification>();
        
        InitializeCommands();
        LoadCurrentUser();
        
        // Initial laden
        _ = LoadNewsAsync();
    }

    #region Properties

    /// <summary>
    /// Alle News/Benachrichtigungen
    /// </summary>
    public ObservableCollection<Notification> AllNews
    {
        get => _allNews;
        set => SetProperty(ref _allNews, value);
    }

    /// <summary>
    /// Gefilterte News basierend auf aktuellen Filtereinstellungen
    /// </summary>
    public ObservableCollection<Notification> FilteredNews
    {
        get => _filteredNews;
        set => SetProperty(ref _filteredNews, value);
    }

    /// <summary>
    /// Gibt an, ob gerade Daten geladen werden
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// Filter: Alle News anzeigen
    /// </summary>
    public bool ShowAllNews
    {
        get => _showAllNews;
        set
        {
            if (SetProperty(ref _showAllNews, value) && value)
            {
                ResetOtherFilters();
                ApplyFilter();
            }
        }
    }

    /// <summary>
    /// Filter: Nur ungelesene News anzeigen
    /// </summary>
    public bool ShowUnreadOnly
    {
        get => _showUnreadOnly;
        set
        {
            if (SetProperty(ref _showUnreadOnly, value) && value)
            {
                _showAllNews = false;
                _showTaskNews = false;
                _showMessageNews = false;
                OnPropertyChanged(nameof(ShowAllNews));
                OnPropertyChanged(nameof(ShowTaskNews));
                OnPropertyChanged(nameof(ShowMessageNews));
                ApplyFilter();
            }
        }
    }

    /// <summary>
    /// Filter: Nur Task-bezogene News anzeigen
    /// </summary>
    public bool ShowTaskNews
    {
        get => _showTaskNews;
        set
        {
            if (SetProperty(ref _showTaskNews, value) && value)
            {
                _showAllNews = false;
                _showUnreadOnly = false;
                _showMessageNews = false;
                OnPropertyChanged(nameof(ShowAllNews));
                OnPropertyChanged(nameof(ShowUnreadOnly));
                OnPropertyChanged(nameof(ShowMessageNews));
                ApplyFilter();
            }
        }
    }

    /// <summary>
    /// Filter: Nur Nachrichten-bezogene News anzeigen
    /// </summary>
    public bool ShowMessageNews
    {
        get => _showMessageNews;
        set
        {
            if (SetProperty(ref _showMessageNews, value) && value)
            {
                _showAllNews = false;
                _showUnreadOnly = false;
                _showTaskNews = false;
                OnPropertyChanged(nameof(ShowAllNews));
                OnPropertyChanged(nameof(ShowUnreadOnly));
                OnPropertyChanged(nameof(ShowTaskNews));
                ApplyFilter();
            }
        }
    }

    /// <summary>
    /// Suchtext für Filterung
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    /// <summary>
    /// Gibt an, ob News vorhanden sind
    /// </summary>
    public bool HasNews => FilteredNews?.Count > 0;

    /// <summary>
    /// Gibt an, ob ungelesene News vorhanden sind
    /// </summary>
    public bool HasUnreadNews => AllNews?.Any(n => !n.IsRead) == true;

    /// <summary>
    /// Anzahl ungelesener News
    /// </summary>
    public int UnreadNewsCount => AllNews?.Count(n => !n.IsRead) ?? 0;

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand ShowAllNewsCommand { get; private set; } = null!;
    public ICommand ShowUnreadOnlyCommand { get; private set; } = null!;
    public ICommand ShowTaskNewsCommand { get; private set; } = null!;
    public ICommand ShowMessageNewsCommand { get; private set; } = null!;
    public ICommand MarkAllAsReadCommand { get; private set; } = null!;
    public ICommand MarkAsReadCommand { get; private set; } = null!;
    public ICommand ToggleReadStatusCommand { get; private set; } = null!;
    public ICommand DeleteNotificationCommand { get; private set; } = null!;
    public ICommand HandleNewsItemClickCommand { get; private set; } = null!;
    public ICommand NavigateToTaskCommand { get; private set; } = null!;
    public ICommand ClearSearchCommand { get; private set; } = null!;

    #endregion

    #region Private Methods

    /// <summary>
    /// Initialisiert alle Commands
    /// </summary>
    private void InitializeCommands()
    {
        RefreshCommand = new RelayCommand(async () => await LoadNewsAsync());
        ShowAllNewsCommand = new RelayCommand(() => ShowAllNews = true);
        ShowUnreadOnlyCommand = new RelayCommand(() => ShowUnreadOnly = true);
        ShowTaskNewsCommand = new RelayCommand(() => ShowTaskNews = true);
        ShowMessageNewsCommand = new RelayCommand(() => ShowMessageNews = true);
        MarkAllAsReadCommand = new RelayCommand(async () => await MarkAllAsReadAsync(), () => HasUnreadNews);
        MarkAsReadCommand = new RelayCommand<Notification>(async (notification) => await MarkAsReadAsync(notification));
        ToggleReadStatusCommand = new RelayCommand<Notification>(async (notification) => await ToggleReadStatusAsync(notification));
        DeleteNotificationCommand = new RelayCommand<Notification>(async (notification) => await DeleteNotificationAsync(notification));
        HandleNewsItemClickCommand = new RelayCommand<Notification>(async (notification) => await HandleNewsItemClickAsync(notification));
        NavigateToTaskCommand = new RelayCommand<int>(async (taskId) => await NavigateToTaskAsync(taskId));
        ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
    }

    /// <summary>
    /// Lädt den aktuellen Benutzer
    /// </summary>
    private void LoadCurrentUser()
    {
        var currentUser = _authenticationService.CurrentUser;
        if (currentUser != null)
        {
            _currentUserId = currentUser.Id;
        }
    }

    /// <summary>
    /// Lädt alle News/Benachrichtigungen
    /// </summary>
    private async Task LoadNewsAsync()
    {
        if (_currentUserId == 0) return;

        try
        {
            IsLoading = true;

            // Alle Benachrichtigungen laden (inklusive gelesene)
            var notifications = await _notificationService.GetNotificationsForUserAsync(_currentUserId, includeRead: true, limit: 100);
            
            AllNews.Clear();
            foreach (var notification in notifications.OrderByDescending(n => n.CreatedAt))
            {
                AllNews.Add(notification);
            }

            ApplyFilter();
            
            // Property-Updates für UI
            OnPropertyChanged(nameof(HasNews));
            OnPropertyChanged(nameof(HasUnreadNews));
            OnPropertyChanged(nameof(UnreadNewsCount));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Laden der Benachrichtigungen: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Wendet die aktuellen Filter auf die News an
    /// </summary>
    private void ApplyFilter()
    {
        if (AllNews == null) return;

        var filtered = AllNews.AsEnumerable();

        // Filter anwenden
        if (ShowUnreadOnly)
        {
            filtered = filtered.Where(n => !n.IsRead);
        }
        else if (ShowTaskNews)
        {
            filtered = filtered.Where(n => n.Type == NotificationType.TaskAssigned || 
                                          n.Type == NotificationType.TaskCompleted || 
                                          n.Type == NotificationType.TaskForwarded ||
                                          n.Type == NotificationType.TaskStatusChanged);
        }
        else if (ShowMessageNews)
        {
            filtered = filtered.Where(n => n.Type == NotificationType.TaskCommentAdded);
        }

        // Suchfilter anwenden
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLower();
            filtered = filtered.Where(n => 
                n.Title.ToLower().Contains(searchLower) ||
                n.Message.ToLower().Contains(searchLower) ||
                (n.Task?.Title?.ToLower().Contains(searchLower) == true));
        }

        // Gefilterte Liste aktualisieren
        FilteredNews.Clear();
        foreach (var notification in filtered.OrderByDescending(n => n.CreatedAt))
        {
            FilteredNews.Add(notification);
        }

        OnPropertyChanged(nameof(HasNews));
    }

    /// <summary>
    /// Setzt alle anderen Filter zurück
    /// </summary>
    private void ResetOtherFilters()
    {
        _showUnreadOnly = false;
        _showTaskNews = false;
        _showMessageNews = false;
        OnPropertyChanged(nameof(ShowUnreadOnly));
        OnPropertyChanged(nameof(ShowTaskNews));
        OnPropertyChanged(nameof(ShowMessageNews));
    }

    /// <summary>
    /// Markiert alle Benachrichtigungen als gelesen
    /// </summary>
    private async Task MarkAllAsReadAsync()
    {
        try
        {
            await _notificationService.MarkAllNotificationsAsReadAsync(_currentUserId);
            
            // Lokale Daten aktualisieren
            foreach (var notification in AllNews)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            ApplyFilter();
            OnPropertyChanged(nameof(HasUnreadNews));
            OnPropertyChanged(nameof(UnreadNewsCount));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Markieren der Benachrichtigungen: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Markiert eine Benachrichtigung als gelesen
    /// </summary>
    private async Task MarkAsReadAsync(Notification? notification)
    {
        if (notification == null || notification.IsRead) return;

        try
        {
            await _notificationService.MarkNotificationAsReadAsync(notification.Id);
            
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            ApplyFilter();
            OnPropertyChanged(nameof(HasUnreadNews));
            OnPropertyChanged(nameof(UnreadNewsCount));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Markieren der Benachrichtigung: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Wechselt den Gelesen-Status einer Benachrichtigung
    /// </summary>
    private async Task ToggleReadStatusAsync(Notification? notification)
    {
        if (notification == null) return;

        if (notification.IsRead)
        {
            // Als ungelesen markieren (lokale Änderung)
            notification.IsRead = false;
            notification.ReadAt = null;
        }
        else
        {
            await MarkAsReadAsync(notification);
        }

        ApplyFilter();
        OnPropertyChanged(nameof(HasUnreadNews));
        OnPropertyChanged(nameof(UnreadNewsCount));
    }

    /// <summary>
    /// Löscht eine Benachrichtigung
    /// </summary>
    private async Task DeleteNotificationAsync(Notification? notification)
    {
        if (notification == null) return;

        try
        {
            await _notificationService.DeleteNotificationAsync(notification.Id);
            
            AllNews.Remove(notification);
            FilteredNews.Remove(notification);

            OnPropertyChanged(nameof(HasNews));
            OnPropertyChanged(nameof(HasUnreadNews));
            OnPropertyChanged(nameof(UnreadNewsCount));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Löschen der Benachrichtigung: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Behandelt Klick auf News-Item (OnClick-Event für Benachrichtigungen)
    /// </summary>
    private async Task HandleNewsItemClickAsync(Notification? notification)
    {
        if (notification == null) return;

        try
        {
            // Benachrichtigung als gelesen markieren
            if (!notification.IsRead)
            {
                await MarkAsReadAsync(notification);
            }

            // Detaillierte Benachrichtigungsanzeige
            ShowNotificationDetails(notification);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Verarbeiten der Benachrichtigung: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Zeigt detaillierte Benachrichtigungsinformationen an
    /// </summary>
    private void ShowNotificationDetails(Notification notification)
    {
        var details = $"📋 Benachrichtigungsdetails\n\n";
        details += $"Art: {GetNotificationTypeDisplayText(notification.Type)}\n";
        details += $"Betreff: {notification.Title}\n";
        details += $"Nachricht: {notification.Message}\n";
        
        if (notification.Task != null)
        {
            details += $"Aufgabe: {notification.Task.Title}\n";
        }
        
        details += $"Zeitstempel: {notification.CreatedAt:dd.MM.yyyy HH:mm}\n";
        details += $"Status: {(notification.IsRead ? "Gelesen" : "Ungelesen")}";

        MessageBox.Show(details, "Benachrichtigungsdetails", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// Navigiert zur Task-Detail-Ansicht
    /// </summary>
    private async Task NavigateToTaskAsync(int taskId)
    {
        try
        {
            // Hier würde die Navigation zur Task-Detail-Ansicht implementiert
            // Dies hängt von der spezifischen Navigation-Implementierung ab
            MessageBox.Show($"Navigation zur Aufgabe mit ID {taskId} würde hier implementiert.", "Navigation", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler bei der Navigation: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Konvertiert NotificationType zu Anzeigetext
    /// </summary>
    private string GetNotificationTypeDisplayText(NotificationType type)
    {
        return type switch
        {
            NotificationType.TaskAssigned => "Neuer Task",
            NotificationType.TaskCompleted => "Task abgeschlossen",
            NotificationType.TaskForwarded => "Task weitergeleitet",
            NotificationType.TaskStatusChanged => "Task-Status geändert",
            NotificationType.TaskCommentAdded => "Neue Nachricht",
            NotificationType.TaskDueDateApproaching => "Fälligkeitsdatum nähert sich",
            NotificationType.TaskOverdue => "Task überfällig",
            NotificationType.General => "Allgemeine Benachrichtigung",
            _ => "Unbekannt"
        };
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Aktualisiert die News-Anzeige
    /// </summary>
    public async Task RefreshNewsAsync()
    {
        await LoadNewsAsync();
    }

    /// <summary>
    /// Fügt eine neue Benachrichtigung hinzu (für Echtzeit-Updates)
    /// </summary>
    public void AddNotification(Notification notification)
    {
        if (notification.UserId == _currentUserId)
        {
            AllNews.Insert(0, notification); // Am Anfang einfügen
            ApplyFilter();
            
            OnPropertyChanged(nameof(HasNews));
            OnPropertyChanged(nameof(HasUnreadNews));
            OnPropertyChanged(nameof(UnreadNewsCount));
        }
    }

    #endregion
}