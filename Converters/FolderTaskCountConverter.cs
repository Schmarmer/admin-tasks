using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Admin_Tasks.ViewModels;

namespace Admin_Tasks.Converters;

public class FolderTaskCountConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2)
            return " (0)";
            
        if (values[0] is not string folderName || values[1] is not MainViewModel viewModel)
            return " (0)";

        // Normalize folder name: remove any existing count suffix like " (12)" and trim whitespace
        // This makes the converter robust if the header already contains a count or additional formatting.
        var normalizedName = folderName;
        var idx = normalizedName.IndexOf(" (", StringComparison.Ordinal);
        if (idx >= 0)
        {
            normalizedName = normalizedName.Substring(0, idx);
        }
        normalizedName = normalizedName.Trim();

        int count = normalizedName switch
        {
            "Alle Aufgaben" => viewModel.AllTasksCount,
            "Erstellte Aufgaben" => viewModel.CreatedTasksCount,
            "Zugewiesene Aufgaben" => viewModel.AssignedTasksCount,
            "Ohne Besitzer" => viewModel.UnassignedTasksCount,
            "Abgeschlossene Aufgaben" => viewModel.CompletedTasksCount,
            "Offene Aufgaben" => viewModel.OpenTasksCount,
            _ => 0
        };

        return $" ({count})";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}