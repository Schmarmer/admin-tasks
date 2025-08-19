using Admin_Tasks.Models;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Admin_Tasks.Converters
{
    /// <summary>
    /// Converter für Akzentfarben von Tasks basierend auf Status und Besitzer
    /// </summary>
    public class TaskAccentBrushConverter : IValueConverter
    {
        public static readonly TaskAccentBrushConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskItem task)
            {
                // Besitzlose Tasks (höchste Priorität für Hervorhebung)
                if (task.AssignedToUserId == null)
                {
                    return new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Danger Red - Besitzlos
                }
                
                // Tasks basierend auf Status
                return task.Status switch
                {
                    Models.TaskStatus.Open => new SolidColorBrush(Color.FromRgb(13, 110, 253)), // Primary Blue - Offen
                    Models.TaskStatus.InProgress => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // Warning Yellow - In Bearbeitung
                    Models.TaskStatus.Completed => new SolidColorBrush(Color.FromRgb(25, 135, 84)), // Success Green - Abgeschlossen
                    Models.TaskStatus.OnHold => new SolidColorBrush(Color.FromRgb(108, 117, 125)), // Secondary Gray - Pausiert
                    Models.TaskStatus.Cancelled => new SolidColorBrush(Color.FromRgb(220, 53, 69)), // Danger Red - Abgebrochen
                    _ => new SolidColorBrush(Color.FromRgb(108, 117, 125)) // Default Gray
                };
            }
            
            return new SolidColorBrush(Color.FromRgb(108, 117, 125)); // Default Gray
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}