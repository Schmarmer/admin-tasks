using Admin_Tasks.Models;
using TaskStatus = Admin_Tasks.Models.TaskStatus;

namespace Admin_Tasks.Services;

public interface INotificationService
{
    // Notification Operations
    Task<Notification> CreateNotificationAsync(int userId, string title, string message, NotificationType type, int? taskId = null);
    Task<List<Notification>> GetNotificationsForUserAsync(int userId, bool includeRead = false, int limit = 50);
    Task<List<Notification>> GetUnreadNotificationsForUserAsync(int userId);
    Task<bool> MarkNotificationAsReadAsync(int notificationId);
    Task<bool> MarkAllNotificationsAsReadAsync(int userId);
    Task<bool> DeleteNotificationAsync(int notificationId);
    Task<int> GetUnreadNotificationCountAsync(int userId);
    
    // News-spezifische Methoden
    Task<List<Notification>> GetNewsForUserAsync(int userId, bool includeRead = true, int limit = 100);
    Task<List<Notification>> GetTaskRelatedNewsAsync(int userId, bool includeRead = true);
    Task<List<Notification>> GetMessageRelatedNewsAsync(int userId, bool includeRead = true);
    Task<bool> MarkNotificationAsUnreadAsync(int notificationId);
    Task<List<Notification>> SearchNotificationsAsync(int userId, string searchTerm, bool includeRead = true);
    Task<Dictionary<NotificationType, int>> GetNotificationStatisticsAsync(int userId);
    
    // Automatic Notifications
    Task NotifyTaskAssignedAsync(int taskId, int assignedUserId, int assignedByUserId);
    Task NotifyTaskCompletedAsync(int taskId, int completedByUserId);
    Task NotifyTaskForwardedAsync(int taskId, int fromUserId, int toUserId);
    Task NotifyTaskStatusChangedAsync(int taskId, TaskStatus oldStatus, TaskStatus newStatus, int changedByUserId);
    Task NotifyTaskCommentAddedAsync(int taskId, int commentUserId, string commentContent);
    Task NotifyTaskDueDateApproachingAsync(int taskId);
    Task NotifyTaskOverdueAsync(int taskId);
    
    // Real-time Communication
    Task SendNotificationToUserAsync(int userId, Notification notification);
    Task BroadcastNotificationAsync(Notification notification, List<int> userIds);
    
    // Cleanup
    Task CleanupOldNotificationsAsync(int daysToKeep = 30);
}