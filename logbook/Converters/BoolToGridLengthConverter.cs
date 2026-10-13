using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace logbook.Converters
{
    /// <summary>
    /// bool → GridLength: true → 0, false → значение из параметра (по умолчанию 400).
    /// </summary>
    public class BoolToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isCollapsed = value is bool b && b;

            if (isCollapsed)
                return new GridLength(0);

            // Параметр — ширина в виде строки (например, "400")
            if (parameter is string s && double.TryParse(s, NumberStyles.Any,
                CultureInfo.InvariantCulture, out double w))
            {
                return new GridLength(w);
            }

            return new GridLength(400);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}