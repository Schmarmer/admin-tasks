using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Admin_Tasks.Models;
using Admin_Tasks.Services;
using CommunityToolkit.Mvvm.Input;

namespace Admin_Tasks.ViewModels;

/// <summary>
/// ViewModel für das Notification-Panel
/// </summary>
public class NotificationPanelViewModel : INotifyPropertyChanged
{
    private readonly INotificationService _notificationService;
    private readonly IAuthenticationService _authenticationService;
    private ObservableCollection<Notification> _notifications;
    private ObservableCollection<Notification> _filteredNotifications;
    private bool _isLoading;
    private bool _showAllNotifications = true;
    private bool _showUnreadOnly;
    private bool _showTaskNotifications;
    private int _currentUserId;

    public NotificationPanelViewModel(INotificationService notificationService, IAuthenticationService authenticationService)
    {
        _notificationService = notificationService;
        _authenticationService = authenticationService;
        _notifications = new ObservableCollection<Notification>();
        _filteredNotifications = new ObservableCollection<Notification>();
        
        // Commands initialisieren
        MarkAllAsReadCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(async () => await MarkAllAsReadAsync(), () => HasUnreadNotifications);
        MarkAsReadCommand = new CommunityToolkit.Mvvm.Input.RelayCommand<Notification>(async (notification) => await MarkAsReadAsync(notification));
        ToggleReadStatusCommand = new CommunityToolkit.Mvvm.Input.RelayCommand<Notification>(async (notification) => await ToggleReadStatusAsync(notification));
        DeleteNotificationCommand = new CommunityToolkit.Mvvm.Input.RelayCommand<Notification>(async (notification) => await DeleteNotificationAsync(notification));
        
        // Aktuellen Benutzer laden
        LoadCurrentUser();
        
        // Benachrichtigungen laden
        _ = LoadNotificationsAsync();
    }

    #region Properties

    /// <summary>
    /// Alle Benachrichtigungen
    /// </summary>
    public ObservableCollection<Notification> Notifications
    {
        get => _notifications;
        set => SetProperty(ref _notifications, value);
    }

