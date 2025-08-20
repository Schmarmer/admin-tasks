using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Admin_Tasks.Models;
using Admin_Tasks.Services;
using TaskStatus = Admin_Tasks.Models.TaskStatus;

namespace Admin_Tasks.ViewModels
{
    public class TaskDetailViewModel : INotifyPropertyChanged
    {
        private TaskItem _task;
        private readonly ITaskService _taskService;
        private readonly IAuthenticationService _authService;
        private readonly IAttachmentService _attachmentService;
        private User _selectedUserForForwarding;
        private ObservableCollection<TaskAttachment> _attachments;
        
        public TaskDetailViewModel(ITaskService taskService, IAuthenticationService authService, IAttachmentService attachmentService)
        {
            _taskService = taskService;
            _authService = authService;
            _attachmentService = attachmentService;
            AvailableUsers = new ObservableCollection<User>();
            _attachments = new ObservableCollection<TaskAttachment>();
            LoadUsersAsync();
        }
        
        public void SetTask(TaskItem task)
        {
            _task = task;
            OnPropertyChanged(nameof(Task));
            LoadAttachmentsAsync();
        }
        
        public TaskItem Task
        {
            get => _task;
            set
            {
                _task = value;
                OnPropertyChanged();
            }
        }
        
        public ObservableCollection<User> AvailableUsers { get; }
        
        public ObservableCollection<TaskAttachment> Attachments
        {
            get => _attachments;
            set
            {
                _attachments = value;
                OnPropertyChanged();
            }
        }
        
        public User SelectedUserForForwarding
        {
            get => _selectedUserForForwarding;
            set
            {
                _selectedUserForForwarding = value;
                OnPropertyChanged();
            }
        }
        
        public bool CanForwardOrTakeTask => _authService.CurrentUser?.Role is "Admin" or "Manager" || 
                                            (_task?.AssignedToUserId == null); // Allow taking "Ohne Besitzer" tasks
        
        public bool CanAcceptTask => _task?.AssignedToUserId == _authService.CurrentUser?.Id && 
                                    _task?.Status != TaskStatus.InProgress && 
                                    _task?.Status != TaskStatus.Completed;
        
        public bool CanDeleteAttachments => _authService.CurrentUser?.Role is "Admin" or "Manager" || 
                                           _task?.CreatedByUserId == _authService.CurrentUser?.Id;
        
        public void RefreshTask()
        {
            // Task-Daten aktualisieren
            OnPropertyChanged(nameof(Task));
        }
        
        public void CompleteTask()
        {
            if (_task != null)
            {
                _task.Status = TaskStatus.Completed;
                _task.CompletedAt = DateTime.UtcNow;
                _task.UpdatedAt = DateTime.UtcNow;
                
                // In einer echten Anwendung würde hier die Datenbank aktualisiert
                // _taskService.UpdateTaskAsync(_task);
                
                OnPropertyChanged(nameof(Task));
            }
        }
        
        public async Task<bool> ForwardTaskAsync()
        {
            if (_task != null && SelectedUserForForwarding != null)
            {
                try
                {
                    // Special handling for "Ohne Besitzer" option (ID = 0)
                    int? assignedUserId = SelectedUserForForwarding.Id == 0 ? null : SelectedUserForForwarding.Id;
                    
                    var updatedTask = new TaskItem
                    {
                        Id = _task.Id,
                        Title = _task.Title,
                        Description = _task.Description,
                        Status = assignedUserId == null ? TaskStatus.Open : TaskStatus.InProgress,
                        Priority = _task.Priority,
                        DueDate = _task.DueDate,
                        AssignedToUserId = assignedUserId,
                        CreatedByUserId = _task.CreatedByUserId
                    };
                    
                    var success = await _taskService.UpdateTaskAsync(updatedTask);
                    if (success)
                    {
                        // Update local task object
                        _task.AssignedToUserId = assignedUserId;
                        _task.Status = updatedTask.Status;
                        _task.UpdatedAt = DateTime.UtcNow;
                        OnPropertyChanged(nameof(Task));
                        OnPropertyChanged(nameof(CanForwardOrTakeTask));
                    }
                    return success;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TaskDetailViewModel] Error forwarding task: {ex}");
                    return false;
                }
            }
            return false;
        }
        
        public async Task LoadUsersAsync()
        {
            try
            {
                var users = await _taskService.GetAvailableUsersAsync();
                AvailableUsers.Clear();
                
                // Add "Ohne Besitzer" option first
                AvailableUsers.Add(new User 
                { 
                    Id = 0, 
                    Username = "Ohne Besitzer", 
                    Role = null,
                    IsActive = true
                });
                
                if (users?.Any() == true)
                {
                    foreach (var user in users)
                    {
                        AvailableUsers.Add(user);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskDetailViewModel] Error loading users: {ex}");
            }
        }
        
        public async Task LoadAttachmentsAsync()
        {
            try
            {
                if (_task?.Id > 0)
                {
                    var attachments = await _attachmentService.GetTaskAttachmentsAsync(_task.Id);
                    Attachments.Clear();
                    
                    if (attachments?.Any() == true)
                    {
                        foreach (var attachment in attachments)
                        {
                            Attachments.Add(attachment);
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[TaskDetailViewModel] {Attachments.Count} Anhänge geladen");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskDetailViewModel] Error loading attachments: {ex}");
            }
        }
        
        public async Task<bool> DeleteAttachmentAsync(TaskAttachment attachment)
        {
            try
            {
                var success = await _attachmentService.DeleteAttachmentAsync(attachment.Id);
                if (success)
                {
                    Attachments.Remove(attachment);
                    System.Diagnostics.Debug.WriteLine($"[TaskDetailViewModel] Anhang gelöscht: {attachment.FileName}");
                }
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskDetailViewModel] Error deleting attachment: {ex}");
                return false;
            }
        }
        
        public void AddAttachment(TaskAttachment attachment)
        {
            if (attachment != null && !Attachments.Contains(attachment))
            {
                Attachments.Add(attachment);
                System.Diagnostics.Debug.WriteLine($"[TaskDetailViewModel] Anhang hinzugefügt: {attachment.FileName}");
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}