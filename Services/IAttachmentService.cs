using Admin_Tasks.Models;
using System.IO;

namespace Admin_Tasks.Services
{
    /// <summary>
    /// Interface für den Attachment-Service zur Verwaltung von Dateianhängen
    /// </summary>
    public interface IAttachmentService
    {
        /// <summary>
        /// Speichert eine Datei und erstellt einen TaskAttachment-Eintrag
        /// </summary>
        /// <param name="fileStream">Stream der zu speichernden Datei</param>
        /// <param name="fileName">Ursprünglicher Dateiname</param>
        /// <param name="contentType">MIME-Type der Datei</param>
        /// <param name="taskId">ID der zugehörigen Aufgabe</param>
        /// <param name="uploadedBy">ID des Benutzers, der die Datei hochlädt</param>
        /// <returns>Der erstellte TaskAttachment</returns>
        Task<TaskAttachment> SaveAttachmentAsync(Stream fileStream, string fileName, string contentType, int taskId, int uploadedBy);

        /// <summary>
        /// Speichert eine Datei aus einem Byte-Array
        /// </summary>
        /// <param name="fileData">Datei-Daten als Byte-Array</param>
        /// <param name="fileName">Ursprünglicher Dateiname</param>
        /// <param name="contentType">MIME-Type der Datei</param>
        /// <param name="taskId">ID der zugehörigen Aufgabe</param>
        /// <param name="uploadedBy">ID des Benutzers, der die Datei hochlädt</param>
        /// <returns>Der erstellte TaskAttachment</returns>
        Task<TaskAttachment> SaveAttachmentAsync(byte[] fileData, string fileName, string contentType, int taskId, int uploadedBy);

        /// <summary>
        /// Lädt eine Datei als Stream
        /// </summary>
        /// <param name="attachment">TaskAttachment mit Dateipfad-Informationen</param>
        /// <returns>FileStream der Datei</returns>
        Task<Stream> GetAttachmentStreamAsync(TaskAttachment attachment);

        /// <summary>
        /// Lädt eine Datei als Byte-Array
        /// </summary>
        /// <param name="attachment">TaskAttachment mit Dateipfad-Informationen</param>
        /// <returns>Datei-Daten als Byte-Array</returns>
        Task<byte[]> GetAttachmentDataAsync(TaskAttachment attachment);

        /// <summary>
        /// Löscht eine Datei und den zugehörigen TaskAttachment-Eintrag
        /// </summary>
        /// <param name="attachmentId">ID des zu löschenden Attachments</param>
        /// <returns>True, wenn erfolgreich gelöscht</returns>
        Task<bool> DeleteAttachmentAsync(int attachmentId);

        /// <summary>
        /// Ruft alle Attachments für eine bestimmte Aufgabe ab
        /// </summary>
        /// <param name="taskId">ID der Aufgabe</param>
        /// <returns>Liste der TaskAttachments</returns>
        Task<List<TaskAttachment>> GetAttachmentsForTaskAsync(int taskId);

        /// <summary>
        /// Ruft alle Attachments für eine bestimmte Aufgabe ab (Alias für GetAttachmentsForTaskAsync)
        /// </summary>
        /// <param name="taskId">ID der Aufgabe</param>
        /// <returns>Liste der TaskAttachments</returns>
        Task<List<TaskAttachment>> GetTaskAttachmentsAsync(int taskId);

        /// <summary>
        /// Validiert, ob eine Datei als Anhang erlaubt ist
        /// </summary>
        /// <param name="fileName">Dateiname</param>
        /// <param name="contentType">MIME-Type</param>
        /// <param name="fileSize">Dateigröße in Bytes</param>
        /// <returns>True, wenn die Datei erlaubt ist</returns>
        bool IsValidAttachment(string fileName, string contentType, long fileSize);

        /// <summary>
        /// Erstellt ein Thumbnail für Bilddateien
        /// </summary>
        /// <param name="attachment">TaskAttachment der Bilddatei</param>
        /// <param name="maxWidth">Maximale Breite des Thumbnails</param>
        /// <param name="maxHeight">Maximale Höhe des Thumbnails</param>
        /// <returns>Thumbnail als Byte-Array</returns>
        Task<byte[]?> CreateThumbnailAsync(TaskAttachment attachment, int maxWidth = 200, int maxHeight = 200);

        /// <summary>
        /// Speichert ein TaskAttachment-Objekt, das bereits In-Memory-Daten enthält
        /// </summary>
        /// <param name="attachment">Das zu speichernde TaskAttachment-Objekt</param>
        /// <returns>Das gespeicherte TaskAttachment-Objekt mit aktualisierter ID</returns>
        Task<TaskAttachment> SaveAttachmentAsync(TaskAttachment attachment);
    }
}