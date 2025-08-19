using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Admin_Tasks.ViewModels;
using Admin_Tasks.Models;
using Admin_Tasks.Services;
using Admin_Tasks.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Admin_Tasks.Views
{
    public partial class TaskDetailView : Window
    {
        public TaskDetailViewModel ViewModel { get; private set; }
        private AttachmentDisplayControl _attachmentDisplayControl;
        private ChatControl _chatControl;
        
        public TaskDetailView(TaskItem task)
        {
            InitializeComponent();
            
            // ViewModel aus DI Container holen und Task setzen
            ViewModel = App.ServiceProvider.GetRequiredService<TaskDetailViewModel>();
            ViewModel.SetTask(task);
            DataContext = ViewModel;
            
            // AttachmentService aus DI Container holen
            var attachmentService = App.ServiceProvider.GetRequiredService<IAttachmentService>();
            
            // AttachmentDisplayControl initialisieren
            InitializeAttachmentDisplay(attachmentService, task.Id);
            
            // ChatControl initialisieren
            InitializeChatControl(task.Id);
            
            // Event-Handler für Fenster-Events
            Loaded += TaskDetailView_Loaded;
            KeyDown += TaskDetailView_KeyDown;
        }
        
        private async void TaskDetailView_Loaded(object sender, RoutedEventArgs e)
        {
            // Fokus auf das Fenster setzen
            Focus();
            
            // Benutzer laden
            await ViewModel.LoadUsersAsync();
            
            // Anhänge laden
            if (_attachmentDisplayControl != null)
            {
                await _attachmentDisplayControl.LoadAttachmentsAsync(ViewModel.Task.Id);
            }
        }
        
        private void InitializeAttachmentDisplay(IAttachmentService attachmentService, int taskId)
        {
            _attachmentDisplayControl = new AttachmentDisplayControl(attachmentService)
            {
                CanDeleteAttachments = ViewModel.CanDeleteAttachments // Berechtigungen aus ViewModel verwenden
            };
            
            // Event-Handler für AttachmentDisplayControl
            _attachmentDisplayControl.AttachmentDeleted += OnAttachmentDeleted;
            _attachmentDisplayControl.DeleteError += OnAttachmentDeleteError;
            _attachmentDisplayControl.AttachmentViewRequested += OnAttachmentViewRequested;
            
            // Control in den ContentPresenter einbetten
            AttachmentDisplayPresenter.Content = _attachmentDisplayControl;
        }
        
        private async void InitializeChatControl(int taskId)
        {
            // ChatViewModel aus DI Container holen
            var chatViewModel = App.ServiceProvider.GetRequiredService<ChatViewModel>();
            
            // ChatControl erstellen und ViewModel zuweisen
            _chatControl = new ChatControl
            {
                DataContext = chatViewModel
            };
            
            // Control in den ContentPresenter einbetten
            ChatControlPresenter.Content = _chatControl;
            
            // Asynchrone Initialisierung
            await chatViewModel.InitializeForTask(taskId);
        }
        
        private void OnAttachmentDeleted(object sender, AttachmentDeletedEventArgs e)
        {
            // ViewModel über die Löschung informieren
            if (ViewModel.Attachments.Contains(e.DeletedAttachment))
            {
                ViewModel.Attachments.Remove(e.DeletedAttachment);
            }
            
            MessageBox.Show(
                $"Anhang '{e.DeletedAttachment.FileName}' wurde erfolgreich gelöscht.",
                "Anhang gelöscht",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        
        private void OnAttachmentDeleteError(object sender, string errorMessage)
        {
            MessageBox.Show(
                errorMessage,
                "Fehler beim Löschen",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        
        private void OnAttachmentViewRequested(object sender, TaskAttachment attachment)
        {
            // Öffne das Bild mit dem Standard-Programm
            AttachmentDisplayControl.OpenFileWithDefaultProgram(attachment.FilePath);
        }
        
        private void TaskDetailView_KeyDown(object sender, KeyEventArgs e)
        {
            // ESC-Taste zum Schließen
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        private async void EditTask_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Task != null)
            {
                try
                {
                    var taskEditView = App.ServiceProvider.GetRequiredService<TaskEditView>();
                    var taskEditViewModel = taskEditView.DataContext as TaskEditViewModel;
                    
                    if (taskEditViewModel != null)
                    {
                        taskEditViewModel.SetTask(ViewModel.Task);
                    }
                    
                    taskEditView.Owner = this;
                    
                    if (taskEditView.ShowDialog() == true)
                    {
                        // Task wurde erfolgreich bearbeitet, Event auslösen
                        TaskUpdated?.Invoke(ViewModel.Task);
                        
                        // Task-Details aktualisieren
                        ViewModel.RefreshTask();
                        
                        // Anhänge neu laden
                        if (_attachmentDisplayControl != null)
                        {
                            await _attachmentDisplayControl.LoadAttachmentsAsync(ViewModel.Task.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Öffnen der Aufgabenbearbeitung: {ex.Message}", 
                                   "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private async void CompleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Task != null)
            {
                try
                {
                    // Create services
                    var taskService = App.ServiceProvider.GetRequiredService<ITaskService>();
                    var attachmentService = App.ServiceProvider.GetRequiredService<IAttachmentService>();
                    var authService = App.ServiceProvider.GetRequiredService<IAuthenticationService>();
                    
                    // Create completion dialog
                    var completionViewModel = new TaskCompletionViewModel(
                        ViewModel.Task, 
                        taskService, 
                        attachmentService,
                        authService);
                    
                    var completionDialog = new TaskCompletionDialog(completionViewModel)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    
                    var result = completionDialog.ShowDialog();
                    
                    if (result == true)
                    {
                        // Task was completed successfully
                        ViewModel.RefreshTask();
                        
                        MessageBox.Show(
                            "Die Aufgabe wurde erfolgreich abgeschlossen.",
                            "Erfolg",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        
                        // Notify parent that task was updated
                        TaskUpdated?.Invoke(ViewModel.Task);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Fehler beim Öffnen des Abschluss-Dialogs: {ex.Message}",
                        "Fehler",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
        
        private async void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Task != null)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie die Aufgabe '{ViewModel.Task.Title}' wirklich löschen?\n\nDiese Aktion kann nicht rückgängig gemacht werden.",
                    "Aufgabe löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var taskService = App.ServiceProvider.GetRequiredService<ITaskService>();
                        var success = await taskService.DeleteTaskAsync(ViewModel.Task.Id);
                        
                        if (success)
                        {
                            // Task wurde erfolgreich gelöscht, Event auslösen
                            TaskDeleted?.Invoke(ViewModel.Task);
                            
                            MessageBox.Show(
                                "Die Aufgabe wurde erfolgreich gelöscht.",
                                "Erfolg",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                            
                            Close();
                        }
                        else
                        {
                            MessageBox.Show(
                                "Fehler beim Löschen der Aufgabe. Möglicherweise haben Sie keine Berechtigung oder die Aufgabe existiert nicht mehr.",
                                "Fehler",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Fehler beim Löschen der Aufgabe: {ex.Message}",
                            "Fehler",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }
        
        private async void ForwardTask_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Task != null && ViewModel.SelectedUserForForwarding != null)
            {
                var selectedUser = ViewModel.SelectedUserForForwarding;
                var userName = selectedUser.Id == 0 ? "Ohne Besitzer" : $"{selectedUser.FirstName} {selectedUser.LastName}";
                
                var result = MessageBox.Show(
                    $"Möchten Sie die Aufgabe '{ViewModel.Task.Title}' an {userName} weiterleiten?",
                    "Aufgabe weiterleiten",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await ViewModel.ForwardTaskAsync();
                        TaskUpdated?.Invoke(ViewModel.Task);
                        
                        MessageBox.Show(
                            $"Die Aufgabe wurde erfolgreich an {userName} weitergeleitet.",
                            "Erfolg",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show(
                            $"Fehler beim Weiterleiten der Aufgabe: {ex.Message}",
                            "Fehler",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show(
                    "Bitte wählen Sie einen Benutzer aus, an den die Aufgabe weitergeleitet werden soll.",
                    "Auswahl erforderlich",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        
        // Events für Kommunikation mit dem Hauptfenster
        public event System.Action<TaskItem> TaskUpdated;
        public event System.Action<TaskItem> TaskDeleted;
    }
}