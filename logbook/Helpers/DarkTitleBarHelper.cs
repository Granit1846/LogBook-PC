using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace logbook.Helpers
{
    /// <summary>
    /// Делает системный заголовок окна тёмным (Windows 10 1809+ / Windows 11).
    /// </summary>
    public static class DarkTitleBarHelper
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

        /// <summary>
        /// Применить тёмный заголовок к окну (если текущая тема — тёмная).
        /// Вызывать в Loaded окна.
        /// </summary>
        public static void Apply(Window window)
        {
            if (window == null) return;

            try
            {
                int value = ThemeManager.IsDarkTheme ? 1 : 0;

                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                int result = DwmSetWindowAttribute(
                    hwnd,
                    DWMWA_USE_IMMERSIVE_DARK_MODE,
                    ref value,
                    sizeof(int));

                if (result != 0)
                {
                    DwmSetWindowAttribute(
                        hwnd,
                        DWMWA_USE_IMMERSIVE_DARK_MODE_OLD,
                        ref value,
                        sizeof(int));
                }
            }
            catch
            {
                // Игнорируем
            }
        }
    }
}