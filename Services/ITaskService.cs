using Admin_Tasks.Models;

namespace Admin_Tasks.Services;

public interface ITaskService
{
    Task<IEnumerable<TaskItem>> GetAllTasksAsync();
    Task<IEnumerable<TaskItem>> GetTasksByUserAsync(int userId);
    Task<IEnumerable<TaskItem>> GetTasksAssignedToUserAsync(int userId);
    Task<TaskItem?> GetTaskByIdAsync(int taskId);
    Task<TaskItem> CreateTaskAsync(TaskItem task);
    Task<bool> UpdateTaskAsync(TaskItem task);
    Task<bool> DeleteTaskAsync(int taskId);
    Task<bool> AssignTaskAsync(int taskId, int userId);
    Task<bool> CompleteTaskAsync(int taskId);
    Task<bool> CompleteTaskWithDetailsAsync(TaskItem task, TaskCompletionDetails completionDetails);
    Task<bool> AddCommentAsync(int taskId, int userId, string content);
    Task<IEnumerable<TaskComment>> GetTaskCommentsAsync(int taskId);
    Task<IEnumerable<User>> GetAvailableUsersAsync();
}