using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Admin_Tasks.Models;
using Admin_Tasks.Services;

namespace Admin_Tasks.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        private readonly IChatService _chatService;
        private readonly IAuthenticationService _authService;
        private readonly SignalRService _signalRService;
        private int _taskId;
        private string _newMessageText = string.Empty;
        private TaskComment? _replyToComment;
        private bool _isLoading;
        
        public ChatViewModel(IChatService chatService, IAuthenticationService authService, SignalRService signalRService)
        {
            _chatService = chatService;
            _authService = authService;
            _signalRService = signalRService;
            
            Comments = new ObservableCollection<ChatCommentViewModel>();
            
            // Commands
            SendMessageCommand = new RelayCommand(async () => await SendMessageAsync(), () => CanSendMessage);
            CancelReplyCommand = new RelayCommand(() => ReplyToComment = null);
            
            // Subscribe to SignalR events
            _signalRService.NewCommentReceived += OnCommentAdded;
            _signalRService.CommentUpdated += OnCommentUpdated;
            _signalRService.CommentDeleted += OnCommentDeleted;
        }
        
        public ObservableCollection<ChatCommentViewModel> Comments { get; }
        
        public string NewMessageText
        {
            get => _newMessageText;
            set
            {
                _newMessageText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSendMessage));
            }
        }
        
        public TaskComment? ReplyToComment
        {
            get => _replyToComment;
            set
            {
                _replyToComment = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
        
        public bool CanSendMessage => !string.IsNullOrWhiteSpace(NewMessageText) && !IsLoading;
        
        public ICommand SendMessageCommand { get; }
        public ICommand CancelReplyCommand { get; }
        
        public async Task InitializeAsync(int taskId)
        {
            _taskId = taskId;
            await LoadCommentsAsync();
            
            // Join SignalR group for this task
            await _signalRService.JoinTaskChatAsync(taskId);
        }
        
        public async Task InitializeForTask(int taskId)
        {
            await InitializeAsync(taskId);
        }
        
        private async Task LoadCommentsAsync()
        {
            try
            {
                IsLoading = true;
                var comments = await _chatService.GetCommentsForTaskAsync(_taskId);
                
                Comments.Clear();
                if (comments?.Any() == true)
                {
                    foreach (var comment in comments.OrderBy(c => c.CreatedAt))
                    {
                        var viewModel = new ChatCommentViewModel(comment, _authService.CurrentUser?.Id ?? 0);
                        Comments.Add(viewModel);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatViewModel] Error loading comments: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async Task SendMessageAsync()
        {
            if (!CanSendMessage || _authService.CurrentUser == null)
                return;
                
            try
            {
                IsLoading = true;
                
                var comment = await _chatService.AddCommentAsync(
                    _taskId,
                    _authService.CurrentUser?.Id ?? 0,
                    NewMessageText.Trim(),
                    CommentType.Normal,
                    ReplyToComment?.Id
                );
                if (comment != null)
                {
                    NewMessageText = string.Empty;
                    ReplyToComment = null;
                    
                    // Add comment directly to the list as fallback if SignalR doesn't work
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        var viewModel = new ChatCommentViewModel(comment, _authService.CurrentUser?.Id ?? 0);
                        Comments.Add(viewModel);
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatViewModel] Error sending message: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        public void SetReplyTo(TaskComment comment)
        {
            ReplyToComment = comment;
        }
        
        private void OnCommentAdded(object? sender, NewCommentEventArgs e)
        {
            if (e.TaskId == _taskId)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    // Check if comment already exists to avoid duplicates
                    if (Comments.Any(c => c.Comment.Id == e.CommentId))
                        return;
                        
                    var comment = new TaskComment
                    {
                        Id = e.CommentId,
                        TaskId = e.TaskId,
                        Content = e.Content,
                        Type = Enum.TryParse<CommentType>(e.Type, out var commentType) ? commentType : CommentType.Normal,
                        CreatedAt = e.CreatedAt,
                        UserId = e.User.Id,
                        User = new User { Id = e.User.Id, Username = e.User.Username },
                        ParentCommentId = e.ParentCommentId
                    };
                    var viewModel = new ChatCommentViewModel(comment, _authService.CurrentUser?.Id ?? 0);
                    Comments.Add(viewModel);
                });
            }
        }
        
        private void OnCommentUpdated(object? sender, CommentUpdatedEventArgs e)
        {
            if (e.TaskId == _taskId)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    var existingComment = Comments.FirstOrDefault(c => c.Comment.Id == e.CommentId);
                    if (existingComment != null)
                    {
                        existingComment.Comment.Content = e.Content;
                        existingComment.Comment.UpdatedAt = e.UpdatedAt;
                        existingComment.Comment.IsEdited = e.IsEdited;
                        existingComment.NotifyCommentChanged();
                    }
                });
            }
        }
        
        private void OnCommentDeleted(object? sender, CommentDeletedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var comment = Comments.FirstOrDefault(c => c.Comment.Id == e.CommentId);
                if (comment != null)
                {
                    Comments.Remove(comment);
                }
            });
        }
        
        public void Dispose()
        {
            _signalRService.NewCommentReceived -= OnCommentAdded;
            _signalRService.CommentUpdated -= OnCommentUpdated;
            _signalRService.CommentDeleted -= OnCommentDeleted;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class ChatCommentViewModel : INotifyPropertyChanged
    {
        private TaskComment _comment;
        private readonly int _currentUserId;
        
        public ChatCommentViewModel(TaskComment comment, int currentUserId)
        {
            _comment = comment;
            _currentUserId = currentUserId;
        }
        
        public TaskComment Comment => _comment;
        
        public bool IsOwnMessage => _comment.UserId == _currentUserId;
        
        public void UpdateComment(TaskComment updatedComment)
        {
            _comment = updatedComment;
            OnPropertyChanged(nameof(Comment));
        }
        
        public void NotifyCommentChanged()
        {
            OnPropertyChanged(nameof(Comment));
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _executeAsync;
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;
        
        public RelayCommand(Func<Task> executeAsync, Func<bool> canExecute = null)
        {
            _executeAsync = executeAsync;
            _canExecute = canExecute;
        }
        
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        
        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }
        
        public async void Execute(object parameter)
        {
            if (_executeAsync != null)
            {
                await _executeAsync();
            }
            else
            {
                _execute?.Invoke();
            }
        }
        
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}