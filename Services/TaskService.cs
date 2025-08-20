using Admin_Tasks.Models;
using Microsoft.EntityFrameworkCore;

namespace Admin_Tasks.Services;

public class TaskService : ITaskService
{
    private readonly AdminTasksDbContext _context;
    private readonly INotificationService _notificationService;

    public TaskService(AdminTasksDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
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
        
        // Ensure DueDate is UTC if provided
        if (task.DueDate.HasValue && task.DueDate.Value.Kind != DateTimeKind.Utc)
        {
            task.DueDate = DateTime.SpecifyKind(task.DueDate.Value, DateTimeKind.Utc);
        }
        
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
        
        // Ensure DueDate is UTC if provided
        if (task.DueDate.HasValue && task.DueDate.Value.Kind != DateTimeKind.Utc)
        {
            existingTask.DueDate = DateTime.SpecifyKind(task.DueDate.Value, DateTimeKind.Utc);
        }
        else
        {
            existingTask.DueDate = task.DueDate;
        }
        
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
        System.Diagnostics.Debug.WriteLine($"[TaskService.AssignTaskAsync] Starte Zuweisung: TaskId={taskId}, UserId={userId}");
        
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
        {
            System.Diagnostics.Debug.WriteLine($"[TaskService.AssignTaskAsync] Task {taskId} nicht gefunden");
            return false;
        }

        System.Diagnostics.Debug.WriteLine($"[TaskService.AssignTaskAsync] Task gefunden: {task.Title}, aktueller Besitzer: {task.AssignedToUserId}");
        
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
        {
            System.Diagnostics.Debug.WriteLine($"[TaskService.AssignTaskAsync] Benutzer {userId} nicht gefunden oder inaktiv");
            return false;
        }

        System.Diagnostics.Debug.WriteLine($"[TaskService.AssignTaskAsync] Benutzer gefunden: {user.Username}, aktiv: {user.IsActive}");
        
        task.AssignedToUserId = userId;
        task.UpdatedAt = DateTime.UtcNow;
        
        if (task.Status == Models.TaskStatus.Open)
        {
            task.Status = Models.TaskStatus.InProgress;
            System.Diagnostics.Debug.WriteLine($"[TaskService.AssignTaskAsync] Status geändert von Open zu InProgress");
        }

        System.Diagnostics.Debug.WriteLine($"[TaskService.AssignTaskAsync] Speichere Änderungen in Datenbank...");
        
        try
        {
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"[TaskService.AssignTaskAsync] Erfolgreich gespeichert - Task {taskId} ist jetzt Benutzer {userId} zugewiesen");
            
            // Benachrichtigung senden
            await _notificationService.NotifyTaskAssignedAsync(taskId, userId, task.CreatedByUserId);
            System.Diagnostics.Debug.WriteLine($"[TaskService.AssignTaskAsync] Benachrichtigung gesendet");
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TaskService.AssignTaskAsync] Fehler beim Speichern: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Nimmt eine zugewiesene Task an und ändert den Status zu InProgress
    /// </summary>
    /// <param name="taskId">ID der Task</param>
    /// <param name="userId">ID des Benutzers, der die Task annimmt</param>
    /// <returns>True wenn erfolgreich</returns>
    public async Task<bool> AcceptTaskAsync(int taskId, int userId)
    {
        System.Diagnostics.Debug.WriteLine($"[TaskService.AcceptTaskAsync] Starte Task-Annahme: TaskId={taskId}, UserId={userId}");
        
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
        {
            System.Diagnostics.Debug.WriteLine($"[TaskService.AcceptTaskAsync] Task {taskId} nicht gefunden");
            return false;
        }

        // Prüfen ob Task dem Benutzer zugewiesen ist
        if (task.AssignedToUserId != userId)
        {
            System.Diagnostics.Debug.WriteLine($"[TaskService.AcceptTaskAsync] Task ist nicht dem Benutzer {userId} zugewiesen");
            return false;
        }

        // Prüfen ob Task bereits angenommen wurde
        if (task.Status == Models.TaskStatus.InProgress)
        {
            System.Diagnostics.Debug.WriteLine($"[TaskService.AcceptTaskAsync] Task ist bereits in Bearbeitung");
            return true; // Bereits angenommen, kein Fehler
        }

        // Status auf InProgress setzen
        task.Status = Models.TaskStatus.InProgress;
        task.UpdatedAt = DateTime.UtcNow;
        
        System.Diagnostics.Debug.WriteLine($"[TaskService.AcceptTaskAsync] Status geändert zu InProgress");
        
        try
        {
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"[TaskService.AcceptTaskAsync] Task-Annahme erfolgreich abgeschlossen");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TaskService.AcceptTaskAsync] Fehler: {ex.Message}");
            return false;
        }
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

    public async Task<bool> CompleteTaskAsync(int taskId, int completedByUserId)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
            return false;

        task.Status = Models.TaskStatus.Completed;
        task.CompletedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        
        // Benachrichtigung senden
        await _notificationService.NotifyTaskCompletedAsync(taskId, completedByUserId);
        
        return true;
    }

    /// <summary>
    /// Leitet eine Task an einen anderen Benutzer weiter
    /// </summary>
    /// <param name="taskId">ID der Task</param>
    /// <param name="newAssignedUserId">ID des neuen zugewiesenen Benutzers</param>
    /// <param name="forwardingUserId">ID des weiterleitenden Benutzers</param>
    /// <returns>True wenn erfolgreich</returns>
    public async Task<bool> ForwardTaskAsync(int taskId, int newAssignedUserId, int forwardingUserId)
    {
        System.Diagnostics.Debug.WriteLine($"[TaskService.ForwardTaskAsync] Starte Task-Weiterleitung: TaskId={taskId}, NewUserId={newAssignedUserId}, ForwardingUserId={forwardingUserId}");
        
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
        {
            System.Diagnostics.Debug.WriteLine($"[TaskService.ForwardTaskAsync] Task {taskId} nicht gefunden");
            return false;
        }

        // Prüfen ob der weiterleitende Benutzer berechtigt ist
        if (task.AssignedToUserId != forwardingUserId && task.CreatedByUserId != forwardingUserId)
        {
            System.Diagnostics.Debug.WriteLine($"[TaskService.ForwardTaskAsync] Benutzer {forwardingUserId} ist nicht berechtigt, Task weiterzuleiten");
            return false;
        }

        // Prüfen ob der neue Benutzer existiert
        var newUser = await _context.Users.FindAsync(newAssignedUserId);
        if (newUser == null || !newUser.IsActive)
        {
            System.Diagnostics.Debug.WriteLine($"[TaskService.ForwardTaskAsync] Neuer Benutzer {newAssignedUserId} nicht gefunden oder inaktiv");
            return false;
        }

        // Task weiterleiten
        task.AssignedToUserId = newAssignedUserId;
        task.Status = Models.TaskStatus.Open; // Zurück auf Open setzen, da neuer Benutzer sie noch annehmen muss
        task.UpdatedAt = DateTime.UtcNow;

        System.Diagnostics.Debug.WriteLine($"[TaskService.ForwardTaskAsync] Task weitergeleitet an Benutzer {newAssignedUserId}");
        
        try
        {
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"[TaskService.ForwardTaskAsync] Task-Weiterleitung erfolgreich abgeschlossen");
            
            // Benachrichtigung senden
            await _notificationService.NotifyTaskForwardedAsync(taskId, forwardingUserId, newAssignedUserId);
            System.Diagnostics.Debug.WriteLine($"[TaskService.ForwardTaskAsync] Benachrichtigung gesendet");
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TaskService.ForwardTaskAsync] Fehler: {ex.Message}");
            return false;
        }
    }
    
    public async Task<bool> CompleteTaskWithDetailsAsync(TaskItem task, TaskCompletionDetails completionDetails)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Update the task
            var existingTask = await _context.Tasks.FindAsync(task.Id);
            if (existingTask == null)
                return false;
            
            existingTask.Status = task.Status;
            existingTask.CompletedAt = task.CompletedAt;
            existingTask.UpdatedAt = task.UpdatedAt;
            
            // Add completion details
            _context.TaskCompletionDetails.Add(completionDetails);
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            System.Diagnostics.Debug.WriteLine($"Error completing task with details: {ex.Message}");
            return false;
        }
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
        
        // Benachrichtigung über neuen Kommentar senden
        await _notificationService.NotifyTaskCommentAddedAsync(taskId, userId, content.Trim());
        
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