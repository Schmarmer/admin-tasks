using Admin_Tasks.Models;
using Admin_Tasks.Services;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Admin_Tasks.Views
{
    /// <summary>
    /// UserControl für das Hochladen von Bildern mit Drag-and-Drop, Clipboard und File-Dialog Support
    /// </summary>
    public partial class ImageUploadControl : UserControl, INotifyPropertyChanged
    {
        private readonly IAttachmentService _attachmentService;
        private bool _isDragOver;
        private bool _isUploading;

        public ImageUploadControl(IAttachmentService attachmentService)
        {
            InitializeComponent();
            DataContext = this;
            _attachmentService = attachmentService ?? throw new ArgumentNullException(nameof(attachmentService));

            // Keyboard-Handler für Paste-Funktionalität
            KeyDown += OnKeyDown;
            Focusable = true;
        }

        #region Properties

        /// <summary>
        /// Gibt an, ob gerade ein Drag-Over-Vorgang stattfindet
        /// </summary>
        public bool IsDragOver
        {
            get => _isDragOver;
            set => SetProperty(ref _isDragOver, value);
        }

        /// <summary>
        /// Gibt an, ob gerade ein Upload-Vorgang läuft
        /// </summary>
        public bool IsUploading
        {
            get => _isUploading;
            set
            {
                SetProperty(ref _isUploading, value);
                ProgressOverlay.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Task-ID für die Anhänge
        /// </summary>
        public int TaskId { get; set; }

        /// <summary>
        /// Benutzer-ID für die Anhänge
        /// </summary>
        public int UserId { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Event wird ausgelöst, wenn Bilder erfolgreich hochgeladen wurden
        /// </summary>
        public event EventHandler<ImagesUploadedEventArgs>? ImagesUploaded;

        /// <summary>
        /// Event wird ausgelöst, wenn ein Fehler beim Upload auftritt
        /// </summary>
        public event EventHandler<string>? UploadError;

        #endregion

        #region Drag and Drop Events

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (IsUploading) return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Any(f => IsImageFile(f)))
                {
                    e.Effects = DragDropEffects.Copy;
                    IsDragOver = true;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void OnDragLeave(object sender, DragEventArgs e)
        {
            IsDragOver = false;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (IsUploading)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                e.Effects = files.Any(f => IsImageFile(f)) ? DragDropEffects.Copy : DragDropEffects.None;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            IsDragOver = false;

            if (IsUploading || !e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var imageFiles = files.Where(f => IsImageFile(f)).ToArray();

            if (imageFiles.Length > 0)
            {
                await UploadFilesAsync(imageFiles);
            }
        }

        #endregion

        #region Click Events

        private async void OnUploadAreaClick(object sender, MouseButtonEventArgs e)
        {
            if (IsUploading) return;
            await SelectAndUploadFilesAsync();
        }

        private async void OnSelectFilesClick(object sender, RoutedEventArgs e)
        {
            if (IsUploading) return;
            await SelectAndUploadFilesAsync();
        }

        private async void OnPasteClick(object sender, RoutedEventArgs e)
        {
            if (IsUploading) return;
            await PasteFromClipboardAsync();
        }

        #endregion

        #region Keyboard Events

        private async void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (IsUploading) return;

            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                await PasteFromClipboardAsync();
                e.Handled = true;
            }
        }

        #endregion

        #region Upload Methods

        private async Task SelectAndUploadFilesAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Bilder auswählen",
                Filter = "Bilddateien|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|Alle Dateien|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                await UploadFilesAsync(openFileDialog.FileNames);
            }
        }

        private async Task PasteFromClipboardAsync()
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    var image = Clipboard.GetImage();
                    if (image != null)
                    {
                        await UploadImageFromClipboardAsync(image);
                    }
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();
                    var imageFiles = files.Cast<string>().Where(f => IsImageFile(f)).ToArray();
                    
                    if (imageFiles.Length > 0)
                    {
                        await UploadFilesAsync(imageFiles);
                    }
                    else
                    {
                        UploadError?.Invoke(this, "Keine Bilddateien in der Zwischenablage gefunden.");
                    }
                }
                else
                {
                    UploadError?.Invoke(this, "Keine Bilder in der Zwischenablage gefunden.");
                }
            }
            catch (Exception ex)
            {
                UploadError?.Invoke(this, $"Fehler beim Einfügen aus der Zwischenablage: {ex.Message}");
            }
        }

        private async Task UploadFilesAsync(string[] filePaths)
        {
            if (_attachmentService == null || UserId == 0)
            {
                UploadError?.Invoke(this, "Upload-Service ist nicht initialisiert oder die Benutzer-ID fehlt.");
                return;
            }

            IsUploading = true;
            var uploadedAttachments = new List<TaskAttachment>();

            try
            {
                foreach (var filePath in filePaths)
                {
                    if (!IsImageFile(filePath))
                        continue;

                    var fileInfo = new FileInfo(filePath);
                    var fileName = fileInfo.Name;
                    var contentType = GetContentType(fileName);

                    if (!_attachmentService.IsValidAttachment(fileName, contentType, fileInfo.Length))
                    {
                        UploadError?.Invoke(this, $"Datei '{fileName}' ist ungültig oder zu groß.");
                        continue;
                    }

                    if (TaskId == 0) // Create temporary attachment
                    {
                        var fileData = await File.ReadAllBytesAsync(filePath);
                        var tempAttachment = new TaskAttachment
                        {
                            TemporaryId = Guid.NewGuid(),
                            FileName = fileName,
                            ContentType = contentType,
                            FileSize = (int)fileInfo.Length,
                            FileData = fileData, // Store data in memory
                            UploadedAt = DateTime.UtcNow,
                            UploadedBy = UserId // Associate with current user
                        };
                        uploadedAttachments.Add(tempAttachment);
                    }
                    else // Save directly
                    {
                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            var attachment = await _attachmentService.SaveAttachmentAsync(
                                fileStream, fileName, contentType, TaskId, UserId);
                            uploadedAttachments.Add(attachment);
                        }
                    }
                }

                if (uploadedAttachments.Count > 0)
                {
                    ImagesUploaded?.Invoke(this, new ImagesUploadedEventArgs(uploadedAttachments));
                }
            }
            catch (Exception ex)
            {
                UploadError?.Invoke(this, $"Fehler beim Hochladen: {ex.Message}");
            }
            finally
            {
                IsUploading = false;
            }
        }

        private async Task UploadImageFromClipboardAsync(BitmapSource image)
        {
            if (_attachmentService == null || UserId == 0)
            {
                UploadError?.Invoke(this, "Upload-Service ist nicht initialisiert oder die Benutzer-ID fehlt.");
                return;
            }

            IsUploading = true;

            try
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                byte[] imageData;
                using (var memoryStream = new MemoryStream())
                {
                    encoder.Save(memoryStream);
                    imageData = memoryStream.ToArray();
                }

                var fileName = $"clipboard_image_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var contentType = "image/png";

                if (!_attachmentService.IsValidAttachment(fileName, contentType, imageData.Length))
                {
                    UploadError?.Invoke(this, "Das Bild aus der Zwischenablage ist zu groß.");
                    IsUploading = false;
                    return;
                }

                var attachment = new TaskAttachment
                {
                    FileName = fileName,
                    ContentType = contentType,
                    FileSize = imageData.Length,
                    UploadedAt = DateTime.UtcNow,
                    UploadedBy = UserId
                };

                if (TaskId == 0) // Create temporary attachment
                {
                    attachment.TemporaryId = Guid.NewGuid();
                    attachment.FileData = imageData; // Store data in memory
                    ImagesUploaded?.Invoke(this, new ImagesUploadedEventArgs(new[] { attachment }));
                }
                else // Save directly
                {
                    attachment.TaskId = TaskId;
                    var savedAttachment = await _attachmentService.SaveAttachmentAsync(
                        new MemoryStream(imageData), fileName, contentType, TaskId, UserId);
                    ImagesUploaded?.Invoke(this, new ImagesUploadedEventArgs(new[] { savedAttachment }));
                }
            }
            catch (Exception ex)
            {
                UploadError?.Invoke(this, $"Fehler beim Hochladen des Bildes aus der Zwischenablage: {ex.Message}");
            }
            finally
            {
                IsUploading = false;
            }
        }

        #endregion

        #region Helper Methods

        private bool IsImageFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" }.Contains(extension);
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
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
    /// Event-Argumente für erfolgreich hochgeladene Bilder
    /// </summary>
    public class ImagesUploadedEventArgs : EventArgs
    {
        public IReadOnlyList<TaskAttachment> Attachments { get; }

        public ImagesUploadedEventArgs(IEnumerable<TaskAttachment> attachments)
        {
            Attachments = attachments.ToList().AsReadOnly();
        }
    }
}