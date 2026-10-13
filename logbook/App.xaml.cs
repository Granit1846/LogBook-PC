using System;
using System.Windows;
using logbook.Helpers;
using logbook.Services;
using logbook.Views;

namespace logbook
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            SettingsService settingsService;
            try
            {
                settingsService = new SettingsService();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось открыть настройки LogBook. Файл настроек не изменён.\n\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            ThemeManager.Apply(
                settingsService.Settings.Theme,
                settingsService.Settings.FontFamily,
                settingsService.Settings.FontSize);

            LocalizationManager.Apply(settingsService.Settings.Language);

            EnterKeyNavigationHelper.Initialize();

            StorageService storage;
            try
            {
                storage = new StorageService(settingsService);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось открыть данные LogBook. Исходный файл не изменён.\n\n{ex.Message}",
                    LocalizationManager.Get("Msg_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var selectWindow = new ProfileSelectWindow(storage);

            bool? result = selectWindow.ShowDialog();

            if (result != true || string.IsNullOrWhiteSpace(selectWindow.SelectedProfile))
            {
                Shutdown();
                return;
            }

            MainWindow mainWindow;
            try
            {
                mainWindow = new MainWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString() + "\n\n--- INNER ---\n" +
                    (ex.InnerException?.ToString() ?? "(null)"),
                    "XAML Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }
            Application.Current.MainWindow = mainWindow;

            mainWindow.SetInitialProfile(selectWindow.SelectedProfile);
            mainWindow.Show();

            // ПРОВЕРКА СИНХРОНИЗАЦИИ АГРЕГАТОВ — после Show()
            if (mainWindow.DataContext is logbook.ViewModels.MainViewModel vm)
            {
                vm.CheckAndShowSetupAggregates();
            }

            mainWindow.ShowResourceAlertsIfNeeded();

            // АВТО-ПРОВЕРКА ОБНОВЛЕНИЙ — фоном, задержка 5 секунд
            _ = CheckUpdatesOnStartupAsync(settingsService, mainWindow);
        }

        /// <summary>
        /// Автоматическая проверка обновлений при запуске.
        /// Показывает окно через 5 секунд после старта, если есть новая версия.
        /// Если проверка выключена или произошла ошибка — молча выходит.
        /// </summary>
        private async System.Threading.Tasks.Task CheckUpdatesOnStartupAsync(
            SettingsService settingsService,
            Window owner)
        {
            try
            {
                // Проверка выключена пользователем
                if (!settingsService.Settings.AutoCheckUpdates)
                    return;

                // Задержка, чтобы главное окно успело отрисоваться
                await System.Threading.Tasks.Task.Delay(5000);

                // Если окно уже закрыто — не показываем
                if (owner == null || !owner.IsLoaded)
                    return;

                var info = await UpdateChecker.CheckAsync(
                    settingsService.Settings.SupabaseUrl);

                // Обновлений нет / ошибка — молча выходим
                if (info == null)
                    return;

                owner.Dispatcher.Invoke(() =>
                {
                    var updateWindow = new logbook.Views.UpdateWindow(
                        UpdateChecker.CurrentVersion,
                        info.Version,
                        info.Notes,
                        info.Url,
                        info.FileName)
                    {
                        Owner = owner
                    };
                    updateWindow.ShowDialog();
                });
            }
            catch
            {
                // Молча игнорируем любые ошибки авто-проверки
            }
        }
    }
}
