using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Admin_Tasks.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using TaskStatus = Admin_Tasks.Models.TaskStatus;

namespace Admin_Tasks.Services;

public class NotificationService : INotificationService
{
    private readonly AdminTasksDbContext _context;
    private readonly IHubContext<ChatHub>? _hubContext;

    public NotificationService(AdminTasksDbContext context, IHubContext<ChatHub>? hubContext = null)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<Notification> CreateNotificationAsync(int userId, string title, string message, NotificationType type, int? taskId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            TaskId = taskId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Load navigation properties
        await _context.Entry(notification)
            .Reference(n => n.User)
            .LoadAsync();

        if (taskId.HasValue)
        {
            await _context.Entry(notification)
                .Reference(n => n.Task)
                .LoadAsync();
        }

        // Send real-time notification
        await SendNotificationToUserAsync(userId, notification);

        return notification;
    }

    public async Task<List<Notification>> GetNotificationsForUserAsync(int userId, bool includeRead = false, int limit = 50)
    {
        var query = _context.Notifications
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
        return await _context.Notifications
            .Include(n => n.Task)
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> MarkNotificationAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId);

        if (notification == null)
            return false;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAllNotificationsAsReadAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId);

        if (notification == null)
            return false;

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetUnreadNotificationCountAsync(int userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    // Automatic Notification Methods
    public async Task NotifyTaskAssignedAsync(int taskId, int assignedUserId, int assignedByUserId)
    {
        var task = await _context.Tasks
            .Include(t => t.CreatedByUser)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        var assignedByUser = await _context.Users
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
        var task = await _context.Tasks
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        var completedByUser = await _context.Users
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
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId);

        var fromUser = await _context.Users
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
        var task = await _context.Tasks
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        var changedByUser = await _context.Users
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
        var task = await _context.Tasks
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        var commentUser = await _context.Users
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
        var task = await _context.Tasks
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
        var task = await _context.Tasks
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
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        
        var oldNotifications = await _context.Notifications
            .Where(n => n.CreatedAt < cutoffDate && n.IsRead)
            .ToListAsync();

        _context.Notifications.RemoveRange(oldNotifications);
        await _context.SaveChangesAsync();
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
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }
}