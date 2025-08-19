using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;

namespace Admin_Tasks.Services;

public class SignalRConnectionManager : IDisposable
{
    private HubConnection? _connection;
    private readonly ConcurrentDictionary<string, List<Func<object[], Task>>> _eventHandlers = new();
    private bool _disposed = false;
    private readonly string _hubUrl;
    private int _currentUserId;
    private readonly List<int> _joinedTaskGroups = new();

    public SignalRConnectionManager(string hubUrl = "http://localhost:5000/chathub")
    {
        _hubUrl = hubUrl;
    }

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public event EventHandler<string>? ConnectionStateChanged;
    public event EventHandler<Exception>? ConnectionError;

    public async Task StartConnectionAsync(int userId)
    {
        if (_connection != null)
        {
            await StopConnectionAsync();
        }

        _currentUserId = userId;

        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect()
            .Build();

        // Setup connection event handlers
        _connection.Closed += OnConnectionClosed;
        _connection.Reconnecting += OnReconnecting;
        _connection.Reconnected += OnReconnected;

        // Register all event handlers
        RegisterEventHandlers();

        try
        {
            await _connection.StartAsync();
            
            // Join user group for notifications
            await JoinUserGroupAsync(_currentUserId);
            
            ConnectionStateChanged?.Invoke(this, "Connected");
        }
        catch (Exception ex)
        {
            ConnectionError?.Invoke(this, ex);
            throw;
        }
    }

    public async Task StopConnectionAsync()
    {
        if (_connection != null)
        {
            try
            {
                // Leave all joined groups
                await LeaveAllGroupsAsync();
                
                await _connection.StopAsync();
                await _connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                ConnectionError?.Invoke(this, ex);
            }
            finally
            {
                _connection = null;
                ConnectionStateChanged?.Invoke(this, "Disconnected");
            }
        }
    }

    public async Task JoinTaskGroupAsync(int taskId)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _connection.InvokeAsync("JoinTaskGroup", taskId);
                
                if (!_joinedTaskGroups.Contains(taskId))
                {
                    _joinedTaskGroups.Add(taskId);
                }
            }
            catch (Exception ex)
            {
                ConnectionError?.Invoke(this, ex);
            }
        }
    }

    public async Task LeaveTaskGroupAsync(int taskId)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _connection.InvokeAsync("LeaveTaskGroup", taskId);
                _joinedTaskGroups.Remove(taskId);
            }
            catch (Exception ex)
            {
                ConnectionError?.Invoke(this, ex);
            }
        }
    }

    public async Task JoinUserGroupAsync(int userId)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _connection.InvokeAsync("JoinUserGroup", userId);
            }
            catch (Exception ex)
            {
                ConnectionError?.Invoke(this, ex);
            }
        }
    }

    public async Task LeaveUserGroupAsync(int userId)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _connection.InvokeAsync("LeaveUserGroup", userId);
            }
            catch (Exception ex)
            {
                ConnectionError?.Invoke(this, ex);
            }
        }
    }

    private async Task LeaveAllGroupsAsync()
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            // Leave all task groups
            var tasks = _joinedTaskGroups.Select(taskId => LeaveTaskGroupAsync(taskId));
            await Task.WhenAll(tasks);
            
            // Leave user group
            await LeaveUserGroupAsync(_currentUserId);
        }
        
        _joinedTaskGroups.Clear();
    }

    public void RegisterEventHandler(string eventName, Func<object[], Task> handler)
    {
        _eventHandlers.AddOrUpdate(eventName, 
            new List<Func<object[], Task>> { handler },
            (key, existingHandlers) => 
            {
                existingHandlers.Add(handler);
                return existingHandlers;
            });

        // If connection is already established, register the handler immediately
        if (_connection != null)
        {
            _connection.On(eventName, handler);
        }
    }

    public void UnregisterEventHandler(string eventName)
    {
        _eventHandlers.TryRemove(eventName, out _);
        
        if (_connection != null)
        {
            _connection.Remove(eventName);
        }
    }

    private void RegisterEventHandlers()
    {
        if (_connection == null) return;

        foreach (var kvp in _eventHandlers)
        {
            foreach (var handler in kvp.Value)
            {
                _connection.On(kvp.Key, handler);
            }
        }
    }

    private async Task OnConnectionClosed(Exception? exception)
    {
        ConnectionStateChanged?.Invoke(this, "Disconnected");
        
        if (exception != null)
        {
            ConnectionError?.Invoke(this, exception);
        }
        
        await Task.CompletedTask;
    }

    private async Task OnReconnecting(Exception? exception)
    {
        ConnectionStateChanged?.Invoke(this, "Reconnecting");
        
        if (exception != null)
        {
            ConnectionError?.Invoke(this, exception);
        }
        
        await Task.CompletedTask;
    }

    private async Task OnReconnected(string? connectionId)
    {
        ConnectionStateChanged?.Invoke(this, "Reconnected");
        
        // Rejoin all groups after reconnection
        try
        {
            await JoinUserGroupAsync(_currentUserId);
            
            var rejoinTasks = _joinedTaskGroups.Select(taskId => JoinTaskGroupAsync(taskId));
            await Task.WhenAll(rejoinTasks);
        }
        catch (Exception ex)
        {
            ConnectionError?.Invoke(this, ex);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Task.Run(async () => await StopConnectionAsync()).Wait(5000);
            _disposed = true;
        }
    }
}

// Event argument classes for strongly typed events
public class NewCommentEventArgs : EventArgs
{
    public int CommentId { get; set; }
    public int TaskId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public CommentUser User { get; set; } = new();
    public int? ParentCommentId { get; set; }
}

public class CommentUpdatedEventArgs : EventArgs
{
    public int CommentId { get; set; }
    public int TaskId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public bool IsEdited { get; set; }
}

public class CommentDeletedEventArgs : EventArgs
{
    public int CommentId { get; set; }
    public int TaskId { get; set; }
}

public class NewNotificationEventArgs : EventArgs
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? TaskId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}

public class CommentUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}