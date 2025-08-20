using Admin_Tasks.ViewModels;
using Admin_Tasks.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System;
using System.Linq;
using System.Windows.Controls.Primitives;
using Admin_Tasks.Views;

namespace Admin_Tasks.Views
{
    /// <summary>
    /// Interaktionslogik für MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        private readonly IServiceProvider _serviceProvider;
        
        public MainView(MainViewModel viewModel, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            DataContext = viewModel;
            _serviceProvider = serviceProvider;
            
            // Event-Handler für ViewModel-Events
            viewModel.LogoutRequested += OnLogoutRequested;
            viewModel.TaskEditRequested += OnTaskEditRequested;
            viewModel.TaskCreateRequested += OnTaskCreateRequested;
            viewModel.ChatOverviewRequested += OnChatOverviewRequested;
            viewModel.NewsRequested += OnNewsRequested;
        }
        
        private void OnLogoutRequested(object? sender, EventArgs e)
        {
            // Hauptfenster schließen und Login anzeigen
            var app = (App)Application.Current;
            app.ShowLoginWindow();
            Close();
        }
        
        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            ProfilePopup.IsOpen = !ProfilePopup.IsOpen;
        }
        
        private void OnTaskEditRequested(object? sender, TaskItem task)
        {
            try
            {
                var taskEditView = _serviceProvider.GetRequiredService<TaskEditView>();
                var taskEditViewModel = taskEditView.DataContext as TaskEditViewModel;
                
                if (task != null && taskEditViewModel != null)
                {
                    taskEditViewModel.LoadTask(task);
                }
                
                taskEditView.Owner = this;
                
                if (taskEditView.ShowDialog() == true)
                {
                    // Aufgabenliste aktualisieren
                    if (DataContext is MainViewModel vm)
                    {
                        _ = vm.RefreshCommand.ExecuteAsync(null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Aufgabenbearbeitung: {ex.Message}", 
                               "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void OnTaskCreateRequested(object? sender, EventArgs e)
        {
            try
            {
                var taskEditView = _serviceProvider.GetRequiredService<TaskEditView>();
                var taskEditViewModel = taskEditView.DataContext as TaskEditViewModel;
                
                if (taskEditViewModel != null)
                {
                    taskEditViewModel.PrepareForNewTask();
                    
                    // Event-Handler für automatische Aktualisierung nach Task-Erstellung
                    taskEditViewModel.TaskSaved += async (s, task) =>
                    {
                        // Aufgabenliste mit Ladeanimation aktualisieren
                        if (DataContext is MainViewModel vm)
                        {
                            await vm.RefreshCommand.ExecuteAsync(null);
                        }
                    };
                }
                
                taskEditView.Owner = this;
                
                taskEditView.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Erstellen einer neuen Aufgabe: {ex.Message}", 
                               "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void CreateCustomFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomFolderDialog()
            {
                Owner = this
            };
            
            if (dialog.ShowDialog() == true && DataContext is MainViewModel vm)
            {
                var customFolder = new CustomFolder
                {
                    Name = dialog.FolderName,
                    Description = dialog.FolderDescription,
                    Color = dialog.FolderColor,
                    TaskIds = new List<int>()
                };
                _ = vm.CreateCustomFolderCommand.ExecuteAsync(customFolder);
            }
        }
        
        private void DeleteCustomFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.CommandParameter is CustomFolder folder)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie den Ordner '{folder.Name}' wirklich löschen?\n\nDie zugeordneten Aufgaben werden nicht gelöscht, sondern nur aus dem Ordner entfernt.",
                    "Ordner löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                    
                if (result == MessageBoxResult.Yes && DataContext is MainViewModel vm)
                {
                    _ = vm.DeleteFolderCommand.ExecuteAsync(folder);
                }
            }
        }
        
        private void EditCustomFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.CommandParameter is CustomFolder folder)
            {
                var dialog = new CustomFolderDialog(folder)
                {
                    Owner = this
                };
                
                if (dialog.ShowDialog() == true && DataContext is MainViewModel vm)
                {
                    // Ordner aktualisieren
                    folder.Name = dialog.FolderName;
                    folder.Description = dialog.FolderDescription;
                    folder.Color = dialog.FolderColor;
                    
                    _ = vm.RefreshCommand.ExecuteAsync(null);
                }
            }
        }
        
        // Drag-and-Drop Event-Handler für Custom Folders
        
        private void CustomFolderItem_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TaskItem)) || e.Data.GetDataPresent("TaskItem"))
            {
                if (sender is Border border)
                {
                    border.Background = Application.Current.Resources["AccentBrush"] as System.Windows.Media.Brush;
                    border.Opacity = 0.7;
                }
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void CustomFolderItem_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TaskItem)) || e.Data.GetDataPresent("TaskItem"))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }
        
        private void CustomFolderItem_Drop(object sender, DragEventArgs e)
        {
            if (sender is Border border && border.DataContext is CustomFolder folder)
            {
                TaskItem? task = null;
                if (e.Data.GetDataPresent(typeof(TaskItem)))
                {
                    task = e.Data.GetData(typeof(TaskItem)) as TaskItem;
                }
                else if (e.Data.GetDataPresent("TaskItem"))
                {
                    task = e.Data.GetData("TaskItem") as TaskItem;
                }

                if (task != null)
                {
                    if (DataContext is MainViewModel vm)
                    {
                        _ = vm.AddTaskToFolderCommand.ExecuteAsync((folder, task));
                    }

                    // Visuelles Feedback zurücksetzen
                    ResetFolderVisualFeedback(border);
                    e.Handled = true;
                }
            }
        }
        
        private void CustomFolderItem_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is Border border)
            {
                // Prüfen ob wir wirklich das Element verlassen (nicht nur ein Child-Element)
                var position = e.GetPosition(border);
                var hitTest = border.InputHitTest(position);
                
                if (hitTest == null || !IsDescendantOf(hitTest as DependencyObject, border))
                {
                    ResetFolderVisualFeedback(border);
                }
            }
            e.Handled = true;
        }
        
        private void ResetFolderVisualFeedback(Border border)
        {
            border.Background = System.Windows.Media.Brushes.Transparent;
            border.Opacity = 1.0;
        }
        
        private bool IsDescendantOf(DependencyObject child, DependencyObject parent)
        {
            if (child == null || parent == null)
                return false;
                
            DependencyObject current = child;
            while (current != null)
            {
                if (current == parent)
                    return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }
        
        private void CreateFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderNameDialog()
            {
                Owner = this
            };
            
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.CreateFolderCommand.Execute(dialog.FolderName);
                }
            }
        }
        
        private async void OnChatOverviewRequested(object? sender, EventArgs e)
        {
            try
            {
                var chatOverviewView = _serviceProvider.GetRequiredService<ChatOverviewView>();
                
                // Event für Task-Details abonnieren und ViewModel initialisieren
                if (chatOverviewView.DataContext is ChatOverviewViewModel chatViewModel)
                {
                    chatViewModel.TaskDetailsRequested += OnTaskDetailsRequestedFromChat;
                }
                
                // ViewModel initialisieren, da das Loaded-Event möglicherweise nicht ausgelöst wird
                await chatOverviewView.InitializeViewModelAsync();
                
                var chatWindow = new Window
                {
                    Title = "Chat-Übersicht",
                    Content = chatOverviewView,
                    Owner = this,
                    Width = 1200,
                    Height = 800,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                
                // Event nach dem Schließen des Fensters wieder abmelden
                chatWindow.Closed += (s, args) =>
                {
                    if (chatOverviewView.DataContext is ChatOverviewViewModel vm)
                    {
                        vm.TaskDetailsRequested -= OnTaskDetailsRequestedFromChat;
                        vm.Dispose();
                    }
                };
                
                chatWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Chat-Übersicht: {ex.Message}", 
                               "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void OnNewsRequested(object? sender, EventArgs e)
        {
            try
            {
                var newsView = _serviceProvider.GetRequiredService<NewsView>();
                
                // ViewModel initialisieren
                if (newsView.DataContext is NewsViewModel newsViewModel)
                {
                    newsViewModel.RefreshCommand.Execute(null);
                }
                
                var newsWindow = new Window
                {
                    Title = "📰 News - Benachrichtigungen",
                    Content = newsView,
                    Owner = this,
                    Width = 1000,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Background = Application.Current.Resources["BackgroundBrush"] as Brush
                };
                
                newsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der News-Seite: {ex.Message}", 
                               "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void OnTaskDetailsRequestedFromChat(object? sender, int taskId)
        {
            try
            {
                // Task anhand der ID laden und Task-Edit-Dialog öffnen
                if (DataContext is MainViewModel mainViewModel)
                {
                    var task = mainViewModel.Tasks.FirstOrDefault(t => t.Id == taskId);
                    if (task != null)
                    {
                        OnTaskEditRequested(this, task);
                    }
                    else
                    {
                        MessageBox.Show("Die angeforderte Aufgabe konnte nicht gefunden werden.", 
                                       "Aufgabe nicht gefunden", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Task-Details: {ex.Message}", 
                               "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void DeleteFolder_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel && !string.IsNullOrWhiteSpace(viewModel.SelectedFolder))
            {
                // Nur benutzerdefinierte Ordner können gelöscht werden
                var customFolder = viewModel.CustomFolders.FirstOrDefault(f => f.Name == viewModel.SelectedFolder);
                if (customFolder != null)
                {
                    var result = MessageBox.Show(
                        $"Möchten Sie den Ordner '{viewModel.SelectedFolder}' wirklich löschen?",
                        "Ordner löschen",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                        
                    if (result == MessageBoxResult.Yes)
                    {
                        viewModel.DeleteFolderCommand.Execute(viewModel.SelectedFolder);
                    }
                }
                else
                {
                    MessageBox.Show("Standardordner können nicht gelöscht werden.", "Information", 
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        
        private void TaskList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ListView listView && listView.SelectedItem is TaskItem selectedTask)
            {
                try
                {
                    var taskDetailView = new TaskDetailView(selectedTask)
                    {
                        Owner = this
                    };
                    
                    // Event-Handler für Task-Updates
                    taskDetailView.TaskUpdated += (task) =>
                    {
                        // Aufgabenliste aktualisieren
                        if (DataContext is MainViewModel vm)
                        {
                            _ = vm.RefreshCommand.ExecuteAsync(null);
                        }
                    };
                    
                    taskDetailView.TaskDeleted += (task) =>
                    {
                        // Aufgabenliste aktualisieren
                        if (DataContext is MainViewModel vm)
                        {
                            _ = vm.RefreshCommand.ExecuteAsync(null);
                        }
                    };
                    
                    taskDetailView.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Öffnen der Aufgabendetails: {ex.Message}", 
                                   "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // Drag-and-Drop Event-Handler für Tasks
        private Point _startPoint;
        private bool _isDragging = false;
        
        private void TaskList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
            _isDragging = false;
        }
        
        private void TaskList_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;
                
                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    var listView = sender as ListView;
                    var listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
                    
                    if (listViewItem != null && listViewItem.DataContext is TaskItem task)
                    {
                        _isDragging = true;
                        DataObject dragData = new DataObject(typeof(TaskItem), task);
                        DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Move);
                        _isDragging = false;
                    }
                }
            }
        }
        
        private static T? FindAncestor<T>(DependencyObject current) where T : class
        {
            do
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }
        
        private async void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[MainView] ForwardButton_Click aufgerufen");
            
            if (sender is Button button && button.Tag is TaskItem task && DataContext is MainViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine($"[MainView] Task gefunden: {task.Title}, ID: {task.Id}");
                var result = await viewModel.ShowForwardTaskDialogAsync(task);
                System.Diagnostics.Debug.WriteLine($"[MainView] Weiterleitung Ergebnis: {result}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[MainView] Sender, Tag oder DataContext ist null/falsch");
            }
        }
        
        protected override void OnClosed(EventArgs e)
        {
            // Event-Handler entfernen
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.LogoutRequested -= OnLogoutRequested;
                viewModel.TaskEditRequested -= OnTaskEditRequested;
                viewModel.TaskCreateRequested -= OnTaskCreateRequested;
            }
            base.OnClosed(e);
        }
    }
}