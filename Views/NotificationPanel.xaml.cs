using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Admin_Tasks.ViewModels;
using Admin_Tasks.Models;

namespace Admin_Tasks.Views;

/// <summary>
/// UserControl für die Anzeige und Verwaltung von Benachrichtigungen
/// </summary>
public partial class NotificationPanel : UserControl
{
    public NotificationPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Event-Handler für Klick auf eine Benachrichtigung
    /// </summary>
    private void NotificationItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is Notification notification)
        {
            // Benachrichtigung als gelesen markieren wenn ungelesen
            if (!notification.IsRead && DataContext is NotificationPanelViewModel viewModel)
            {
                viewModel.MarkAsReadCommand?.Execute(notification);
            }

            // Wenn die Benachrichtigung mit einer Aufgabe verknüpft ist, navigiere zur Aufgabe
            if (notification.TaskId.HasValue)
            {
                NavigateToTask(notification.TaskId.Value);
            }
        }
    }

    /// <summary>
    /// Navigiert zur entsprechenden Aufgabe
    /// </summary>
    private void NavigateToTask(int taskId)
    {
        // Event für Navigation zur Aufgabe auslösen
        var args = new TaskNavigationEventArgs(taskId);
        TaskNavigationRequested?.Invoke(this, args);
    }

    /// <summary>
    /// Event wird ausgelöst, wenn zur einer Aufgabe navigiert werden soll
    /// </summary>
    public event EventHandler<TaskNavigationEventArgs>? TaskNavigationRequested;
}

/// <summary>
/// EventArgs für Task-Navigation
/// </summary>
public class TaskNavigationEventArgs : EventArgs
{
    public int TaskId { get; }

    public TaskNavigationEventArgs(int taskId)
    {
        TaskId = taskId;
    }
}