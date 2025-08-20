using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Admin_Tasks.Models;

namespace Admin_Tasks.Converters
{
    /// <summary>
    /// Converter der die Sichtbarkeit des Accept-Buttons basierend auf Task-Status und Benutzer bestimmt
    /// </summary>
    public class TaskAcceptVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values == null || values.Length != 2 || values[0] == null || values[1] == null)
                    return Visibility.Collapsed;
                    
                if (values[0] is not TaskItem task || values[1] is not User currentUser)
                    return Visibility.Collapsed;

                // Button ist nur sichtbar wenn:
                // 1. Task ist dem aktuellen User zugewiesen
                // 2. Task ist noch nicht InProgress oder Completed
                // 3. Task hat einen Status von Open (zugewiesene Tasks haben Status Open)
                bool isAssignedToCurrentUser = task.AssignedToUserId == currentUser.Id;
                bool canBeAccepted = task.Status == Admin_Tasks.Models.TaskStatus.Open;
                bool isNotInProgress = task.Status != Admin_Tasks.Models.TaskStatus.InProgress && task.Status != Admin_Tasks.Models.TaskStatus.Completed;
                
                return (isAssignedToCurrentUser && canBeAccepted && isNotInProgress) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}