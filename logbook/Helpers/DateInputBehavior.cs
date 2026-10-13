using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace logbook.Helpers
{
    /// <summary>
    /// Автоформатирование даты в TextBox.
    /// При потере фокуса:
    ///   - "14.08.26"     → "14.08.2026"
    ///   - "26,04,2026"   → "26.04.2026"
    ///   - "26,04,26"     → "26.04.2026"
    /// Если уже "14.08.2026" — не трогаем.
    /// </summary>
    public static class DateInputBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(DateInputBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
            => (bool)obj.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject obj, bool value)
            => obj.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb)
            {
                if ((bool)e.NewValue)
                    tb.LostFocus += TextBox_LostFocus;
                else
                    tb.LostFocus -= TextBox_LostFocus;
            }
        }

        private static void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!(sender is TextBox tb)) return;

            string text = tb.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(text))
                return;

            // Заменяем запятые на точки
            string normalized = text.Replace(',', '.');

            // Проверяем, что формат "dd.MM.yy" или "dd.MM.yyyy"
            var match = Regex.Match(normalized, @"^(\d{1,2})\.(\d{1,2})\.(\d{2,4})$");
            if (!match.Success)
                return;

            string day = match.Groups[1].Value.PadLeft(2, '0');
            string month = match.Groups[2].Value.PadLeft(2, '0');
            string year = match.Groups[3].Value;

            // Если год — 2 знака → добавляем "20"
            if (year.Length == 2)
                year = "20" + year;

            tb.Text = $"{day}.{month}.{year}";
        }
    }
}