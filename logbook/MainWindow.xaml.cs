using logbook.Helpers;
using logbook.ViewModels;
using logbook.Views;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace logbook
{
    public partial class MainWindow : Window
    {
        // ==================== ТЁМНЫЙ ЗАГОЛОВОК ====================

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

        // ==================== ТРЕЙ ====================

        private TrayHelper _tray;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString() + "\n\n--- INNER ---\n" +
                    (ex.InnerException?.ToString() ?? "(null)"),
                    "MainWindow XAML Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyDarkTitleBar();
            InitTray();
        }

        /// <summary>
        /// Инициализация иконки в трее.
        /// </summary>
        private void InitTray()
        {
            _tray = new TrayHelper(this, "pack://application:,,,/Resources/Icons/logbook.ico", "Logbook");
        }

        /// <summary>
        /// Тёмный системный заголовок окна.
        /// </summary>
        private void ApplyDarkTitleBar()
        {
            try
            {
                int value = ThemeManager.IsDarkTheme ? 1 : 0;
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero) return;

                int result = DwmSetWindowAttribute(
                    hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));

                if (result != 0)
                {
                    DwmSetWindowAttribute(
                        hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref value, sizeof(int));
                }
            }
            catch { }
        }

        // ==================== ПРОЧЕЕ ====================

        public void SetInitialProfile(string profile)
        {
            if (DataContext is logbook.ViewModels.MainViewModel vm)
            {
                vm.SetInitialProfile(profile);
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Получаем VM
            if (!(DataContext is MainViewModel vm))
                return;

            // Нет несохранённых изменений — выходим молча
            if (!vm.IsDirty)
                return;

            var dlg = new ConfirmCloseWindow { Owner = this };
            var result = dlg.ShowDialog();

            // null = крестик → отменяем закрытие
            if (result != true)
            {
                e.Cancel = true;
                return;
            }

            // result == true → пользователь подтвердил выход
            if (dlg.SaveBeforeClose)
            {
                // Сохранить без MessageBox — окно уже закрывается
                vm.SaveAllSilent();
            }
            // иначе — просто выходим без сохранения
        }

        public void ShowResourceAlertsIfNeeded()
        {
            if (DataContext is logbook.ViewModels.MainViewModel vm)
            {
                if (!vm.SettingsService.Settings.Notifications) return;

                var oilMessages = vm.BuildOilAlerts();
                if (oilMessages.Count > 0)
                {
                    var w = new logbook.Views.OilAlertWindow(oilMessages) { Owner = this };
                    w.ShowDialog();
                }

                var resourceMessages = vm.BuildResourceAlerts();
                if (resourceMessages.Count > 0)
                {
                    var w = new logbook.Views.ResourceAlertWindow(resourceMessages) { Owner = this };
                    w.ShowDialog();
                }
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (Keyboard.FocusedElement is TextBox)
                {
                    base.OnPreviewKeyDown(e);
                    return;
                }

                if (DataContext is logbook.ViewModels.MainViewModel vm)
                {
                    vm.UndoCommand.Execute(null);
                    e.Handled = true;
                }
            }

            base.OnPreviewKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Останавливаем таймеры
            if (DataContext is logbook.ViewModels.MainViewModel vm)
            {
                vm.StopAutoSave();
                vm.StopRecalcTimer();
            }

            // Скрываем иконку в трее
            _tray?.Dispose();  // если TrayHelper реализует IDisposable

            // Завершаем приложение
            Application.Current.Shutdown();
        }
    }
}