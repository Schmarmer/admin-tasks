using System;
using System.Globalization;
using System.Windows.Data;
using Admin_Tasks.Models;

namespace Admin_Tasks.Converters
{
    // Combines a TaskItem and a User into a ValueTuple for AssignTaskCommand
    public class TaskUserPairConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return null!;

            if (values[0] is TaskItem task && values[1] is User user)
            {
                return (task, user);
            }
            return null!;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
