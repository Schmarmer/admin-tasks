using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Admin_Tasks.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using TaskStatus = Admin_Tasks.Models.TaskStatus;

namespace Admin_Tasks.Services;

public class NotificationService : INotificationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<ChatHub>? _hubContext;

    public NotificationService(IServiceProvider serviceProvider, IHubContext<ChatHub>? hubContext = null)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    public async Task<Notification> CreateNotificationAsync(int userId, string title, string message, NotificationType type, int? taskId = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            TaskId = taskId,
            CreatedAt = DateTime.UtcNow
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Load navigation properties
        await context.Entry(notification)
            .Reference(n => n.User)
            .LoadAsync();

        if (taskId.HasValue)
        {
            await context.Entry(notification)
                .Reference(n => n.Task)
                .LoadAsync();
        }

        // Send real-time notification
        await SendNotificationToUserAsync(userId, notification);

        return notification;
    }

    public async Task<List<Notification>> GetNotificationsForUserAsync(int userId, bool includeRead = false, int limit = 50)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var query = context.Notifications
            .Include(n => n.Task)
            .Where(n => n.UserId == userId);

        if (!includeRead)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Notification>> GetUnreadNotificationsForUserAsync(int userId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        return await context.Notifications
            .Include(n => n.Task)
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> MarkNotificationAsReadAsync(int notificationId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId);

        if (notification == null)
            return false;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAllNotificationsAsReadAsync(int userId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var notifications = await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId);

        if (notification == null)
            return false;

        context.Notifications.Remove(notification);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetUnreadNotificationCountAsync(int userId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        return await context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    // News-spezifische Methoden
    public async Task<List<Notification>> GetNewsForUserAsync(int userId, bool includeRead = true, int limit = 100)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var query = context.Notifications
            .Include(n => n.Task)
            .Include(n => n.User)
            .Where(n => n.UserId == userId);

        if (!includeRead)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Notification>> GetTaskRelatedNewsAsync(int userId, bool includeRead = true)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var taskTypes = new[] 
        {
            NotificationType.TaskAssigned,
            NotificationType.TaskCompleted,
            NotificationType.TaskForwarded,
            NotificationType.TaskStatusChanged
        };

        var query = context.Notifications
            .Include(n => n.Task)
            .Include(n => n.User)
            .Where(n => n.UserId == userId && taskTypes.Contains(n.Type));

        if (!includeRead)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Notification>> GetMessageRelatedNewsAsync(int userId, bool includeRead = true)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var messageTypes = new[] 
        {
            NotificationType.TaskCommentAdded
        };

        var query = context.Notifications
            .Include(n => n.Task)
            .Include(n => n.User)
            .Where(n => n.UserId == userId && messageTypes.Contains(n.Type));

        if (!includeRead)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> MarkNotificationAsUnreadAsync(int notificationId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId);

        if (notification == null)
            return false;

        notification.IsRead = false;
        notification.ReadAt = null;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Notification>> SearchNotificationsAsync(int userId, string searchTerm, bool includeRead = true)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetNewsForUserAsync(userId, includeRead);

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var searchLower = searchTerm.ToLower();
        
        var query = context.Notifications
            .Include(n => n.Task)
            .Include(n => n.User)
            .Where(n => n.UserId == userId &&
                       (n.Title.ToLower().Contains(searchLower) ||
                        n.Message.ToLower().Contains(searchLower) ||
                        (n.Task != null && n.Task.Title.ToLower().Contains(searchLower))));

        if (!includeRead)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<Dictionary<NotificationType, int>> GetNotificationStatisticsAsync(int userId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var stats = await context.Notifications
            .Where(n => n.UserId == userId)
            .GroupBy(n => n.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);

        return stats;
    }

    // Automatic Notification Methods
    public async Task NotifyTaskAssignedAsync(int taskId, int assignedUserId, int assignedByUserId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var task = await context.Tasks
            .Include(t => t.CreatedByUser)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        var assignedByUser = await context.Users
            .FirstOrDefaultAsync(u => u.Id == assignedByUserId);

        if (task != null && assignedByUser != null)
        {
            await CreateNotificationAsync(
                assignedUserId,
                "Neue Aufgabe zugewiesen",
                $"Ihnen wurde die Aufgabe '{task.Title}' von {assignedByUser.FirstName} {assignedByUser.LastName} zugewiesen.",
                NotificationType.TaskAssigned,
                taskId
            );
        }
    }

    public async Task NotifyTaskCompletedAsync(int taskId, int completedByUserId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var task = await context.Tasks
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        var completedByUser = await context.Users
            .FirstOrDefaultAsync(u => u.Id == completedByUserId);

        if (task != null && completedByUser != null)
        {
            var usersToNotify = new List<int>();

            // Notify task creator if different from completer
            if (task.CreatedByUserId != completedByUserId)
            {
                usersToNotify.Add(task.CreatedByUserId);
            }

            // Notify assigned user if different from completer and creator
            if (task.AssignedToUserId.HasValue && 
                task.AssignedToUserId != completedByUserId && 
                task.AssignedToUserId != task.CreatedByUserId)
            {
                usersToNotify.Add(task.AssignedToUserId.Value);
            }

            foreach (var userId in usersToNotify)
            {
                await CreateNotificationAsync(
                    userId,
                    "Aufgabe abgeschlossen",
                    $"Die Aufgabe '{task.Title}' wurde von {completedByUser.FirstName} {completedByUser.LastName} abgeschlossen.",
                    NotificationType.TaskCompleted,
                    taskId
                );
            }
        }
    }

    public async Task NotifyTaskForwardedAsync(int taskId, int fromUserId, int toUserId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var task = await context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId);

        var fromUser = await context.Users
            .FirstOrDefaultAsync(u => u.Id == fromUserId);

        if (task != null && fromUser != null)
        {
            await CreateNotificationAsync(
                toUserId,
                "Aufgabe weitergeleitet",
                $"Die Aufgabe '{task.Title}' wurde Ihnen von {fromUser.FirstName} {fromUser.LastName} weitergeleitet.",
                NotificationType.TaskForwarded,
                taskId
            );
        }
    }

    public async Task NotifyTaskStatusChangedAsync(int taskId, TaskStatus oldStatus, TaskStatus newStatus, int changedByUserId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var task = await context.Tasks
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        var changedByUser = await context.Users
            .FirstOrDefaultAsync(u => u.Id == changedByUserId);

        if (task != null && changedByUser != null)
        {
            var usersToNotify = new List<int>();

            // Notify task creator if different from changer
            if (task.CreatedByUserId != changedByUserId)
            {
                usersToNotify.Add(task.CreatedByUserId);
            }

            // Notify assigned user if different from changer and creator
            if (task.AssignedToUserId.HasValue && 
                task.AssignedToUserId != changedByUserId && 
                task.AssignedToUserId != task.CreatedByUserId)
            {
                usersToNotify.Add(task.AssignedToUserId.Value);
            }

            var statusText = GetStatusDisplayText(newStatus);

            foreach (var userId in usersToNotify)
            {
                await CreateNotificationAsync(
                    userId,
                    "Aufgabenstatus geändert",
                    $"Der Status der Aufgabe '{task.Title}' wurde von {changedByUser.FirstName} {changedByUser.LastName} auf '{statusText}' geändert.",
                    NotificationType.TaskStatusChanged,
                    taskId
                );
            }
        }
    }

    public async Task NotifyTaskCommentAddedAsync(int taskId, int commentUserId, string commentContent)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var task = await context.Tasks
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        var commentUser = await context.Users
            .FirstOrDefaultAsync(u => u.Id == commentUserId);

        if (task != null && commentUser != null)
        {
            var usersToNotify = new List<int>();

            // Notify task creator if different from commenter
            if (task.CreatedByUserId != commentUserId)
            {
                usersToNotify.Add(task.CreatedByUserId);
            }

            // Notify assigned user if different from commenter and creator
            if (task.AssignedToUserId.HasValue && 
                task.AssignedToUserId != commentUserId && 
                task.AssignedToUserId != task.CreatedByUserId)
            {
                usersToNotify.Add(task.AssignedToUserId.Value);
            }

            var previewText = commentContent.Length > 100 ? 
                commentContent.Substring(0, 100) + "..." : commentContent;

            foreach (var userId in usersToNotify)
            {
                await CreateNotificationAsync(
                    userId,
                    "Neuer Kommentar",
                    $"{commentUser.FirstName} {commentUser.LastName} hat einen Kommentar zur Aufgabe '{task.Title}' hinzugefügt: {previewText}",
                    NotificationType.TaskCommentAdded,
                    taskId
                );
            }
        }
    }

    public async Task NotifyTaskDueDateApproachingAsync(int taskId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var task = await context.Tasks
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task?.AssignedToUserId.HasValue == true && task.DueDate.HasValue)
        {
            var daysUntilDue = (task.DueDate.Value - DateTime.UtcNow).Days;
            
            await CreateNotificationAsync(
                task.AssignedToUserId.Value,
                "Fälligkeitsdatum nähert sich",
                $"Die Aufgabe '{task.Title}' ist in {daysUntilDue} Tag(en) fällig.",
                NotificationType.TaskDueDateApproaching,
                taskId
            );
        }
    }

    public async Task NotifyTaskOverdueAsync(int taskId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var task = await context.Tasks
            .Include(t => t.AssignedToUser)
            .Include(t => t.CreatedByUser)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task?.DueDate.HasValue == true)
        {
            var daysOverdue = (DateTime.UtcNow - task.DueDate.Value).Days;
            var usersToNotify = new List<int>();

            if (task.AssignedToUserId.HasValue)
            {
                usersToNotify.Add(task.AssignedToUserId.Value);
            }

            if (task.CreatedByUserId != task.AssignedToUserId)
            {
                usersToNotify.Add(task.CreatedByUserId);
            }

            foreach (var userId in usersToNotify)
            {
                await CreateNotificationAsync(
                    userId,
                    "Aufgabe überfällig",
                    $"Die Aufgabe '{task.Title}' ist seit {daysOverdue} Tag(en) überfällig.",
                    NotificationType.TaskOverdue,
                    taskId
                );
            }
        }
    }

    // Real-time Communication
    public async Task SendNotificationToUserAsync(int userId, Notification notification)
    {
        if (_hubContext != null)
        {
            await _hubContext.Clients.Group($"User_{userId}")
                .SendAsync("NewNotification", new
                {
                    Id = notification.Id,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type.ToString(),
                    TaskId = notification.TaskId,
                    CreatedAt = notification.CreatedAt,
                    IsRead = notification.IsRead
                });
        }
    }

    public async Task BroadcastNotificationAsync(Notification notification, List<int> userIds)
    {
        if (_hubContext != null)
        {
            var tasks = userIds.Select(userId => 
                _hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("NewNotification", new
                    {
                        Id = notification.Id,
                        Title = notification.Title,
                        Message = notification.Message,
                        Type = notification.Type.ToString(),
                        TaskId = notification.TaskId,
                        CreatedAt = notification.CreatedAt,
                        IsRead = notification.IsRead
                    })
            );

            await Task.WhenAll(tasks);
        }
    }

    public async Task CleanupOldNotificationsAsync(int daysToKeep = 30)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        
        var oldNotifications = await context.Notifications
            .Where(n => n.CreatedAt < cutoffDate && n.IsRead)
            .ToListAsync();

        context.Notifications.RemoveRange(oldNotifications);
        await context.SaveChangesAsync();
    }

    private static string GetStatusDisplayText(TaskStatus status)
    {
        return status switch
        {
            TaskStatus.Open => "Offen",
            TaskStatus.InProgress => "In Bearbeitung",
            TaskStatus.Completed => "Abgeschlossen",
            TaskStatus.Cancelled => "Abgebrochen",
            _ => status.ToString()
        };
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
        
        return await context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }
}