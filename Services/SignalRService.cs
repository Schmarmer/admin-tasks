using System.Collections.ObjectModel;
using System.Windows;
using Admin_Tasks.Models;

namespace Admin_Tasks.Services;

public class SignalRService : IDisposable
{
    private readonly SignalRConnectionManager _connectionManager;
    private bool _disposed = false;

    // Events für UI-Updates
    public event EventHandler<NewCommentEventArgs>? NewCommentReceived;
    public event EventHandler<CommentUpdatedEventArgs>? CommentUpdated;
    public event EventHandler<CommentDeletedEventArgs>? CommentDeleted;
    public event EventHandler<NewNotificationEventArgs>? NewNotificationReceived;
    public event EventHandler<string>? ConnectionStatusChanged;
    public event EventHandler<Exception>? ConnectionError;

    public SignalRService(string hubUrl = "http://localhost:5000/chathub")
    {
        _connectionManager = new SignalRConnectionManager(hubUrl);
        
        // Connection events
        _connectionManager.ConnectionStateChanged += OnConnectionStateChanged;
        _connectionManager.ConnectionError += OnConnectionError;
        
        RegisterSignalREvents();
    }

    public bool IsConnected => _connectionManager.IsConnected;

    public async Task StartAsync(int userId)
    {
        try
        {
            await _connectionManager.StartConnectionAsync(userId);
        }
        catch (Exception ex)
        {
            OnConnectionError(this, ex);
            throw;
        }
    }

    public async Task StopAsync()
    {
        try
        {
            await _connectionManager.StopConnectionAsync();
        }
        catch (Exception ex)
        {
            OnConnectionError(this, ex);
        }
    }

    public async Task JoinTaskChatAsync(int taskId)
    {
        await _connectionManager.JoinTaskGroupAsync(taskId);
    }

    public async Task LeaveTaskChatAsync(int taskId)
    {
        await _connectionManager.LeaveTaskGroupAsync(taskId);
    }

    private void RegisterSignalREvents()
    {
        // Register for new comments
        _connectionManager.RegisterEventHandler("NewComment", async (args) =>
        {
            if (args.Length >= 7)
            {
                var eventArgs = new NewCommentEventArgs
                {
                    CommentId = Convert.ToInt32(args[0]),
                    TaskId = Convert.ToInt32(args[1]),
                    Content = args[2]?.ToString() ?? string.Empty,
                    Type = args[3]?.ToString() ?? string.Empty,
                    CreatedAt = Convert.ToDateTime(args[4]),
                    User = new CommentUser
                    {
                        Id = Convert.ToInt32(args[5]),
                        Username = args[6]?.ToString() ?? string.Empty
                    },
                    ParentCommentId = args.Length > 7 && args[7] != null ? Convert.ToInt32(args[7]) : null
                };

                // Dispatch to UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    NewCommentReceived?.Invoke(this, eventArgs);
                });
            }
        });

        // Register for comment updates
        _connectionManager.RegisterEventHandler("CommentUpdated", async (args) =>
        {
            if (args.Length >= 5)
            {
                var eventArgs = new CommentUpdatedEventArgs
                {
                    CommentId = Convert.ToInt32(args[0]),
                    TaskId = Convert.ToInt32(args[1]),
                    Content = args[2]?.ToString() ?? string.Empty,
                    UpdatedAt = args[3] != null ? Convert.ToDateTime(args[3]) : null,
                    IsEdited = Convert.ToBoolean(args[4])
                };

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    CommentUpdated?.Invoke(this, eventArgs);
                });
            }
        });

        // Register for comment deletions
        _connectionManager.RegisterEventHandler("CommentDeleted", async (args) =>
        {
            if (args.Length >= 2)
            {
                var eventArgs = new CommentDeletedEventArgs
                {
                    CommentId = Convert.ToInt32(args[0]),
                    TaskId = Convert.ToInt32(args[1])
                };

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    CommentDeleted?.Invoke(this, eventArgs);
                });
            }
        });

        // Register for new notifications
        _connectionManager.RegisterEventHandler("NewNotification", async (args) =>
        {
            if (args.Length >= 7)
            {
                var eventArgs = new NewNotificationEventArgs
                {
                    Id = Convert.ToInt32(args[0]),
                    Title = args[1]?.ToString() ?? string.Empty,
                    Message = args[2]?.ToString() ?? string.Empty,
                    Type = args[3]?.ToString() ?? string.Empty,
                    TaskId = args[4] != null ? Convert.ToInt32(args[4]) : null,
                    CreatedAt = Convert.ToDateTime(args[5]),
                    IsRead = Convert.ToBoolean(args[6])
                };

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    NewNotificationReceived?.Invoke(this, eventArgs);
                });
            }
        });
    }

    private void OnConnectionStateChanged(object? sender, string state)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ConnectionStatusChanged?.Invoke(this, state);
        });
    }

    private void OnConnectionError(object? sender, Exception exception)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ConnectionError?.Invoke(this, exception);
        });
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connectionManager?.Dispose();
            _disposed = true;
        }
    }
}

// Extension methods for easier event handling
public static class SignalRServiceExtensions
{
    public static void HandleNewComment(this SignalRService service, Action<NewCommentEventArgs> handler)
    {
        service.NewCommentReceived += (sender, args) => handler(args);
    }

    public static void HandleCommentUpdated(this SignalRService service, Action<CommentUpdatedEventArgs> handler)
    {
        service.CommentUpdated += (sender, args) => handler(args);
    }

    public static void HandleCommentDeleted(this SignalRService service, Action<CommentDeletedEventArgs> handler)
    {
        service.CommentDeleted += (sender, args) => handler(args);
    }

    public static void HandleNewNotification(this SignalRService service, Action<NewNotificationEventArgs> handler)
    {
        service.NewNotificationReceived += (sender, args) => handler(args);
    }

    public static void HandleConnectionStatusChanged(this SignalRService service, Action<string> handler)
    {
        service.ConnectionStatusChanged += (sender, status) => handler(status);
    }

    public static void HandleConnectionError(this SignalRService service, Action<Exception> handler)
    {
        service.ConnectionError += (sender, ex) => handler(ex);
    }
}