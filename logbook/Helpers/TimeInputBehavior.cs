using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace logbook.Helpers
{
    /// <summary>
    /// Автоформатирование времени в TextBox.
    /// При потере фокуса:
    ///   - "361"    → "361:00"
    ///   - "275.46" → "275:46"
    ///   - "275,46" → "275:46"
    /// Если уже "275:46" — не трогаем.
    /// </summary>
    public static class TimeInputBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(TimeInputBehavior),
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

            // Уже в формате "чч:мм" — не трогаем
            if (Regex.IsMatch(text, @"^\d+:\d{1,2}$"))
                return;

            // Заменяем точку/запятую на двоеточие
            string normalized = text.Replace(',', ':').Replace('.', ':');

            // Проверяем, что формат "N:M"
            if (Regex.IsMatch(normalized, @"^\d+:\d{1,2}$"))
            {
                tb.Text = normalized;
                return;
            }

            // Если просто целое число
            if (Regex.IsMatch(text, @"^\d+$"))
            {
                tb.Text = text + ":00";
                return;
            }

            // Если "N:M" после замены (например, "275:46") — уже нормально
            // Если что-то другое — пробуем вытащить числа
            var match = Regex.Match(text, @"^(\d+)[.,:](\d+)$");
            if (match.Success)
            {
                string hours = match.Groups[1].Value;
                string minutes = match.Groups[2].Value;

                // Минуты — 2 знака (если 1 знак — добавить 0)
                if (minutes.Length == 1)
                    minutes = "0" + minutes;

                tb.Text = $"{hours}:{minutes}";
            }
        }
    }
}