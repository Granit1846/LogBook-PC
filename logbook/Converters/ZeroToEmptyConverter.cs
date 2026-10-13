using System;
using System.Globalization;
using System.Windows.Data;

namespace logbook.Converters
{
    /// <summary>
    /// int → string: 0 становится "", остальное — как есть.
    /// string → int: "" или null становится 0.
    /// </summary>
    public class ZeroToEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i && i == 0)
                return "";
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value as string;
            if (string.IsNullOrWhiteSpace(s))
                return 0;
            if (int.TryParse(s, out int n))
                return n;
            return 0;
        }
    }
}