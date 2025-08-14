using Admin_Tasks.Models;
using Microsoft.EntityFrameworkCore;

namespace Admin_Tasks.Services;

public class TaskService : ITaskService
{
    private readonly AdminTasksDbContext _context;

    public TaskService(AdminTasksDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaskItem>> GetAllTasksAsync()
    {
        return await _context.Tasks
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetTasksByUserAsync(int userId)
    {
        return await _context.Tasks
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .Where(t => t.CreatedByUserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetTasksAssignedToUserAsync(int userId)
    {
        return await _context.Tasks
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .Where(t => t.AssignedToUserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<TaskItem?> GetTaskByIdAsync(int taskId)
    {
        return await _context.Tasks
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .Include(t => t.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(t => t.Id == taskId);
    }

    public async Task<TaskItem> CreateTaskAsync(TaskItem task)
    {
        task.CreatedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;
        
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        
        return await GetTaskByIdAsync(task.Id) ?? task;
    }

    public async Task<bool> UpdateTaskAsync(TaskItem task)
    {
        var existingTask = await _context.Tasks.FindAsync(task.Id);
        if (existingTask == null)
            return false;

        existingTask.Title = task.Title;
        existingTask.Description = task.Description;
        existingTask.Status = task.Status;
        existingTask.Priority = task.Priority;
        existingTask.DueDate = task.DueDate;
        existingTask.AssignedToUserId = task.AssignedToUserId;
        existingTask.UpdatedAt = DateTime.UtcNow;

        if (task.Status == Models.TaskStatus.Completed && existingTask.CompletedAt == null)
        {
            existingTask.CompletedAt = DateTime.UtcNow;
        }
        else if (task.Status != Models.TaskStatus.Completed)
        {
            existingTask.CompletedAt = null;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTaskAsync(int taskId)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
            return false;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignTaskAsync(int taskId, int userId)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
            return false;

        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
            return false;

        task.AssignedToUserId = userId;
        task.UpdatedAt = DateTime.UtcNow;
        
        if (task.Status == Models.TaskStatus.Open)
        {
            task.Status = Models.TaskStatus.InProgress;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompleteTaskAsync(int taskId)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
            return false;

        task.Status = Models.TaskStatus.Completed;
        task.CompletedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddCommentAsync(int taskId, int userId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
            return false;

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        var comment = new TaskComment
        {
            TaskId = taskId,
            UserId = userId,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.TaskComments.Add(comment);
        
        // Update task's UpdatedAt timestamp
        task.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<TaskComment>> GetTaskCommentsAsync(int taskId)
    {
        return await _context.TaskComments
            .Include(c => c.User)
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetAvailableUsersAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }
}