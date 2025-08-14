using Admin_Tasks.Models;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Admin_Tasks.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        public static readonly StatusToBrushConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.TaskStatus status)
        {
            return status switch
            {
                Models.TaskStatus.Open => new SolidColorBrush(Color.FromRgb(108, 117, 125)), // Secondary
                Models.TaskStatus.InProgress => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // Warning
                Models.TaskStatus.Completed => new SolidColorBrush(Color.FromRgb(25, 135, 84)), // Success
                    _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
                };
            }
            
            return new SolidColorBrush(Color.FromRgb(108, 117, 125));
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}