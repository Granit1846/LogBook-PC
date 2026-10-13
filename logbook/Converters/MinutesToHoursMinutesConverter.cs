using System;
using System.Globalization;
using System.Windows.Data;

namespace logbook.Converters
{
    public class MinutesToHoursMinutesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int minutes)
            {
                int hours = minutes / 60;
                int mins = minutes % 60;
                return $"{hours}:{mins:D2}";
            }
            return "0:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}