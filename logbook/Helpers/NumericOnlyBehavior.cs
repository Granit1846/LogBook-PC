using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace logbook.Helpers
{
    public static class NumericOnlyBehavior
    {
        public static readonly DependencyProperty IsNumericOnlyProperty =
            DependencyProperty.RegisterAttached(
                "IsNumericOnly",
                typeof(bool),
                typeof(NumericOnlyBehavior),
                new PropertyMetadata(false, OnIsNumericOnlyChanged));

        public static bool GetIsNumericOnly(DependencyObject obj)
            => (bool)obj.GetValue(IsNumericOnlyProperty);

        public static void SetIsNumericOnly(DependencyObject obj, bool value)
            => obj.SetValue(IsNumericOnlyProperty, value);

        private static void OnIsNumericOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.PreviewTextInput += TextBox_PreviewTextInput;
                    DataObject.AddPastingHandler(textBox, TextBox_Pasting);
                }
                else
                {
                    textBox.PreviewTextInput -= TextBox_PreviewTextInput;
                    DataObject.RemovePastingHandler(textBox, TextBox_Pasting);
                }
            }
        }

        private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = (TextBox)sender;
            e.Handled = !IsInputValid(tb, e.Text);
        }

        private static void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(typeof(string)))
            {
                e.CancelCommand();
                return;
            }

            string text = (string)e.DataObject.GetData(typeof(string));
            var tb = (TextBox)sender;

            if (!IsInputValid(tb, text))
                e.CancelCommand();
        }

        /// <summary>
        /// Проверка ввода: только цифры, одна запятая или одна точка.
        /// </summary>
        private static bool IsInputValid(TextBox tb, string input)
        {
            // Разрешаем: цифры, запятая, точка
            if (!Regex.IsMatch(input, "^[0-9,.]+$"))
                return false;

            // Формируем предполагаемый итоговый текст
            string current = tb.Text ?? "";
            int selStart = tb.SelectionStart;
            int selLen = tb.SelectionLength;

            string newText = current.Substring(0, selStart)
                           + input
                           + current.Substring(selStart + selLen);

            // Не более одной запятой или точки (учитываем оба варианта)
            int separators = 0;
            foreach (char c in newText)
                if (c == ',' || c == '.') separators++;

            if (separators > 1) return false;

            // Нельзя начинать с разделителя
            if (newText.Length > 0 && (newText[0] == ',' || newText[0] == '.'))
                return false;

            return true;
        }
    }
}