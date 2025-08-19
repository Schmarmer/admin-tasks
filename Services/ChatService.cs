using Admin_Tasks.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace Admin_Tasks.Services;

public class ChatService : IChatService
{
    private readonly AdminTasksDbContext _context;
    private readonly IHubContext<ChatHub>? _hubContext;

    public ChatService(AdminTasksDbContext context, IHubContext<ChatHub>? hubContext = null)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<TaskComment> AddCommentAsync(int taskId, int userId, string content, CommentType type = CommentType.Normal, int? parentCommentId = null)
    {
        var comment = new TaskComment
        {
            TaskId = taskId,
            UserId = userId,
            Content = content.Trim(),
            Type = type,
            ParentCommentId = parentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        _context.TaskComments.Add(comment);
        await _context.SaveChangesAsync();

        // Load navigation properties
        await _context.Entry(comment)
            .Reference(c => c.User)
            .LoadAsync();

        await _context.Entry(comment)
            .Reference(c => c.Task)
            .LoadAsync();

        // Notify real-time clients
        await NotifyNewCommentAsync(taskId, comment);

        return comment;
    }

    public async Task<TaskComment> UpdateCommentAsync(int commentId, string content)
    {
        var comment = await _context.TaskComments
            .Include(c => c.User)
            .Include(c => c.Task)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null)
            throw new ArgumentException("Kommentar nicht gefunden", nameof(commentId));

        comment.Content = content.Trim();
        comment.UpdatedAt = DateTime.UtcNow;
        comment.IsEdited = true;

        await _context.SaveChangesAsync();

        // Notify real-time clients
        await NotifyCommentUpdatedAsync(comment.TaskId, comment);

        return comment;
    }

    public async Task<bool> DeleteCommentAsync(int commentId, int userId)
    {
        var comment = await _context.TaskComments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId);

        if (comment == null)
            return false;

        var taskId = comment.TaskId;

        // Delete replies first
        var replies = await _context.TaskComments
            .Where(c => c.ParentCommentId == commentId)
            .ToListAsync();

        _context.TaskComments.RemoveRange(replies);
        _context.TaskComments.Remove(comment);

        await _context.SaveChangesAsync();

        // Notify real-time clients
        await NotifyCommentDeletedAsync(taskId, commentId);

