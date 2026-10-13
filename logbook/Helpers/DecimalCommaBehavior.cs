using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace logbook.Helpers
{
    /// <summary>
    /// Дробное число с разделителем «,».
    /// Заменяет «.» на «,», блокирует вторую «,»,
    /// обрезает лишние знаки после «,».
    /// </summary>
    public static class DecimalCommaBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(DecimalCommaBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
            => (bool)obj.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject obj, bool value)
            => obj.SetValue(IsEnabledProperty, value);

        // Максимум знаков после запятой
        public static readonly DependencyProperty MaxDecimalPlacesProperty =
            DependencyProperty.RegisterAttached(
                "MaxDecimalPlaces",
                typeof(int),
                typeof(DecimalCommaBehavior),
                new PropertyMetadata(2));

        public static int GetMaxDecimalPlaces(DependencyObject obj)
            => (int)obj.GetValue(MaxDecimalPlacesProperty);

        public static void SetMaxDecimalPlaces(DependencyObject obj, int value)
            => obj.SetValue(MaxDecimalPlacesProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb)
            {
                if ((bool)e.NewValue)
                {
                    tb.PreviewTextInput += Tb_PreviewTextInput;
                    tb.TextChanged += Tb_TextChanged;
                    DataObject.AddPastingHandler(tb, Tb_Pasting);
                }
                else
                {
                    tb.PreviewTextInput -= Tb_PreviewTextInput;
                    tb.TextChanged -= Tb_TextChanged;
                    DataObject.RemovePastingHandler(tb, Tb_Pasting);
                }
            }
        }

        private static void Tb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = (TextBox)sender;

            // Заменяем «.» на «,»
            string input = e.Text.Replace('.', ',');

            // Только цифры и «,»
            if (!Regex.IsMatch(input, "^[0-9,]+$"))
            {
                e.Handled = true;
                return;
            }

            // Считаем, сколько «,» уже есть (исключая выделение)
            string current = tb.Text ?? "";
            int selStart = tb.SelectionStart;
            int selLen = tb.SelectionLength;

            string newText = current.Substring(0, selStart)
                           + input
                           + current.Substring(selStart + selLen);

            // Не более одной «,»
            int commas = newText.Count(c => c == ',');
            if (commas > 1)
            {
                e.Handled = true;
                return;
            }

            // Нельзя начинать с «,»
            if (newText.StartsWith(","))
            {
                e.Handled = true;
                return;
            }

            // Проверяем число знаков после «,»
            int commaIndex = newText.IndexOf(',');
            if (commaIndex >= 0)
            {
                int decimals = newText.Length - commaIndex - 1;
                int max = GetMaxDecimalPlaces(tb);
                if (decimals > max)
                {
                    e.Handled = true;
                    return;
                }
            }

            // Если «,» заменена — принудительно вставляем её
            if (e.Text.Contains('.'))
            {
                e.Handled = true;

                string before = current.Substring(0, selStart);
                string after = current.Substring(selStart + selLen);
                tb.Text = before + input + after;
                tb.CaretIndex = selStart + input.Length;
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
            text = text.Replace('.', ',');

            var tb = (TextBox)sender;

            // Проверка на формат
            if (!Regex.IsMatch(text, "^[0-9,]+$"))
            {
                e.CancelCommand();
                return;
            }

            string current = tb.Text ?? "";
            int selStart = tb.SelectionStart;
            int selLen = tb.SelectionLength;

            string newText = current.Substring(0, selStart)
                           + text
                           + current.Substring(selStart + selLen);

            int commas = newText.Count(c => c == ',');
            if (commas > 1)
            {
                e.CancelCommand();
                return;
            }

            if (newText.StartsWith(","))
            {
                e.CancelCommand();
                return;
            }

            int commaIndex = newText.IndexOf(',');
            if (commaIndex >= 0)
            {
                int decimals = newText.Length - commaIndex - 1;
                int max = GetMaxDecimalPlaces(tb);
                if (decimals > max)
                {
                    e.CancelCommand();
                    return;
                }
            }

            // Если в тексте есть «.» — вставляем вручную
            if (text.Contains(','))
            {
                e.CancelCommand();
                tb.Text = newText;
                tb.CaretIndex = selStart + text.Length;
            }
        }

        private static void Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            int max = GetMaxDecimalPlaces(tb);

            string text = tb.Text ?? "";
            int commaIndex = text.IndexOf(',');
            if (commaIndex < 0) return;

            int decimals = text.Length - commaIndex - 1;
            if (decimals > max)
            {
                // Обрезаем
                int caret = tb.CaretIndex;
                string trimmed = text.Substring(0, commaIndex + 1 + max);
                tb.Text = trimmed;
                tb.CaretIndex = System.Math.Min(caret, trimmed.Length);
            }
        }
    }
}