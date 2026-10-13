using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace logbook.Helpers
{
    /// <summary>
    /// Иконка в системном трее с контекстным меню.
    /// </summary>
    public class TrayHelper : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private Window _window;

        public TrayHelper(Window window, string iconPath, string tooltip)
        {
            _window = window;

            _notifyIcon = new NotifyIcon
            {
                Icon = LoadIcon(iconPath),
                Visible = false,
                Text = tooltip
            };

            // Двойной клик — показать окно
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();

            // Контекстное меню
            var menu = new ContextMenuStrip();
            menu.Items.Add("Открыть", null, (s, e) => ShowWindow());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Выход", null, (s, e) => ExitApp());
            _notifyIcon.ContextMenuStrip = menu;
        }

        /// <summary>
        /// Показать иконку в трее.
        /// </summary>
        public void Show()
        {
            if (_notifyIcon != null)
                _notifyIcon.Visible = true;
        }

        /// <summary>
        /// Скрыть иконку.
        /// </summary>
        public void Hide()
        {
            if (_notifyIcon != null)
                _notifyIcon.Visible = false;
        }

        /// <summary>
        /// Показать главное окно.
        /// </summary>
        private void ShowWindow()
        {
            if (_window == null) return;

            _window.Show();
            _window.WindowState = WindowState.Normal;
            _window.Activate();
            _window.Topmost = true;
            _window.Topmost = false;
            _window.Focus();
        }

        /// <summary>
        /// Выход из приложения.
        /// </summary>
        private void ExitApp()
        {
            Hide();
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Загрузить иконку из ресурсов.
        /// </summary>
        private Icon LoadIcon(string path)
        {
            try
            {
                var uri = new Uri(path, UriKind.RelativeOrAbsolute);

                // Если это ресурс приложения
                var streamInfo = System.Windows.Application.GetResourceStream(uri);
                if (streamInfo != null)
                    return new Icon(streamInfo.Stream);

                // Иначе — из файла
                return new Icon(path);
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
    }
}