using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Admin_Tasks.Models;
using Admin_Tasks.ViewModels;

namespace Admin_Tasks.Converters;

/// <summary>
/// Converter der die Custom Folders für eine Task zurückgibt
/// </summary>
public class TaskToFoldersConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length != 2 || values[0] is not TaskItem task)
            return new List<CustomFolder>();
            
        if (values[1] is not IEnumerable<CustomFolder> customFolders || customFolders == null)
            return new List<CustomFolder>();

        try
        {
            return customFolders.Where(folder => folder != null && folder.ContainsTask(task.Id)).ToList();
        }
        catch
        {
            return new List<CustomFolder>();
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter der prüft ob eine Task zu mindestens einem Custom Folder gehört
/// </summary>
public class TaskHasFoldersConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length != 2 || values[0] is not TaskItem task)
            return false;
            
        if (values[1] is not IEnumerable<CustomFolder> customFolders || customFolders == null)
            return false;

        try
        {
            return customFolders.Any(folder => folder != null && folder.ContainsTask(task.Id));
        }
        catch
        {
            return false;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter der die Anzahl der Custom Folders für eine Task zurückgibt
/// </summary>
public class TaskFolderCountConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length != 2 || values[0] is not TaskItem task)
            return 0;
            
        if (values[1] is not IEnumerable<CustomFolder> customFolders || customFolders == null)
            return 0;

        try
        {
            return customFolders.Count(folder => folder != null && folder.ContainsTask(task.Id));
        }
        catch
        {
            return 0;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter der die Farben der Custom Folders für eine Task als String zurückgibt
/// </summary>
public class TaskFolderColorsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (values == null || values.Length != 2 || values[0] == null || values[1] == null)
                return string.Empty;
                
            if (values[0] is not TaskItem task || values[1] is not IEnumerable<CustomFolder> customFolders)
                return string.Empty;

            var folders = customFolders.Where(folder => folder != null && folder.ContainsTask(task.Id)).ToList();
            if (!folders.Any())
                return string.Empty;

            // Erste Folder-Farbe zurückgeben für einfache Darstellung
            return folders.First().Color ?? string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter der die Namen der Custom Folders für eine Task als String zurückgibt
/// </summary>
public class TaskFolderNamesConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (values == null || values.Length != 2 || values[0] == null || values[1] == null)
                return string.Empty;
                
            if (values[0] is not TaskItem task || values[1] is not IEnumerable<CustomFolder> customFolders)
                return string.Empty;

            var folders = customFolders.Where(folder => folder != null && folder.ContainsTask(task.Id)).ToList();
            if (!folders.Any())
                return string.Empty;

            return string.Join(", ", folders.Select(f => f.Name ?? string.Empty));
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter der die Sichtbarkeit basierend auf Custom Folder-Zuordnung bestimmt
/// </summary>
public class TaskFolderVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (values == null || values.Length != 2 || values[0] == null || values[1] == null)
                return System.Windows.Visibility.Collapsed;
                
            if (values[0] is not TaskItem task || values[1] is not IEnumerable<CustomFolder> customFolders)
                return System.Windows.Visibility.Collapsed;

            bool hasFolders = customFolders.Any(folder => folder != null && folder.ContainsTask(task.Id));
            return hasFolders ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
        catch (Exception)
        {
            return System.Windows.Visibility.Collapsed;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}