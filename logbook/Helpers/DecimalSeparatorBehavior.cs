using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace logbook.Helpers
{
    /// <summary>
    /// Поведение для TextBox: все нецифровые символы заменяются на запятую.
    /// В поле допускается только одна запятая, и она не может быть первой.
    ///
    /// Примеры:
    ///   "1.5"   → "1,5"
    ///   "1a2"   → "1,2"
    ///   "1,5,3" → "1,53"  (вторая запятая блокируется)
    ///   "abc"   → ","     (первая буква → запятая, остальные блокируются)
    ///   ".5"    → блокируется (нельзя начинать с запятой)
    /// </summary>
    public static class DecimalSeparatorBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(DecimalSeparatorBehavior),
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
                {
                    tb.PreviewTextInput += Tb_PreviewTextInput;
                    DataObject.AddPastingHandler(tb, Tb_Pasting);
                }
                else
                {
                    tb.PreviewTextInput -= Tb_PreviewTextInput;
                    DataObject.RemovePastingHandler(tb, Tb_Pasting);
                }
            }
        }

        private static void Tb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = (TextBox)sender;
            string input = e.Text;

            if (string.IsNullOrEmpty(input)) return;

            // Обрабатываем по символам
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (char.IsDigit(c))
                {
                    // Цифра — пропускаем
                    continue;
                }

                // Не цифра → превращаем в запятую
                string comma = ",";

                // Проверяем, можно ли вставить запятую
                if (!CanInsertChar(tb, comma))
                {
                    e.Handled = true;
                    return;
                }

                // Вставляем запятую вручную
                e.Handled = true;

                int selStart = tb.SelectionStart;
                int selLen = tb.SelectionLength;

                string current = tb.Text ?? "";
                string newText = current.Substring(0, selStart)
                               + comma
                               + current.Substring(selStart + selLen);

                tb.Text = newText;
                tb.CaretIndex = selStart + comma.Length;
                return;   // обработали один символ — выходим
            }

            // Все символы — цифры, пропускаем как есть
            if (!IsValidInput(tb, input))
            {
                e.Handled = true;
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
            var tb = (TextBox)sender;

            // Заменяем все нецифры на запятую
            string cleaned = "";
            foreach (char c in text)
            {
                cleaned += char.IsDigit(c) ? c.ToString() : ",";
            }

            // Оставляем только первую запятую, если их несколько
            int firstComma = cleaned.IndexOf(',');
            if (firstComma >= 0)
            {
                // После первой запятой — все остальные убираем
                string before = cleaned.Substring(0, firstComma + 1);
                string after = cleaned.Substring(firstComma + 1).Replace(",", "");
                cleaned = before + after;
            }

            // Нельзя начинать с запятой
            if (cleaned.StartsWith(","))
                cleaned = cleaned.Substring(1);

            if (string.IsNullOrEmpty(cleaned))
            {
                e.CancelCommand();
                return;
            }

            // Проверяем, что итоговый текст корректен
            string current = tb.Text ?? "";
            int selStart = tb.SelectionStart;
            int selLen = tb.SelectionLength;

            string newText = current.Substring(0, selStart)
                           + cleaned
                           + current.Substring(selStart + selLen);

            if (!IsValidFullText(newText))
            {
                e.CancelCommand();
                return;
            }

            // Вставляем вручную
            e.CancelCommand();
            tb.Text = newText;
            tb.CaretIndex = selStart + cleaned.Length;
        }

        /// <summary>
        /// Можно ли вставить символ в текущую позицию.
        /// </summary>
        private static bool CanInsertChar(TextBox tb, string input)
        {
            string current = tb.Text ?? "";
            int selStart = tb.SelectionStart;
            int selLen = tb.SelectionLength;

            string newText = current.Substring(0, selStart)
                           + input
                           + current.Substring(selStart + selLen);

            return IsValidFullText(newText);
        }

        /// <summary>
        /// Проверяет, что текст корректен:
        ///   - только цифры и максимум одна запятая;
        ///   - не начинается с запятой.
        /// </summary>
        private static bool IsValidFullText(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;

            // Не начинается с запятой
            if (text.StartsWith(",")) return false;

            int commas = 0;
            foreach (char c in text)
            {
                if (c == ',')
                {
                    commas++;
                    if (commas > 1) return false;
                }
                else if (!char.IsDigit(c))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Проверяет, что ввод (только цифры) допустим.
        /// </summary>
        private static bool IsValidInput(TextBox tb, string input)
        {
            if (string.IsNullOrEmpty(input)) return true;
            if (!Regex.IsMatch(input, "^[0-9]+$")) return false;

            string current = tb.Text ?? "";
            int selStart = tb.SelectionStart;
            int selLen = tb.SelectionLength;

            string newText = current.Substring(0, selStart)
                           + input
                           + current.Substring(selStart + selLen);

            return IsValidFullText(newText);
        }
    }
}