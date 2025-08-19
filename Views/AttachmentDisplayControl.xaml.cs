using Admin_Tasks.Models;
using Admin_Tasks.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Admin_Tasks.Views
{
    /// <summary>
    /// UserControl für die Anzeige von Task-Anhängen mit Vorschau und Löschfunktion
    /// </summary>
    public partial class AttachmentDisplayControl : UserControl, INotifyPropertyChanged
    {
        private readonly IAttachmentService _attachmentService;
        private ObservableCollection<TaskAttachment> _attachments;

        public AttachmentDisplayControl()
        {
            InitializeComponent();
            _attachments = new ObservableCollection<TaskAttachment>();
            DataContext = this;
            UpdatePlaceholderVisibility();
        }

        public AttachmentDisplayControl(IAttachmentService attachmentService) : this()
        {
            _attachmentService = attachmentService;
        }

        #region Properties

        /// <summary>
        /// Collection der anzuzeigenden Anhänge
        /// </summary>
        public ObservableCollection<TaskAttachment> Attachments
        {
            get => _attachments;
            set
            {
                if (SetProperty(ref _attachments, value))
                {
                    UpdatePlaceholderVisibility();
                }
            }
        }

        /// <summary>
        /// Gibt an, ob der Benutzer Anhänge löschen darf
        /// </summary>
        public bool CanDeleteAttachments { get; set; } = true;

        #endregion

        #region Events

        /// <summary>
        /// Event wird ausgelöst, wenn ein Anhang gelöscht wurde
        /// </summary>
        public event EventHandler<AttachmentDeletedEventArgs>? AttachmentDeleted;

        /// <summary>
        /// Event wird ausgelöst, wenn ein Fehler beim Löschen auftritt
        /// </summary>
        public event EventHandler<string>? DeleteError;

        /// <summary>
        /// Event wird ausgelöst, wenn ein Anhang zur Vollbildanzeige ausgewählt wurde
        /// </summary>
        public event EventHandler<TaskAttachment>? AttachmentViewRequested;

        #endregion

        #region Public Methods

        /// <summary>
        /// Fügt einen neuen Anhang zur Anzeige hinzu
        /// </summary>
        public void AddAttachment(TaskAttachment attachment)
        {
            if (attachment != null && !Attachments.Contains(attachment))
            {
                Attachments.Add(attachment);
                UpdatePlaceholderVisibility();
            }
        }

        /// <summary>
        /// Fügt mehrere Anhänge zur Anzeige hinzu
        /// </summary>
        public void AddAttachments(IEnumerable<TaskAttachment> attachments)
        {
            foreach (var attachment in attachments)
            {
                AddAttachment(attachment);
            }
        }

        /// <summary>
        /// Entfernt einen Anhang aus der Anzeige
        /// </summary>
        public void RemoveAttachment(TaskAttachment attachment)
        {
            if (attachment != null && Attachments.Contains(attachment))
            {
                Attachments.Remove(attachment);
                UpdatePlaceholderVisibility();
            }
        }

        /// <summary>
        /// Lädt Anhänge für eine bestimmte Task-ID
        /// </summary>
        public async Task LoadAttachmentsAsync(int taskId)
        {
            if (_attachmentService == null)
                return;

            try
            {
                var attachments = await _attachmentService.GetTaskAttachmentsAsync(taskId);
                Attachments.Clear();
                
                foreach (var attachment in attachments)
                {
                    Attachments.Add(attachment);
                }
                
                UpdatePlaceholderVisibility();
            }
            catch (Exception ex)
            {
                DeleteError?.Invoke(this, $"Fehler beim Laden der Anhänge: {ex.Message}");
            }
        }

        /// <summary>
        /// Löscht alle Anhänge aus der Anzeige
        /// </summary>
        public void ClearAttachments()
        {
            Attachments.Clear();
            UpdatePlaceholderVisibility();
        }

        #endregion

        #region Event Handlers

        private async void OnDeleteAttachment(object sender, RoutedEventArgs e)
        {
            if (!CanDeleteAttachments || _attachmentService == null)
                return;

            var button = sender as Button;
            var attachment = button?.Tag as TaskAttachment;
            
            if (attachment == null)
                return;

            // Bestätigungsdialog
            var result = MessageBox.Show(
                $"Möchten Sie den Anhang '{attachment.FileName}' wirklich löschen?",
                "Anhang löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // Anhang aus dem Service löschen
                await _attachmentService.DeleteAttachmentAsync(attachment.Id);
                
                // Anhang aus der UI entfernen
                RemoveAttachment(attachment);
                
                // Event auslösen
                AttachmentDeleted?.Invoke(this, new AttachmentDeletedEventArgs(attachment));
            }
            catch (Exception ex)
            {
                DeleteError?.Invoke(this, $"Fehler beim Löschen des Anhangs: {ex.Message}");
            }
        }

        private void OnViewAttachment(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var attachment = button?.Tag as TaskAttachment;
            
            if (attachment != null)
            {
                AttachmentViewRequested?.Invoke(this, attachment);
            }
        }

        private void OnImageClick(object sender, MouseButtonEventArgs e)
        {
            var image = sender as Image;
            var attachment = image?.Tag as TaskAttachment;
            
            if (attachment != null)
            {
                AttachmentViewRequested?.Invoke(this, attachment);
            }
        }

        #endregion

        #region Helper Methods

        private void UpdatePlaceholderVisibility()
        {
            NoAttachmentsPlaceholder.Visibility = 
                Attachments?.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Öffnet eine Datei mit dem Standard-Programm
        /// </summary>
        public static void OpenFileWithDefaultProgram(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    };
                    Process.Start(processStartInfo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Öffnen der Datei: {ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    /// <summary>
    /// Event-Argumente für gelöschte Anhänge
    /// </summary>
    public class AttachmentDeletedEventArgs : EventArgs
    {
        public TaskAttachment DeletedAttachment { get; }

        public AttachmentDeletedEventArgs(TaskAttachment deletedAttachment)
        {
            DeletedAttachment = deletedAttachment;
        }
    }
}