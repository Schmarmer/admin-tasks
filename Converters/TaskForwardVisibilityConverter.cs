using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Admin_Tasks.Models;

namespace Admin_Tasks.Converters
{
    public class TaskForwardVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || values[0] is not TaskItem task || values[1] is not User currentUser)
                return Visibility.Collapsed;

            // Forward button is visible if:
            // 1. Task is assigned to current user OR created by current user OR current user is admin/manager
            // 2. Task is not completed or cancelled
            // 3. Task status is Open or InProgress
            bool canBeForwarded = (task.AssignedToUserId == currentUser.Id || 
                                   task.CreatedByUserId == currentUser.Id ||
                                   currentUser.Role == "Admin" || 
                                   currentUser.Role == "Manager") &&
                                  task.Status != Admin_Tasks.Models.TaskStatus.Completed &&
                                  task.Status != Admin_Tasks.Models.TaskStatus.Cancelled &&
                                  (task.Status == Admin_Tasks.Models.TaskStatus.Open || 
                                   task.Status == Admin_Tasks.Models.TaskStatus.InProgress);

            return canBeForwarded ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}