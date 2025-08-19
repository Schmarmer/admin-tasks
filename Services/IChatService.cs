using Admin_Tasks.Models;

namespace Admin_Tasks.Services;

public interface IChatService
{
    // Comment/Chat Operations
    Task<TaskComment> AddCommentAsync(int taskId, int userId, string content, CommentType type = CommentType.Normal, int? parentCommentId = null);
    Task<TaskComment> UpdateCommentAsync(int commentId, string content);
    Task<bool> DeleteCommentAsync(int commentId, int userId);
    Task<TaskComment?> GetCommentByIdAsync(int commentId);
    Task<List<TaskComment>> GetCommentsForTaskAsync(int taskId);
    Task<List<TaskComment>> GetUnreadCommentsForUserAsync(int userId);
    Task<bool> MarkCommentAsReadAsync(int commentId, int userId);
    Task<bool> MarkAllCommentsAsReadAsync(int taskId, int userId);
    
    // Real-time Communication
    Task NotifyNewCommentAsync(int taskId, TaskComment comment);
    Task NotifyCommentUpdatedAsync(int taskId, TaskComment comment);
    Task NotifyCommentDeletedAsync(int taskId, int commentId);
    
    // Statistics
    Task<int> GetUnreadCommentCountForUserAsync(int userId);
    Task<int> GetUnreadCommentCountForTaskAsync(int taskId, int userId);
    
    // Chat Overview
    Task<List<TaskChatSummary>> GetChatSummariesForUserAsync(int userId);
    Task<TaskChatSummary?> GetChatSummaryForTaskAsync(int taskId, int userId);
    Task<List<TaskChatSummary>> GetActiveChatSummariesAsync(int userId);
    Task<bool> MarkChatAsFavoriteAsync(int taskId, int userId, bool isFavorite);
    Task<bool> MuteChatAsync(int taskId, int userId, bool isMuted);
}