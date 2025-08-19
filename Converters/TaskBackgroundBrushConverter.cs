using Admin_Tasks.Models;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Admin_Tasks.Converters
{
    /// <summary>
    /// Converter für Hintergrundfarben von Task-Karten basierend auf Besitzerstatus
    /// </summary>
    public class TaskBackgroundBrushConverter : IValueConverter
    {
        public static readonly TaskBackgroundBrushConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskItem task)
            {
                // Besitzlose Tasks erhalten einen leicht rötlichen Hintergrund
                if (task.AssignedToUserId == null)
                {
                    // Sehr subtiler roter Hintergrund für besitzlose Tasks
                    return new SolidColorBrush(Color.FromArgb(25, 220, 53, 69)); // 10% Opacity Red
                }
                
                // Abgeschlossene Tasks erhalten einen leicht grünlichen Hintergrund
                if (task.Status == Models.TaskStatus.Completed)
                {
                    return new SolidColorBrush(Color.FromArgb(20, 25, 135, 84)); // 8% Opacity Green
                }
            }
            
            // Transparent für normale Tasks (verwendet Standard-Hintergrund)
            return Brushes.Transparent;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}