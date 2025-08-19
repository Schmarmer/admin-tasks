using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Admin_Tasks.Converters;

/// <summary>
/// Konvertiert einen Hex-Farbstring in einen SolidColorBrush
/// </summary>
public class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string colorString && !string.IsNullOrEmpty(colorString))
        {
            try
            {
                // Entferne # falls vorhanden
                if (colorString.StartsWith("#"))
                    colorString = colorString.Substring(1);

                // Konvertiere Hex zu Color
                var color = (Color)ColorConverter.ConvertFromString("#" + colorString);
                return new SolidColorBrush(color);
            }
            catch
            {
                // Fallback zu einer Standard-Farbe
                return new SolidColorBrush(Colors.Gray);
            }
        }

        // Fallback zu einer Standard-Farbe
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            return brush.Color.ToString();
        }

        return "#808080"; // Grau als Fallback
    }
}