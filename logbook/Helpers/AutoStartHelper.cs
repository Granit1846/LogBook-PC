using System;
using Microsoft.Win32;

namespace logbook.Helpers
{
    /// <summary>
    /// Управление автозапуском приложения через реестр
    /// (HKCU\Software\Microsoft\Windows\CurrentVersion\Run).
    /// </summary>
    public static class AutoStartHelper
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "Logbook";

        /// <summary>
        /// Включить/выключить автозапуск.
        /// </summary>
        public static void SetAutoStart(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
                if (key == null) return;

                if (enabled)
                {
                    string exePath = GetExecutablePath();
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
                else
                {
                    if (key.GetValue(AppName) != null)
                        key.DeleteValue(AppName, throwOnMissingValue: false);
                }
            }
            catch
            {
                // Игнорируем ошибки
            }
        }

        /// <summary>
        /// Проверить, включён ли автозапуск.
        /// </summary>
        public static bool IsAutoStartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
                if (key == null) return false;

                var value = key.GetValue(AppName) as string;
                return !string.IsNullOrWhiteSpace(value);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Путь к .exe текущего приложения.
        /// </summary>
        private static string GetExecutablePath()
        {
            return Environment.ProcessPath
                   ?? System.IO.Path.Combine(AppContext.BaseDirectory, "Logbook.exe");
        }
    }
}