        return true;
    }

    public async Task<TaskComment?> GetCommentByIdAsync(int commentId)
    {
        return await _context.TaskComments
            .Include(c => c.User)
            .Include(c => c.Task)
            .Include(c => c.ParentComment)
            .Include(c => c.Replies)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(c => c.Id == commentId);
    }

    public async Task<List<TaskComment>> GetCommentsForTaskAsync(int taskId)
    {
        return await _context.TaskComments
            .Include(c => c.User)
            .Include(c => c.ParentComment)
            .Include(c => c.Replies)
                .ThenInclude(r => r.User)
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<TaskComment>> GetUnreadCommentsForUserAsync(int userId)
    {
        // Get tasks assigned to or created by the user
        var userTaskIds = await _context.Tasks
            .Where(t => t.AssignedToUserId == userId || t.CreatedByUserId == userId)
            .Select(t => t.Id)
            .ToListAsync();

        return await _context.TaskComments
            .Include(c => c.User)
            .Include(c => c.Task)
            .Where(c => userTaskIds.Contains(c.TaskId) && 
                       c.UserId != userId && 
                       !c.IsRead)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> MarkCommentAsReadAsync(int commentId, int userId)
    {
        var comment = await _context.TaskComments
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null || comment.UserId == userId)
            return false;

        comment.IsRead = true;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> MarkAllCommentsAsReadAsync(int taskId, int userId)
    {
        var comments = await _context.TaskComments
            .Where(c => c.TaskId == taskId && c.UserId != userId && !c.IsRead)
            .ToListAsync();

        foreach (var comment in comments)
        {
            comment.IsRead = true;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetUnreadCommentCountForUserAsync(int userId)
    {
        var userTaskIds = await _context.Tasks
            .Where(t => t.AssignedToUserId == userId || t.CreatedByUserId == userId)
            .Select(t => t.Id)
            .ToListAsync();

        return await _context.TaskComments
            .CountAsync(c => userTaskIds.Contains(c.TaskId) && 
                           c.UserId != userId && 
                           !c.IsRead);
    }

    public async Task<int> GetUnreadCommentCountForTaskAsync(int taskId, int userId)
    {
        return await _context.TaskComments
            .CountAsync(c => c.TaskId == taskId && 
                           c.UserId != userId && 
                           !c.IsRead);
    }
    
    public async Task<List<TaskChatSummary>> GetChatSummariesForUserAsync(int userId)
    {
        System.Diagnostics.Debug.WriteLine($"[ChatService] GetChatSummariesForUserAsync called for userId: {userId}");
        
        // Erst alle relevanten Tasks laden, dann Duplikate vermeiden
        var relevantTasks = await _context.Tasks
            .Include(t => t.AssignedToUser)
            .Include(t => t.Comments)
                .ThenInclude(c => c.User)
            .Where(t => t.AssignedToUserId == userId || 
                       t.CreatedByUserId == userId ||
                       t.Comments.Any(c => c.UserId == userId))
            .Distinct() // Duplikate auf Task-Ebene vermeiden
            .ToListAsync();
            
        System.Diagnostics.Debug.WriteLine($"[ChatService] Found {relevantTasks.Count} relevant tasks");
        
        var taskChats = new List<TaskChatSummary>();
        
        foreach (var task in relevantTasks)
        {
            var lastComment = task.Comments?.OrderByDescending(c => c.CreatedAt).FirstOrDefault();
            
            var chatSummary = new TaskChatSummary
            {
                TaskId = task.Id,
                TaskTitle = task.Title,
                TaskDescription = !string.IsNullOrEmpty(task.Description) && task.Description.Length > 100 ? task.Description.Substring(0, 100) + "..." : task.Description ?? string.Empty,
                TaskStatus = task.Status,
                TaskPriority = task.Priority,
                AssignedUserId = task.AssignedToUserId,
                AssignedUser = task.AssignedToUser,
                LastMessageContent = lastComment?.Content?.Length > 50 ? lastComment.Content.Substring(0, 50) + "..." : lastComment?.Content,
                LastMessageTime = lastComment?.CreatedAt,
                LastMessageAuthorId = lastComment?.UserId,
                LastMessageAuthor = lastComment?.User,
                UnreadCount = task.Comments?.Count(c => c.UserId != userId && !c.IsRead) ?? 0,
                TotalMessageCount = task.Comments?.Count() ?? 0,
                IsLastMessageFromCurrentUser = lastComment?.UserId == userId,
                LastActivity = task.Comments?.Any() == true ? 
                    task.Comments.Max(c => c.CreatedAt) : 
                    task.UpdatedAt,
                IsFavorite = false, // TODO: Implement user preferences
                IsMuted = false // TODO: Implement user preferences
            };
            
            taskChats.Add(chatSummary);
        }
        
        // Nach LastActivity sortieren
        taskChats = taskChats.OrderByDescending(tc => tc.LastActivity).ToList();
            
        System.Diagnostics.Debug.WriteLine($"[ChatService] Created {taskChats.Count} chat summaries before duplicate removal");
        
        // Duplikate nach TaskId entfernen (falls vorhanden)
        var uniqueChats = taskChats
            .GroupBy(tc => tc.TaskId)
            .Select(g => g.First())
            .ToList();
            
        System.Diagnostics.Debug.WriteLine($"[ChatService] After duplicate removal: {uniqueChats.Count} unique chat summaries");
        
        if (taskChats.Count != uniqueChats.Count)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatService] WARNING: Removed {taskChats.Count - uniqueChats.Count} duplicate chat summaries!");
        }
            
        return uniqueChats;
    }
    
    public async Task<TaskChatSummary?> GetChatSummaryForTaskAsync(int taskId, int userId)
    {
        var task = await _context.Tasks
            .Include(t => t.AssignedToUser)
            .Include(t => t.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(t => t.Id == taskId);
            
        if (task == null) return null;
        
        var lastComment = task.Comments.OrderByDescending(c => c.CreatedAt).FirstOrDefault();
        
        return new TaskChatSummary
        {
            TaskId = task.Id,
            TaskTitle = task.Title,
            TaskDescription = task.Description.Length > 100 ? task.Description.Substring(0, 100) + "..." : task.Description,
            TaskStatus = task.Status,
            TaskPriority = task.Priority,
            AssignedUserId = task.AssignedToUserId,
            AssignedUser = task.AssignedToUser,
            LastMessageContent = lastComment?.Content?.Length > 50 ? 
                lastComment.Content.Substring(0, 50) + "..." : 
                lastComment?.Content,
            LastMessageTime = lastComment?.CreatedAt,
            LastMessageAuthorId = lastComment?.UserId,
            LastMessageAuthor = lastComment?.User,
            UnreadCount = task.Comments.Count(c => c.UserId != userId),
            TotalMessageCount = task.Comments.Count,
            IsLastMessageFromCurrentUser = lastComment?.UserId == userId,
            LastActivity = task.Comments.Any() ? 
                task.Comments.Max(c => c.CreatedAt) : 
                task.UpdatedAt,
            IsFavorite = false, // TODO: Implement user preferences
            IsMuted = false // TODO: Implement user preferences
        };
    }
    
    public async Task<List<TaskChatSummary>> GetActiveChatSummariesAsync(int userId)
    {
        var activeChats = await GetChatSummariesForUserAsync(userId);
        return activeChats.Where(c => c.TotalMessageCount > 0 && c.TaskStatus != Models.TaskStatus.Completed).ToList();
    }
    
    public async Task<bool> MarkChatAsFavoriteAsync(int taskId, int userId, bool isFavorite)
    {
        // TODO: Implement user chat preferences table
        // For now, return true as placeholder
        return await Task.FromResult(true);
    }
    
    public async Task<bool> MuteChatAsync(int taskId, int userId, bool isMuted)
    {
        // TODO: Implement user chat preferences table
        // For now, return true as placeholder
        return await Task.FromResult(true);
    }

    // Real-time Communication Methods
    public async Task NotifyNewCommentAsync(int taskId, TaskComment comment)
    {
        if (_hubContext != null)
        {
            await _hubContext.Clients.Group($"Task_{taskId}")
                .SendAsync("NewComment", new
                {
                    CommentId = comment.Id,
                    TaskId = taskId,
                    Content = comment.Content,
                    Type = comment.Type.ToString(),
                    CreatedAt = comment.CreatedAt,
                    User = new
                    {
                        Id = comment.User?.Id,
                        Username = comment.User?.Username,
                        FirstName = comment.User?.FirstName,
                        LastName = comment.User?.LastName
                    },
                    ParentCommentId = comment.ParentCommentId
                });
        }
    }

    public async Task NotifyCommentUpdatedAsync(int taskId, TaskComment comment)
    {
        if (_hubContext != null)
        {
            await _hubContext.Clients.Group($"Task_{taskId}")
                .SendAsync("CommentUpdated", new
                {
                    CommentId = comment.Id,
                    TaskId = taskId,
                    Content = comment.Content,
                    UpdatedAt = comment.UpdatedAt,
                    IsEdited = comment.IsEdited
                });
        }
    }

    public async Task NotifyCommentDeletedAsync(int taskId, int commentId)
    {
        if (_hubContext != null)
        {
            await _hubContext.Clients.Group($"Task_{taskId}")
                .SendAsync("CommentDeleted", new
                {
                    CommentId = commentId,
                    TaskId = taskId
                });
        }
    }
}

// SignalR Hub für Real-Time Chat
public class ChatHub : Hub
{
    public async Task JoinTaskGroup(int taskId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Task_{taskId}");
    }

    public async Task LeaveTaskGroup(int taskId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Task_{taskId}");
    }

    public async Task JoinUserGroup(int userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
    }

    public async Task LeaveUserGroup(int userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
    }
}