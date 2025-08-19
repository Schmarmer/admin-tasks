using System.Globalization;
using System.Windows.Data;
using Admin_Tasks.Models;

namespace Admin_Tasks.Converters;

/// <summary>
/// Converter für Notification-Type zu Icon
/// </summary>
public class NotificationTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is NotificationType type)
        {
            return type switch
            {
                NotificationType.TaskAssigned => "📋",
                NotificationType.TaskCompleted => "✅",
                NotificationType.TaskForwarded => "↗️",
                NotificationType.TaskStatusChanged => "🔄",
                NotificationType.TaskCommentAdded => "💬",
                NotificationType.TaskDueDateApproaching => "⏰",
                NotificationType.TaskOverdue => "⚠️",
                NotificationType.General => "ℹ️",
                _ => "🔔"
            };
        }
        return "🔔";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter für Read-Status zu Icon
/// </summary>
public class ReadStatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isRead)
        {
            return isRead ? "👁️" : "👁️‍🗨️";
        }
        return "👁️‍🗨️";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter für Read-Status zu Tooltip
/// </summary>
public class ReadStatusToTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isRead)
        {
            return isRead ? "Als ungelesen markieren" : "Als gelesen markieren";
        }
        return "Als gelesen markieren";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}