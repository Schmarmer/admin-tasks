using System.Globalization;
using System.Windows.Data;

namespace Admin_Tasks.Converters
{
    /// <summary>
    /// Converter der DateTime-Werte in relative Zeitangaben umwandelt
    /// z.B. "vor 5 Minuten", "vor 2 Stunden", "gestern"
    /// </summary>
    public class RelativeTimeConverter : IValueConverter
    {
        public static readonly RelativeTimeConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var now = DateTime.Now;
                var timeSpan = now - dateTime;
                
                if (timeSpan.TotalMinutes < 1)
                {
                    return "gerade eben";
                }
                else if (timeSpan.TotalMinutes < 60)
                {
                    var minutes = (int)timeSpan.TotalMinutes;
                    return $"vor {minutes} Min.";
                }
                else if (timeSpan.TotalHours < 24)
                {
                    var hours = (int)timeSpan.TotalHours;
                    return $"vor {hours} Std.";
                }
                else if (timeSpan.TotalDays < 7)
                {
                    var days = (int)timeSpan.TotalDays;
                    if (days == 1)
                        return "gestern";
                    else
                        return $"vor {days} Tagen";
                }
                else if (timeSpan.TotalDays < 30)
                {
                    var weeks = (int)(timeSpan.TotalDays / 7);
                    if (weeks == 1)
                        return "vor 1 Woche";
                    else
                        return $"vor {weeks} Wochen";
                }
                else if (timeSpan.TotalDays < 365)
                {
                    var months = (int)(timeSpan.TotalDays / 30);
                    if (months == 1)
                        return "vor 1 Monat";
                    else
                        return $"vor {months} Monaten";
                }
                else
                {
                    var years = (int)(timeSpan.TotalDays / 365);
                    if (years == 1)
                        return "vor 1 Jahr";
                    else
                        return $"vor {years} Jahren";
                }
            }
            
            return "unbekannt";
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}