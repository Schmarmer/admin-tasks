using System;
using System.ComponentModel.DataAnnotations;

namespace Admin_Tasks.Models
{
    /// <summary>
    /// Zusammenfassung eines Task-Chats für die Chat-Übersicht
    /// </summary>
    public class TaskChatSummary
    {
        /// <summary>
        /// ID der Aufgabe
        /// </summary>
        public int TaskId { get; set; }
        
        /// <summary>
        /// Titel der Aufgabe
        /// </summary>
        [Required]
        public string TaskTitle { get; set; } = string.Empty;
        
        /// <summary>
        /// Beschreibung der Aufgabe (gekürzt)
        /// </summary>
        public string TaskDescription { get; set; } = string.Empty;
        
        /// <summary>
        /// Status der Aufgabe
        /// </summary>
        public TaskStatus TaskStatus { get; set; }
        
        /// <summary>
        /// Priorität der Aufgabe
        /// </summary>
        public TaskPriority TaskPriority { get; set; }
        
        /// <summary>
        /// Zugewiesener Benutzer
        /// </summary>
        public User? AssignedUser { get; set; }
        
        /// <summary>
        /// ID des zugewiesenen Benutzers
        /// </summary>
        public int? AssignedUserId { get; set; }
        
        /// <summary>
        /// Inhalt der letzten Nachricht
        /// </summary>
        public string? LastMessageContent { get; set; }
        
        /// <summary>
        /// Zeitpunkt der letzten Nachricht
        /// </summary>
        public DateTime? LastMessageTime { get; set; }
        
        /// <summary>
        /// Autor der letzten Nachricht
        /// </summary>
        public User? LastMessageAuthor { get; set; }
        
        /// <summary>
        /// ID des Autors der letzten Nachricht
        /// </summary>
        public int? LastMessageAuthorId { get; set; }
        
        /// <summary>
        /// Anzahl ungelesener Nachrichten für den aktuellen Benutzer
        /// </summary>
        public int UnreadCount { get; set; }
        
        /// <summary>
        /// Gesamtanzahl der Nachrichten in diesem Chat
        /// </summary>
        public int TotalMessageCount { get; set; }
        
        /// <summary>
        /// Ob der aktuelle Benutzer die letzte Nachricht geschrieben hat
        /// </summary>
        public bool IsLastMessageFromCurrentUser { get; set; }
        
        /// <summary>
        /// Zeitpunkt der letzten Aktivität (letzte Nachricht oder Task-Update)
        /// </summary>
        public DateTime LastActivity { get; set; }
        
        /// <summary>
        /// Ob dieser Chat als Favorit markiert ist
        /// </summary>
        public bool IsFavorite { get; set; }
        
        /// <summary>
        /// Ob dieser Chat stummgeschaltet ist
        /// </summary>
        public bool IsMuted { get; set; }
        
        /// <summary>
        /// Gleichheitsprüfung basierend auf TaskId
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is TaskChatSummary other)
            {
                return TaskId == other.TaskId;
            }
            return false;
        }
        
        /// <summary>
        /// Hash-Code basierend auf TaskId
        /// </summary>
        public override int GetHashCode()
        {
            return TaskId.GetHashCode();
        }
    }
}