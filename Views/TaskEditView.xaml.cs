using Admin_Tasks.ViewModels;
using Admin_Tasks.Models;
using Admin_Tasks.Services;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Admin_Tasks.Views
{
    /// <summary>
    /// Interaktionslogik für TaskEditView.xaml
    /// </summary>
    public partial class TaskEditView : Window
    {
        private ImageUploadControl? _imageUploadControl;
        private AttachmentDisplayControl? _attachmentDisplayControl;
        private IAttachmentService? _attachmentService;
        
        public TaskEditView(TaskEditViewModel viewModel)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[TaskEditView] Initialisierung gestartet");
                
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("[TaskEditView] InitializeComponent abgeschlossen");
                
                DataContext = viewModel;
                System.Diagnostics.Debug.WriteLine($"[TaskEditView] DataContext gesetzt: {viewModel?.GetType().Name}");
                
                // Event-Handler für ViewModel-Events
                viewModel.TaskSaved += OnTaskSaved;
                viewModel.Cancelled += OnEditCancelled;
                
                System.Diagnostics.Debug.WriteLine("[TaskEditView] Event-Handler registriert");
                
                // Log AvailableUsers count for debugging
                if (viewModel.AvailableUsers != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[TaskEditView] AvailableUsers Count: {viewModel.AvailableUsers.Count}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[TaskEditView] AvailableUsers ist null!");
                }
                
                // Attachment-Funktionalität initialisieren
                InitializeAttachmentControls();
                
                System.Diagnostics.Debug.WriteLine("[TaskEditView] Initialisierung erfolgreich abgeschlossen");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskEditView] FEHLER bei Initialisierung: {ex}");
                throw;
            }
        }
        
        private void InitializeAttachmentControls()
        {
            try
            {
                _attachmentService = App.ServiceProvider.GetRequiredService<IAttachmentService>();
                var authService = App.ServiceProvider.GetRequiredService<IAuthenticationService>();

                if (_attachmentService == null || authService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TaskEditView] Services nicht verfügbar");
                    return;
                }

                var viewModel = DataContext as TaskEditViewModel;
                if (viewModel == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TaskEditView] ViewModel nicht verfügbar");
                    return;
                }

                // ImageUploadControl initialisieren
                _imageUploadControl = new ImageUploadControl(_attachmentService);

                // AttachmentDisplayControl initialisieren
                _attachmentDisplayControl = new AttachmentDisplayControl(_attachmentService)
                {
                    CanDeleteAttachments = true // Dies sollte an die Berechtigungen des Benutzers gebunden sein
                };

                if (viewModel.IsEditMode && viewModel.Task != null)
                {
                    _imageUploadControl.TaskId = viewModel.Task.Id;
                    _imageUploadControl.UserId = viewModel.Task.CreatedByUserId;
                    _attachmentDisplayControl.CanDeleteAttachments = viewModel.CanDeleteAttachments;
                }
                else
                {
                    _imageUploadControl.TaskId = 0; // Kennzeichnet eine neue Aufgabe
                    _imageUploadControl.UserId = authService.CurrentUser?.Id ?? 0;
                    _attachmentDisplayControl.CanDeleteAttachments = true; // Bei neuen Tasks kann man immer löschen
                }

                // Event-Handler registrieren
                _attachmentDisplayControl.AttachmentDeleted += OnAttachmentDeleted;
                _attachmentDisplayControl.DeleteError += OnAttachmentDeleteError;
                _attachmentDisplayControl.AttachmentViewRequested += OnAttachmentViewRequested;
                _imageUploadControl.ImagesUploaded += OnImageUploaded;
                _imageUploadControl.UploadError += OnUploadError;

                // Controls zu den ContentPresentern hinzufügen
                ImageUploadPresenter.Content = _imageUploadControl;
                AttachmentDisplayPresenter.Content = _attachmentDisplayControl;

                // Bestehende Anhänge laden
                LoadExistingAttachments();

                System.Diagnostics.Debug.WriteLine("[TaskEditView] Attachment-Controls erfolgreich initialisiert");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskEditView] Fehler bei Attachment-Initialisierung: {ex}");
            }
        }

        private async void LoadExistingAttachments()
        {
            try
            {
                var viewModel = DataContext as TaskEditViewModel;
                if (viewModel?.Task != null && viewModel.IsEditMode && _attachmentDisplayControl != null)
                {
                    await _attachmentDisplayControl.LoadAttachmentsAsync(viewModel.Task.Id);
                    System.Diagnostics.Debug.WriteLine($"[TaskEditView] Anhänge für Task {viewModel.Task.Id} geladen");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskEditView] Fehler beim Laden der Anhänge: {ex}");
            }
        }
        
        private async void OnAttachmentDeleted(object? sender, AttachmentDeletedEventArgs e)
        {
            try
            {
                if (_attachmentService != null)
                {
                    await _attachmentService.DeleteAttachmentAsync(e.DeletedAttachment.Id);
                    System.Diagnostics.Debug.WriteLine($"[TaskEditView] Anhang gelöscht: ID {e.DeletedAttachment.Id}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskEditView] Fehler beim Löschen des Anhangs: {ex}");
                MessageBox.Show($"Fehler beim Löschen des Anhangs: {ex.Message}", "Lösch-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void OnAttachmentDeleteError(object? sender, string errorMessage)
        {
            MessageBox.Show($"Fehler beim Löschen: {errorMessage}", "Lösch-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
        private void OnAttachmentViewRequested(object? sender, TaskAttachment attachment)
        {
            try
            {
                if (System.IO.File.Exists(attachment.FilePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = attachment.FilePath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("Die Datei wurde nicht gefunden.", "Datei nicht gefunden", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Datei: {ex.Message}", "Öffnen-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void OnTaskSaved(object? sender, TaskItem task)
        {
            DialogResult = true;
            Close();
        }
        
        private void OnEditCancelled(object? sender, EventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private async void OnImageUploaded(object? sender, ImagesUploadedEventArgs e)
        {
            try
            {
                if (DataContext is TaskEditViewModel viewModel)
                {
                    foreach (var attachment in e.Attachments)
                    {
                        viewModel.AddAttachment(attachment);
                        
                        // Sofortige Anzeige im AttachmentDisplayControl
                        if (_attachmentDisplayControl != null)
                        {
                            _attachmentDisplayControl.AddAttachment(attachment);
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"[TaskEditView] {e.Attachments.Count} Bilder hochgeladen und angezeigt");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskEditView] Fehler beim Hinzufügen der Anhänge: {ex}");
                MessageBox.Show($"Fehler beim Hinzufügen der Anhänge: {ex.Message}", "Upload-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void OnUploadError(object? sender, string errorMessage)
        {
            MessageBox.Show($"Upload-Fehler: {errorMessage}", "Upload-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
        // Event-Handler für Kategorie-Funktionalität
        private void CreateNewCategory_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TaskEditViewModel viewModel)
            {
                viewModel.IsCreatingNewCategory = true;
                viewModel.NewCategoryName = string.Empty;
            }
        }
        
        private void CancelNewCategory_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TaskEditViewModel viewModel)
            {
                viewModel.IsCreatingNewCategory = false;
                viewModel.NewCategoryName = string.Empty;
            }
        }
        
        protected override void OnClosed(EventArgs e)
        {
            // Event-Handler entfernen
            if (DataContext is TaskEditViewModel viewModel)
            {
                viewModel.TaskSaved -= OnTaskSaved;
                viewModel.Cancelled -= OnEditCancelled;
            }
            
            // Attachment-Event-Handler entfernen
            if (_attachmentDisplayControl != null)
            {
                _attachmentDisplayControl.AttachmentDeleted -= OnAttachmentDeleted;
                _attachmentDisplayControl.DeleteError -= OnAttachmentDeleteError;
                _attachmentDisplayControl.AttachmentViewRequested -= OnAttachmentViewRequested;
            }
            
            if (_imageUploadControl != null)
            {
                _imageUploadControl.ImagesUploaded -= OnImageUploaded;
                _imageUploadControl.UploadError -= OnUploadError;
            }
            
            base.OnClosed(e);
        }
    }
}