using System.Windows;

namespace logbook.ViewModels
{
    public partial class MainViewModel
    {
        // ============================================================
        // ==================== НАСТРОЙКИ =============================
        // ============================================================

        private void OpenSettings()
        {
            var settingsWindow = new logbook.Views.SettingsWindow(this)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            settingsWindow.ShowDialog();
        }

        /// <summary>
        /// Сменить путь к данным (переносит файлы).
        /// </summary>
        public void ChangeDataFolder(string newFolder)
        {
            _storage.ChangeDataFolder(newFolder);
        }

        /// <summary>
        /// Возвращает StorageService (для окна настроек).
        /// </summary>
        public Services.StorageService GetStorage()
        {
            return _storage;
        }

        private void ToggleLeftPanel()
        {
            IsLeftPanelCollapsed = !IsLeftPanelCollapsed;
        }
    }
}