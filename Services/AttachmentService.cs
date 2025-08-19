using Admin_Tasks.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Admin_Tasks.Services
{
    /// <summary>
    /// Service für die Verwaltung von Dateianhängen
    /// </summary>
    public class AttachmentService : IAttachmentService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string _uploadsPath;
        private readonly long _maxFileSize = 10 * 1024 * 1024; // 10 MB
        private readonly string[] _allowedImageTypes = { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp", "image/webp" };
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

        public AttachmentService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _uploadsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads", "Attachments");
            
            // Stelle sicher, dass der Upload-Ordner existiert
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
            }
        }

        public async Task<TaskAttachment> SaveAttachmentAsync(Stream fileStream, string fileName, string contentType, int taskId, int uploadedBy)
        {
            if (!IsValidAttachment(fileName, contentType, fileStream.Length))
            {
                throw new ArgumentException("Ungültige Datei. Nur Bilddateien bis 10 MB sind erlaubt.");
            }

            // Erstelle einen eindeutigen Dateinamen
            var fileExtension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(_uploadsPath, uniqueFileName);

            // Speichere die Datei
            using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamOutput);
            }

            // Erstelle TaskAttachment-Eintrag mit eigenem DbContext-Scope
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
            
            var attachment = new TaskAttachment
            {
                FileName = fileName,
                FilePath = filePath,
                FileSize = (int)fileStream.Length,
                ContentType = contentType,
                TaskId = taskId,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow
            };

            context.TaskAttachments.Add(attachment);
            await context.SaveChangesAsync();

            return attachment;
        }

        public async Task<TaskAttachment> SaveAttachmentAsync(byte[] fileData, string fileName, string contentType, int taskId, int uploadedBy)
        {
            using (var memoryStream = new MemoryStream(fileData))
            {
                return await SaveAttachmentAsync(memoryStream, fileName, contentType, taskId, uploadedBy);
            }
        }

        public async Task<Stream> GetAttachmentStreamAsync(TaskAttachment attachment)
        {
            if (!File.Exists(attachment.FilePath))
            {
                throw new FileNotFoundException($"Datei nicht gefunden: {attachment.FileName}");
            }

            return new FileStream(attachment.FilePath, FileMode.Open, FileAccess.Read);
        }

        public async Task<byte[]> GetAttachmentDataAsync(TaskAttachment attachment)
        {
            if (!File.Exists(attachment.FilePath))
            {
                throw new FileNotFoundException($"Datei nicht gefunden: {attachment.FileName}");
            }

            return await File.ReadAllBytesAsync(attachment.FilePath);
        }

        public async Task<bool> DeleteAttachmentAsync(int attachmentId)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
            
            var attachment = await context.TaskAttachments.FindAsync(attachmentId);
            if (attachment == null)
            {
                return false;
            }

            try
            {
                // Lösche die physische Datei
                if (File.Exists(attachment.FilePath))
                {
                    File.Delete(attachment.FilePath);
                }

                // Lösche den Datenbankeintrag
                context.TaskAttachments.Remove(attachment);
                await context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<TaskAttachment>> GetAttachmentsForTaskAsync(int taskId)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
            
            return await context.TaskAttachments
                .Where(a => a.TaskId == taskId)
                .Include(a => a.User)
                .OrderBy(a => a.UploadedAt)
                .ToListAsync();
        }

        public bool IsValidAttachment(string fileName, string contentType, long fileSize)
        {
            // Prüfe Dateigröße
            if (fileSize > _maxFileSize)
            {
                return false;
            }

            // Prüfe Content-Type
            if (!_allowedImageTypes.Contains(contentType.ToLowerInvariant()))
            {
                return false;
            }

            // Prüfe Dateiendung
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return false;
            }

            return true;
        }

        public async Task<byte[]?> CreateThumbnailAsync(TaskAttachment attachment, int maxWidth = 200, int maxHeight = 200)
        {
            if (!attachment.IsImage)
            {
                return null;
            }

            try
            {
                Image originalImage;
                if (attachment.FileData != null && attachment.FileData.Length > 0)
                {
                    using (var ms = new MemoryStream(attachment.FileData))
                    {
                        originalImage = Image.FromStream(ms);
                    }
                }
                else if (File.Exists(attachment.FilePath))
                {
                    originalImage = Image.FromFile(attachment.FilePath);
                }
                else
                {
                    return null;
                }

                using (originalImage)
                {
                    // Berechne neue Dimensionen unter Beibehaltung des Seitenverhältnisses
                    var ratioX = (double)maxWidth / originalImage.Width;
                    var ratioY = (double)maxHeight / originalImage.Height;
                    var ratio = Math.Min(ratioX, ratioY);

                    var newWidth = (int)(originalImage.Width * ratio);
                    var newHeight = (int)(originalImage.Height * ratio);

                    using (var thumbnail = new Bitmap(newWidth, newHeight))
                    {
                        using (var graphics = Graphics.FromImage(thumbnail))
                        {
                            graphics.CompositingQuality = CompositingQuality.HighQuality;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.SmoothingMode = SmoothingMode.HighQuality;

                            graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);
                        }

                        // Konvertiere zu Byte-Array
                        using (var memoryStream = new MemoryStream())
                        {
                            thumbnail.Save(memoryStream, ImageFormat.Jpeg);
                            return memoryStream.ToArray();
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Ruft alle Attachments für eine bestimmte Aufgabe ab (Alias für GetAttachmentsForTaskAsync)
        /// </summary>
        /// <param name="taskId">ID der Aufgabe</param>
        /// <returns>Liste der TaskAttachments</returns>
        public async Task<List<TaskAttachment>> GetTaskAttachmentsAsync(int taskId)
        {
            return await GetAttachmentsForTaskAsync(taskId);
        }

        public async Task<TaskAttachment> SaveAttachmentAsync(TaskAttachment attachment)
        {
            if (attachment.FileData == null || attachment.FileData.Length == 0)
            {
                throw new ArgumentException("Die Dateidaten dürfen nicht leer sein.", nameof(attachment));
            }

            if (!IsValidAttachment(attachment.FileName, attachment.ContentType, attachment.FileData.Length))
            {
                throw new ArgumentException("Ungültige Datei. Nur Bilddateien bis 10 MB sind erlaubt.");
            }

            var fileExtension = Path.GetExtension(attachment.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(_uploadsPath, uniqueFileName);

            await File.WriteAllBytesAsync(filePath, attachment.FileData);

            attachment.FilePath = filePath;
            attachment.FileData = null; // Clear byte array after saving to file

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AdminTasksDbContext>();
            
            context.TaskAttachments.Add(attachment);
            await context.SaveChangesAsync();

            return attachment;
        }

        /// <summary>
        /// Erstellt einen sicheren Hash für Dateinamen
        /// </summary>
        private string CreateFileHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(hashBytes).Replace("/", "_").Replace("+", "-").Substring(0, 8);
            }
        }

        /// <summary>
        /// Bereinigt Dateinamen von ungültigen Zeichen
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "attachment" : sanitized;
        }
    }
}