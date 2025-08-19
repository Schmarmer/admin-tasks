using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Admin_Tasks.Converters
{
    /// <summary>
    /// Converter der einen Integer-Wert in Visibility umwandelt
    /// Zeigt das Element nur an, wenn der Wert größer als 0 ist
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public static readonly CountToVisibilityConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            
            if (value is long longCount)
            {
                return longCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}