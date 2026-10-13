using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace logbook.Helpers
{
    /// <summary>
    /// Дата в формате «дд.мм.гг».
    /// Автоматически вставляет «.» после 2 и 5 цифр.
    /// Любые другие символы игнорируются.
    /// </summary>
    public static class DateDotBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(DateDotBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
            => (bool)obj.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject obj, bool value)
            => obj.SetValue(IsEnabledProperty, value);

        private static bool _isFormatting = false;

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb)
            {
                if ((bool)e.NewValue)
                {
                    tb.TextChanged += Tb_TextChanged;
                    DataObject.AddPastingHandler(tb, Tb_Pasting);
                }
                else
                {
                    tb.TextChanged -= Tb_TextChanged;
                    DataObject.RemovePastingHandler(tb, Tb_Pasting);
                }
            }
        }

        private static void Tb_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(typeof(string)))
            {
                e.CancelCommand();
                return;
            }

            string text = (string)e.DataObject.GetData(typeof(string));
            string digits = Regex.Replace(text, @"[^0-9]", "");

            if (digits.Length == 0)
            {
                e.CancelCommand();
                return;
            }

            var tb = (TextBox)sender;
            e.CancelCommand();

            // Объединяем с текущими цифрами
            string currentDigits = Regex.Replace(tb.Text ?? "", @"[^0-9]", "");
            string combined = currentDigits + digits;

            if (combined.Length > 6)
                combined = combined.Substring(0, 6);

            ApplyFormat(tb, combined);
        }

        private static void Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFormatting) return;
            if (!(sender is TextBox tb)) return;

            string text = tb.Text ?? "";

            // Уже в правильном формате?
            if (Regex.IsMatch(text, @"^\d{0,2}(\.\d{0,2}(\.\d{0,2})?)?$"))
                return;

            // Берём только цифры
            string digits = Regex.Replace(text, @"[^0-9]", "");

            if (digits.Length > 6)
                digits = digits.Substring(0, 6);

            ApplyFormat(tb, digits);
        }

        private static void ApplyFormat(TextBox tb, string digits)
        {
            _isFormatting = true;
            try
            {
                string formatted = FormatDigits(digits);

                if (tb.Text != formatted)
                {
                    tb.Text = formatted;
                    tb.CaretIndex = formatted.Length;
                }
            }
            finally
            {
                _isFormatting = false;
            }
        }

        /// <summary>
        /// Форматирует цифры в «дд.мм.гг».
        /// </summary>
        private static string FormatDigits(string digits)
        {
            if (digits.Length <= 2)
                return digits;

            if (digits.Length <= 4)
                return digits.Substring(0, 2) + "." + digits.Substring(2);

            // 5 или 6 цифр
            string result = digits.Substring(0, 2) + "." + digits.Substring(2, 2) + ".";

            if (digits.Length >= 6)
                result += digits.Substring(4, 2);
            else
                result += digits.Substring(4); // 1 символ

            return result;
        }
    }
}