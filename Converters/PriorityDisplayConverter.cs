using System;
using System.Globalization;
using System.Windows.Data;
using Admin_Tasks.Models;

namespace Admin_Tasks.Converters;

public class PriorityDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return "Alle Prioritäten";
            
        if (value is TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Unspecified => "Nicht spezifiziert",
                TaskPriority.Low => "Niedrig",
                TaskPriority.Medium => "Mittel",
                TaskPriority.High => "Hoch",
                TaskPriority.Critical => "Kritisch",
                _ => priority.ToString()
            };
        }
        
        return value?.ToString() ?? "Alle Prioritäten";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}