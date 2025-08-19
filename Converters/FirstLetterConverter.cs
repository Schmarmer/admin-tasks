using System.Globalization;
using System.Windows.Data;

namespace Admin_Tasks.Converters
{
    /// <summary>
    /// Converter der den ersten Buchstaben eines Strings zurückgibt
    /// Wird für Avatar-Anzeigen verwendet
    /// </summary>
    public class FirstLetterConverter : IValueConverter
    {
        public static readonly FirstLetterConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && !string.IsNullOrWhiteSpace(text))
            {
                return text.Trim().Substring(0, 1).ToUpper();
            }
            
            return "?";
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}