    /// <summary>
    /// Gefilterte Benachrichtigungen basierend auf den aktuellen Filtereinstellungen
    /// </summary>
    public ObservableCollection<Notification> FilteredNotifications
    {
        get => _filteredNotifications;
        set => SetProperty(ref _filteredNotifications, value);
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
    /// Filter: Alle Benachrichtigungen anzeigen
    /// </summary>
    public bool ShowAllNotifications
    {
        get => _showAllNotifications;
        set
        {
            if (SetProperty(ref _showAllNotifications, value) && value)
            {
                ShowUnreadOnly = false;
                ShowTaskNotifications = false;
                ApplyFilter();
            }
        }
    }

    /// <summary>
    /// Filter: Nur ungelesene Benachrichtigungen anzeigen
    /// </summary>
    public bool ShowUnreadOnly
    {
        get => _showUnreadOnly;
        set
        {
            if (SetProperty(ref _showUnreadOnly, value) && value)
            {
                ShowAllNotifications = false;
                ShowTaskNotifications = false;
                ApplyFilter();
            }
        }
    }

    /// <summary>
    /// Filter: Nur aufgabenbezogene Benachrichtigungen anzeigen
    /// </summary>
    public bool ShowTaskNotifications
    {
        get => _showTaskNotifications;
        set
        {
            if (SetProperty(ref _showTaskNotifications, value) && value)
            {
                ShowAllNotifications = false;
                ShowUnreadOnly = false;
                ApplyFilter();
            }
        }
    }

    /// <summary>
    /// Anzahl ungelesener Benachrichtigungen
    /// </summary>
    public int UnreadCount => Notifications.Count(n => !n.IsRead);

    /// <summary>
    /// Gibt an, ob ungelesene Benachrichtigungen vorhanden sind
    /// </summary>
    public bool HasUnreadNotifications => UnreadCount > 0;

    /// <summary>
    /// Gibt an, ob Benachrichtigungen vorhanden sind
    /// </summary>
    public bool HasNotifications => FilteredNotifications.Count > 0;

    /// <summary>
    /// Gibt an, ob keine Benachrichtigungen vorhanden sind
    /// </summary>
    public bool IsEmpty => !IsLoading && FilteredNotifications.Count == 0;

    #endregion

    #region Commands

    /// <summary>
    /// Command zum Markieren aller Benachrichtigungen als gelesen
    /// </summary>
    public CommunityToolkit.Mvvm.Input.IRelayCommand MarkAllAsReadCommand { get; }

    /// <summary>
    /// Command zum Markieren einer Benachrichtigung als gelesen
    /// </summary>
    public CommunityToolkit.Mvvm.Input.IRelayCommand<Notification> MarkAsReadCommand { get; }

    /// <summary>
    /// Command zum Umschalten des Gelesen-Status einer Benachrichtigung
    /// </summary>
    public CommunityToolkit.Mvvm.Input.IRelayCommand<Notification> ToggleReadStatusCommand { get; }

    /// <summary>
    /// Command zum Löschen einer Benachrichtigung
    /// </summary>
    public CommunityToolkit.Mvvm.Input.IRelayCommand<Notification> DeleteNotificationCommand { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Lädt die Benachrichtigungen für den aktuellen Benutzer
    /// </summary>
    public async Task LoadNotificationsAsync()
    {
        if (_currentUserId == 0) return;

        IsLoading = true;
        try
        {
            var notifications = await _notificationService.GetNotificationsForUserAsync(_currentUserId, includeRead: true, limit: 100);
            
            Notifications.Clear();
            foreach (var notification in notifications)
            {
                Notifications.Add(notification);
            }
            
            ApplyFilter();
            NotifyCountProperties();
        }
        catch (Exception ex)
        {
            // Fehlerbehandlung - könnte durch ein Event oder Logging-Service erweitert werden
            System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Benachrichtigungen: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Markiert alle Benachrichtigungen als gelesen
    /// </summary>
    private async Task MarkAllAsReadAsync()
    {
        if (_currentUserId == 0) return;

        try
        {
            await _notificationService.MarkAllNotificationsAsReadAsync(_currentUserId);
            
            // Lokale Aktualisierung
            foreach (var notification in Notifications.Where(n => !n.IsRead))
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }
            
            NotifyCountProperties();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Markieren aller Benachrichtigungen als gelesen: {ex.Message}");
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
            
            // Lokale Aktualisierung
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            
            NotifyCountProperties();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Markieren der Benachrichtigung als gelesen: {ex.Message}");
        }
    }

    /// <summary>
    /// Schaltet den Gelesen-Status einer Benachrichtigung um
    /// </summary>
    private async Task ToggleReadStatusAsync(Notification? notification)
    {
        if (notification == null) return;

        if (notification.IsRead)
        {
            // Aktuell gibt es keine "Als ungelesen markieren" Funktion im Service
            // Dies könnte bei Bedarf erweitert werden
        }
        else
        {
            await MarkAsReadAsync(notification);
        }
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
            
            // Lokale Entfernung
            Notifications.Remove(notification);
            ApplyFilter();
            NotifyCountProperties();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Löschen der Benachrichtigung: {ex.Message}");
        }
    }

    /// <summary>
    /// Wendet den aktuellen Filter auf die Benachrichtigungen an
    /// </summary>
    private void ApplyFilter()
    {
        FilteredNotifications.Clear();
        
        IEnumerable<Notification> filtered = Notifications;
        
        if (ShowUnreadOnly)
        {
            filtered = filtered.Where(n => !n.IsRead);
        }
        else if (ShowTaskNotifications)
        {
            filtered = filtered.Where(n => n.TaskId.HasValue);
        }
        // ShowAllNotifications ist der Standard, keine weitere Filterung nötig
        
        foreach (var notification in filtered.OrderByDescending(n => n.CreatedAt))
        {
            FilteredNotifications.Add(notification);
        }
        
        OnPropertyChanged(nameof(HasNotifications));
        OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>
    /// Lädt den aktuellen Benutzer
    /// </summary>
    private void LoadCurrentUser()
    {
        var currentUser = _authenticationService.CurrentUser;
        _currentUserId = currentUser?.Id ?? 0;
    }

    /// <summary>
    /// Benachrichtigt über Änderungen der Count-Properties
    /// </summary>
    private void NotifyCountProperties()
    {
        OnPropertyChanged(nameof(UnreadCount));
        OnPropertyChanged(nameof(HasUnreadNotifications));
        OnPropertyChanged(nameof(HasNotifications));
        OnPropertyChanged(nameof(IsEmpty));
        
        // Command CanExecute aktualisieren
        ((CommunityToolkit.Mvvm.Input.RelayCommand)MarkAllAsReadCommand).NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Fügt eine neue Benachrichtigung hinzu (für Real-Time Updates)
    /// </summary>
    public void AddNotification(Notification notification)
    {
        if (notification.UserId == _currentUserId)
        {
            Notifications.Insert(0, notification); // Am Anfang einfügen (neueste zuerst)
            ApplyFilter();
            NotifyCountProperties();
        }
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}