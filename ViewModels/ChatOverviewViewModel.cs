using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Admin_Tasks.Models;
using Admin_Tasks.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Admin_Tasks.ViewModels
{
    public class ChatOverviewViewModel : INotifyPropertyChanged
    {
        private readonly IChatService _chatService;
        private readonly IAuthenticationService _authService;
        private readonly SignalRService _signalRService;
        private TaskChatSummary? _selectedChat;
        private ChatViewModel? _selectedChatViewModel;
        private bool _isLoading;

        private readonly object _filterLock = new object();
        
        public ChatOverviewViewModel(IChatService chatService, IAuthenticationService authService, SignalRService signalRService)
        {
            _chatService = chatService;
            _authService = authService;
            _signalRService = signalRService;
            
            ChatSummaries = new ObservableCollection<TaskChatSummary>();
            FilteredChatSummaries = new ObservableCollection<TaskChatSummary>();
            
            // Commands
            RefreshCommand = new RelayCommand(async () => await RefreshChatsAsync());
            SelectChatCommand = new RelayCommand<TaskChatSummary>(async (chat) => await SelectChatAsync(chat));
            ToggleFavoriteCommand = new RelayCommand<TaskChatSummary>(async (chat) => await ToggleFavoriteAsync(chat));
            ToggleMuteCommand = new RelayCommand<TaskChatSummary>(async (chat) => await ToggleMuteAsync(chat));
            ShowTaskDetailsCommand = new RelayCommand<TaskChatSummary>((chat) => ShowTaskDetails(chat));
            
            // Subscribe to SignalR events for real-time updates
            _signalRService.NewCommentReceived += OnNewCommentReceived;
            _signalRService.CommentUpdated += OnCommentUpdated;
            _signalRService.CommentDeleted += OnCommentDeleted;
        }
        
        public ObservableCollection<TaskChatSummary> ChatSummaries { get; }
        public ObservableCollection<TaskChatSummary> FilteredChatSummaries { get; }
        
        public TaskChatSummary? SelectedChat
        {
            get => _selectedChat;
            set
            {
                _selectedChat = value;
                OnPropertyChanged();
            }
        }
        
        public ChatViewModel? SelectedChatViewModel
        {
            get => _selectedChatViewModel;
            set
            {
                _selectedChatViewModel = value;
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
        
        public int TotalUnreadCount => ChatSummaries.Sum(c => c.UnreadCount);
        
        public ICommand RefreshCommand { get; }
        public ICommand SelectChatCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }
        public ICommand ToggleMuteCommand { get; }
        public ICommand ShowTaskDetailsCommand { get; }
        
        public async Task InitializeAsync()
        {
            await RefreshChatsAsync();
        }
        
        private async Task RefreshChatsAsync()
        {
            System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] RefreshChatsAsync called. CurrentUser: {_authService.CurrentUser?.Username ?? "null"}");
            
            if (_authService.CurrentUser == null) 
            {
                System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] CurrentUser is null, returning early");
                return;
            }
            
            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] Loading chats for user: {_authService.CurrentUser.Username}");
                
                var chatSummaries = await _chatService.GetActiveChatSummariesAsync(_authService.CurrentUser.Id);
                System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] Retrieved {chatSummaries.Count()} chat summaries");
                
                App.Current.Dispatcher.Invoke(() =>
                {
                    ChatSummaries.Clear();
                    
                    // Duplikate-sichere Hinzufügung zur Hauptsammlung
                    var addedTaskIds = new HashSet<int>();
                    foreach (var summary in chatSummaries)
                    {
                        if (!addedTaskIds.Contains(summary.TaskId))
                        {
                            ChatSummaries.Add(summary);
                            addedTaskIds.Add(summary.TaskId);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] Duplicate TaskId {summary.TaskId} prevented in ChatSummaries");
                        }
                    }
                    
                    FilterChats();
                    OnPropertyChanged(nameof(TotalUnreadCount));
                    System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] Added {ChatSummaries.Count} chats to collection");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] Error refreshing chats: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async Task SelectChatAsync(TaskChatSummary? chat)
        {
            if (chat == null) return;
            
            try
            {
                SelectedChat = chat;
                
                // Create new ChatViewModel for selected chat
                var chatViewModel = App.ServiceProvider.GetRequiredService<ChatViewModel>();
                await chatViewModel.InitializeForTask(chat.TaskId);
                
                SelectedChatViewModel = chatViewModel;
                
                // Reset unread count for this chat
                chat.UnreadCount = 0;
                OnPropertyChanged(nameof(TotalUnreadCount));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] Error selecting chat: {ex}");
            }
        }
        
        private async Task ToggleFavoriteAsync(TaskChatSummary? chat)
        {
            if (chat == null || _authService.CurrentUser == null) return;
            
            try
            {
                var newFavoriteState = !chat.IsFavorite;
                var success = await _chatService.MarkChatAsFavoriteAsync(chat.TaskId, _authService.CurrentUser.Id, newFavoriteState);
                
                if (success)
                {
                    chat.IsFavorite = newFavoriteState;
                    FilterChats(); // Re-sort if favorites are prioritized
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] Error toggling favorite: {ex}");
            }
        }
        
        private async Task ToggleMuteAsync(TaskChatSummary? chat)
        {
            if (chat == null || _authService.CurrentUser == null) return;
            
            try
            {
                var newMuteState = !chat.IsMuted;
                var success = await _chatService.MuteChatAsync(chat.TaskId, _authService.CurrentUser.Id, newMuteState);
                
                if (success)
                {
                    chat.IsMuted = newMuteState;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] Error toggling mute: {ex}");
            }
        }
        
        private void FilterChats()
        {
            lock (_filterLock)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] FilterChats called. ChatSummaries count: {ChatSummaries.Count}, FilteredChatSummaries count before: {FilteredChatSummaries.Count}");
                
                // Erst Duplikate aus ChatSummaries entfernen (falls vorhanden)
                // Verwende Distinct() mit der implementierten Equals-Methode
                var uniqueChats = ChatSummaries.Distinct().AsEnumerable();
                
                System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] Unique chats after Distinct(): {uniqueChats.Count()}");
                
                var filtered = uniqueChats;
                
                // No search filtering - show all chats
                
                // Sort: Favorites first, then by last activity
                filtered = filtered.OrderByDescending(c => c.IsFavorite)
                                  .ThenByDescending(c => c.LastActivity);
                
                var filteredList = filtered.ToList();
                System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] Filtered list count: {filteredList.Count}");
                
                // Debug: Zeige alle TaskIds in der gefilterten Liste
                var taskIds = filteredList.Select(c => c.TaskId).ToList();
                System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] Filtered TaskIds: [{string.Join(", ", taskIds)}]");
                
                App.Current.Dispatcher.Invoke(() =>
                {
                    // Immer komplett neu erstellen, um sicherzustellen, dass keine Duplikate entstehen
                    FilteredChatSummaries.Clear();
                    
                    foreach (var chat in filteredList)
                    {
                        FilteredChatSummaries.Add(chat);
                        System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] Added chat with TaskId: {chat.TaskId}, Title: {chat.TaskTitle}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] FilteredChatSummaries updated. Final count: {FilteredChatSummaries.Count}");
                });
            }
        }
        
        private void OnNewCommentReceived(object? sender, NewCommentEventArgs e)
        {
            App.Current.Dispatcher.Invoke(async () =>
            {
                var existingChat = ChatSummaries.FirstOrDefault(c => c.TaskId == e.TaskId);
                if (existingChat != null)
                {
                    // Update existing chat
                    existingChat.LastMessageContent = e.Content.Length > 50 ? e.Content.Substring(0, 50) + "..." : e.Content;
                    existingChat.LastMessageTime = e.CreatedAt;
                    existingChat.LastMessageAuthor = new User { Id = e.User.Id, Username = e.User.Username };
                    existingChat.LastMessageAuthorId = e.User.Id;
                    existingChat.IsLastMessageFromCurrentUser = e.User.Id == _authService.CurrentUser?.Id;
                    existingChat.LastActivity = e.CreatedAt;
                    existingChat.TotalMessageCount++;
                    
                    // Increment unread count if not from current user and not currently selected
                    if (e.User.Id != _authService.CurrentUser?.Id && SelectedChat?.TaskId != e.TaskId)
                    {
                        existingChat.UnreadCount++;
                    }
                    
                    OnPropertyChanged(nameof(TotalUnreadCount));
                    FilterChats(); // Re-filter to update sorting
                }
                else
                {
                    // If the chat is new, force a full refresh to correctly load it.
                    System.Diagnostics.Debug.WriteLine($"[ChatOverviewViewModel] New chat for TaskId {e.TaskId} detected, forcing a refresh to add it.");
                    await RefreshChatsAsync();
                }
            });
        }
        
        private void OnCommentUpdated(object? sender, CommentUpdatedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var existingChat = ChatSummaries.FirstOrDefault(c => c.TaskId == e.TaskId);
                if (existingChat != null)
                {
                    // Update last message if this was the most recent comment
                    if (existingChat.LastMessageTime.HasValue && 
                        existingChat.LastMessageTime.Value <= (e.UpdatedAt ?? DateTime.UtcNow))
                    {
                        existingChat.LastMessageContent = e.Content.Length > 50 ? e.Content.Substring(0, 50) + "..." : e.Content;
                        existingChat.LastActivity = e.UpdatedAt ?? DateTime.UtcNow;
                    }
                    
                    FilterChats();
                }
            });
        }
        
        private void OnCommentDeleted(object? sender, CommentDeletedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var existingChat = ChatSummaries.FirstOrDefault(c => c.TaskId == e.TaskId);
                if (existingChat != null)
                {
                    existingChat.TotalMessageCount = Math.Max(0, existingChat.TotalMessageCount - 1);
                    
                    // Just update the count and filter, user can manually refresh for last message updates
                    FilterChats();
                }
            });
        }
        
        public void Dispose()
        {
            _signalRService.NewCommentReceived -= OnNewCommentReceived;
            _signalRService.CommentUpdated -= OnCommentUpdated;
            _signalRService.CommentDeleted -= OnCommentDeleted;
            
            SelectedChatViewModel?.Dispose();
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<int>? TaskDetailsRequested;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private void ShowTaskDetails(TaskChatSummary? chat)
        {
            if (chat != null)
            {
                TaskDetailsRequested?.Invoke(this, chat.TaskId);
            }
        }
    }
    
    // RelayCommand with generic parameter support
    public class RelayCommand<T> : ICommand
    {
        private readonly Func<T?, Task> _executeAsync;
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool> _canExecute;
        
        public RelayCommand(Func<T?, Task> executeAsync, Func<T?, bool> canExecute = null)
        {
            _executeAsync = executeAsync;
            _canExecute = canExecute;
        }
        
        public RelayCommand(Action<T?> execute, Func<T?, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        
        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke((T?)parameter) ?? true;
        }
        
        public async void Execute(object? parameter)
        {
            if (_executeAsync != null)
            {
                await _executeAsync((T?)parameter);
            }
            else
            {
                _execute?.Invoke((T?)parameter);
            }
        }
        
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}