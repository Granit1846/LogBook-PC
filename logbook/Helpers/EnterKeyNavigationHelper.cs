using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace logbook.Helpers
{
    /// <summary>
    /// Обеспечивает навигацию по клавише Enter между полями ввода.
    /// Если поле однострочное (AcceptsReturn == false), нажатие Enter
    /// переводит фокус на следующий элемент ввода (аналогично Tab),
    /// а Shift+Enter — на предыдущий (аналогично Shift+Tab).
    /// </summary>
    public static class EnterKeyNavigationHelper
    {
        private static bool _isInitialized;

        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            EventManager.RegisterClassHandler(
                typeof(TextBox),
                UIElement.KeyDownEvent,
                new KeyEventHandler(OnTextBoxKeyDown));
        }

        private static void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter && e.Key != Key.Return)
                return;

            if (sender is TextBox tb && !tb.AcceptsReturn)
            {
                var direction = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift
                    ? FocusNavigationDirection.Previous
                    : FocusNavigationDirection.Next;

                tb.MoveFocus(new TraversalRequest(direction));
                e.Handled = true;
            }
        }
    }
}
