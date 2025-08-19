using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Admin_Tasks.Models
{
    /// <summary>
    /// Repräsentiert einen Dateianhang zu einer Aufgabe
    /// </summary>
    [Table("TaskAttachments")]
    public class TaskAttachment
    {
        /// <summary>
        /// Eindeutige ID des Anhangs
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Ursprünglicher Dateiname
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Pfad zur gespeicherten Datei auf dem Server
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Dateigröße in Bytes
        /// </summary>
        [Required]
        public int FileSize { get; set; }

        /// <summary>
        /// MIME-Type der Datei (z.B. image/jpeg, image/png)
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Zeitpunkt des Uploads
        /// </summary>
        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ID der zugehörigen Aufgabe
        /// </summary>
        [Required]
        public int TaskId { get; set; }

        /// <summary>
        /// ID des Benutzers, der die Datei hochgeladen hat
        /// </summary>
        [Required]
        public int UploadedBy { get; set; }

        // Navigation Properties
        /// <summary>
        /// Zugehörige Aufgabe
        /// </summary>
        [ForeignKey(nameof(TaskId))]
        public virtual TaskItem? Task { get; set; }

        /// <summary>
        /// Benutzer, der die Datei hochgeladen hat
        /// </summary>
        [ForeignKey(nameof(UploadedBy))]
        public virtual User? User { get; set; }

        [NotMapped]
        public Guid? TemporaryId { get; set; }

        [NotMapped]
        public bool IsTemporary => TaskId == 0 && TemporaryId.HasValue;

        [NotMapped]
        public byte[]? FileData { get; set; }

        /// <summary>
        /// Prüft, ob es sich um eine Bilddatei handelt
        /// </summary>
        public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Pfad zum Thumbnail der Datei (für Bildvorschau)
        /// </summary>
        [MaxLength(500)]
        public string? ThumbnailPath { get; set; }

        /// <summary>
        /// Gibt die Dateigröße in einem lesbaren Format zurück
        /// </summary>
        public string FormattedFileSize
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                if (FileSize < 1024 * 1024)
                    return $"{FileSize / 1024.0:F1} KB";
                if (FileSize < 1024 * 1024 * 1024)
                    return $"{FileSize / (1024.0 * 1024.0):F1} MB";
                return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F1} GB";
            }
        }

        /// <summary>
        /// Gibt den Pfad für die Thumbnail-Anzeige zurück (für temporäre Attachments wird FileData als Base64 verwendet)
        /// </summary>
        [NotMapped]
        public string? DisplayThumbnailPath
        {
            get
            {
                if (IsTemporary && FileData != null && IsImage)
                {
                    // Für temporäre Attachments: Base64-String für direkte Anzeige
                    var base64String = Convert.ToBase64String(FileData);
                    return $"data:{ContentType};base64,{base64String}";
                }
                return ThumbnailPath ?? FilePath;
            }
        }
    }
}