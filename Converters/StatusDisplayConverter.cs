using System;
using System.Globalization;
using System.Windows.Data;
using Admin_Tasks.Models;

namespace Admin_Tasks.Converters;

public class StatusDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return "Alle Status";
            
        if (value is Models.TaskStatus status)
        {
            return status switch
            {
                Models.TaskStatus.Open => "Offen",
                Models.TaskStatus.InProgress => "In Bearbeitung",
                Models.TaskStatus.Completed => "Abgeschlossen",
                Models.TaskStatus.Cancelled => "Abgebrochen",
                Models.TaskStatus.OnHold => "Pausiert",
                _ => status.ToString()
            };
        }
        
        return value?.ToString() ?? "Alle Status";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}