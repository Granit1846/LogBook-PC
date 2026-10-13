using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using logbook.Helpers;

namespace logbook.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly logbook.ViewModels.MainViewModel _mainVm;

        public SettingsWindow(logbook.ViewModels.MainViewModel mainVm)
        {
            InitializeComponent();
            _mainVm = mainVm;

            Loaded += (s, e) => logbook.Helpers.DarkTitleBarHelper.Apply(this);

            LoadSettings();
            UpdateDataFolderTextBox();

            // Версия — из FileVersion собранной сборки (<FileVersion> в .csproj)
            VersionTextBlock.Text = UpdateChecker.CurrentVersion;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        // ==================== ДЕЙСТВИЯ ====================

        private void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            _mainVm.AddProfileCommand.Execute(null);
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new DeleteProfileWindow(_mainVm.GetStorage(), _mainVm.SelectedProfile)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                _mainVm.RefreshProfiles();
            }
        }

        private void SaveAll_Click(object sender, RoutedEventArgs e)
        {
            _mainVm.SaveAllCommand.Execute(null);
        }

        private void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            CreateBackup();
        }

        private void LoadBackup_Click(object sender, RoutedEventArgs e)
        {
            LoadBackup();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            int currentTab = 0;
            if (Application.Current.MainWindow is logbook.MainWindow mw &&
                mw.DataContext is logbook.ViewModels.MainViewModel vm)
            {
                currentTab = vm.SelectedTabIndex;
            }

            var printWindow = new PrintWindow(_mainVm, currentTab)
            {
                Owner = this
            };
            printWindow.ShowDialog();
        }

        // ==================== ДОПОЛНИТЕЛЬНО ====================

        private void BrowsePath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = LocalizationManager.Get("Settings_SelectFolder"),
                    InitialDirectory = _mainVm.SettingsService.Settings.DataFolder
                };

                if (dialog.ShowDialog() != true)
                    return;

                string newPath = dialog.FolderName;

                if (string.Equals(newPath,
                    _mainVm.SettingsService.Settings.DataFolder,
                    StringComparison.OrdinalIgnoreCase))
                    return;

                var result = MessageBox.Show(
                    LocalizationManager.Format("Msg_MoveDataConfirm",
                        _mainVm.SettingsService.Settings.DataFolder,
                        newPath),
                    LocalizationManager.Get("Msg_ChangePath"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                _mainVm.ChangeDataFolder(newPath);
                UpdateDataFolderTextBox();

                var restartWindow = new RestartWindow { Owner = this };
                restartWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationManager.Get("Msg_Error")}:\n{ex.Message}",
                    LocalizationManager.Get("Msg_Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== О ПРОГРАММЕ ====================

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // Выбор файла по текущему языку
                string lang = LocalizationManager.CurrentLanguage;
                string fileName = lang == "en" ? "help.en.html" : "help.html";
                string helpPath = System.IO.Path.Combine(baseDir, "Help", fileName);

                // Если английского файла нет — берём русский
                if (!System.IO.File.Exists(helpPath))
                {
                    helpPath = System.IO.Path.Combine(baseDir, "Help", "help.html");
                }

                if (!System.IO.File.Exists(helpPath))
                {
                    MessageBox.Show(
                        LocalizationManager.Format("Msg_HelpFileNotFound", helpPath),
                        LocalizationManager.Get("About_Help"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = helpPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationManager.Get("Msg_HelpOpenError")}:\n{ex.Message}",
                    LocalizationManager.Get("Msg_Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Website_Click(object sender, RoutedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(UpdateChecker.Website) { UseShellExecute = true }); }
            catch { MessageBox.Show(LocalizationManager.Get("Msg_HelpOpenError"), LocalizationManager.Get("Msg_Error")); }
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn)
                    btn.IsEnabled = false;

                var info = await logbook.Helpers.UpdateChecker.CheckAsync(
                    _mainVm.SettingsService.Settings.SupabaseUrl);

                var dialog = info == null
                    ? new UpdateWindow(UpdateChecker.CurrentVersion)
                    : new UpdateWindow(UpdateChecker.CurrentVersion, info.Version, info.Notes, info.Url, info.FileName);
                dialog.Owner = this;
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationManager.Get("Msg_UpdateError")}:\n{ex.Message}",
                    LocalizationManager.Get("Msg_Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (sender is Button btn)
                    btn.IsEnabled = true;
            }
        }

        // ==================== РЕЗЕРВНОЕ КОПИРОВАНИЕ ====================

        private void CreateBackup()
        {
            try
            {
                _mainVm.SettingsService.EnsureFoldersExist();

                string sourceFile = _mainVm.SettingsService.GetDataFilePath();

                if (!File.Exists(sourceFile))
                {
                    MessageBox.Show(
                        LocalizationManager.Get("Msg_NoDataForBackup"),
                        LocalizationManager.Get("Msg_BackupTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                string defaultFileName = $"logbook_backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json";

                var saveDialog = new SaveFileDialog
                {
                    Title = LocalizationManager.Get("Msg_SaveBackup"),
                    Filter = LocalizationManager.Get("Msg_JsonFilter"),
                    FileName = defaultFileName,
                    InitialDirectory = _mainVm.SettingsService.GetBackupFolder(),
                    DefaultExt = "json"
                };

                if (saveDialog.ShowDialog() != true)
                    return;

                File.Copy(sourceFile, saveDialog.FileName, overwrite: true);

                MessageBox.Show(
                    $"{LocalizationManager.Get("Msg_BackupCreated")}\n{saveDialog.FileName}",
                    LocalizationManager.Get("Msg_Done"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationManager.Get("Msg_BackupCreateError")}:\n{ex.Message}",
                    LocalizationManager.Get("Msg_Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadBackup()
        {
            try
            {
                _mainVm.SettingsService.EnsureFoldersExist();

                var openDialog = new OpenFileDialog
                {
                    Title = LocalizationManager.Get("Msg_LoadBackup"),
                    Filter = LocalizationManager.Get("Msg_JsonFilter"),
                    InitialDirectory = _mainVm.SettingsService.GetBackupFolder(),
                    DefaultExt = "json"
                };

                if (openDialog.ShowDialog() != true)
                    return;

                var result = MessageBox.Show(
                    LocalizationManager.Get("Msg_BackupRestoreConfirm"),
                    LocalizationManager.Get("Msg_Attention"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                string targetFile = _mainVm.SettingsService.GetDataFilePath();
                File.Copy(openDialog.FileName, targetFile, overwrite: true);

                var restartWindow = new RestartWindow { Owner = this };
                restartWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationManager.Get("Msg_BackupLoadError")}:\n{ex.Message}",
                    LocalizationManager.Get("Msg_Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== НАСТРОЙКИ ИНТЕРФЕЙСА ====================

        private void LoadSettings()
        {
            var s = _mainVm.SettingsService.Settings;

            if (LanguageComboBox != null)
            {
                foreach (ComboBoxItem item in LanguageComboBox.Items)
                {
                    if ((string)item.Tag == s.Language)
                    {
                        LanguageComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            if (NotificationsCheckBox != null)
                NotificationsCheckBox.IsChecked = s.Notifications;

            if (ThemeComboBox != null)
            {
                foreach (ComboBoxItem item in ThemeComboBox.Items)
                {
                    if ((string)item.Tag == s.Theme)
                    {
                        ThemeComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            if (FontSizeComboBox != null)
            {
                foreach (ComboBoxItem item in FontSizeComboBox.Items)
                {
                    if ((string)item.Tag == s.FontSize)
                    {
                        FontSizeComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            if (FontFamilyComboBox != null)
            {
                foreach (ComboBoxItem item in FontFamilyComboBox.Items)
                {
                    if ((string)item.Tag == s.FontFamily)
                    {
                        FontFamilyComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            if (AutoSaveCheckBox != null)
                AutoSaveCheckBox.IsChecked = s.AutoSave;

            if (AutoCheckUpdatesCheckBox != null)
                AutoCheckUpdatesCheckBox.IsChecked = s.AutoCheckUpdates;

            if (UndoSizeComboBox != null)
            {
                foreach (ComboBoxItem item in UndoSizeComboBox.Items)
                {
                    if (int.TryParse((string)item.Tag, out int val) && val == s.UndoSize)
                    {
                        UndoSizeComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            if (AutoStartCheckBox != null)
                AutoStartCheckBox.IsChecked = s.StartWithWindows;

            if (MinimizeToTrayCheckBox != null)
                MinimizeToTrayCheckBox.IsChecked = s.MinimizeToTray;

            if (SupabaseUrlTextBox != null)
                SupabaseUrlTextBox.Text = s.SupabaseUrl;

            if (SupabaseKeyTextBox != null)
                SupabaseKeyTextBox.Text = s.SupabaseKey;
        }

        private void SaveSettings()
        {
            var s = _mainVm.SettingsService.Settings;

            if (LanguageComboBox.SelectedItem is ComboBoxItem langItem)
                s.Language = (string)langItem.Tag;

            if (NotificationsCheckBox != null)
                s.Notifications = NotificationsCheckBox.IsChecked == true;

            if (ThemeComboBox.SelectedItem is ComboBoxItem themeItem)
                s.Theme = (string)themeItem.Tag;

            if (FontSizeComboBox.SelectedItem is ComboBoxItem fontSizeItem)
                s.FontSize = (string)fontSizeItem.Tag;

            if (FontFamilyComboBox.SelectedItem is ComboBoxItem fontItem)
                s.FontFamily = (string)fontItem.Tag;

            if (AutoSaveCheckBox != null)
                s.AutoSave = AutoSaveCheckBox.IsChecked == true;

            if (AutoCheckUpdatesCheckBox != null)
                s.AutoCheckUpdates = AutoCheckUpdatesCheckBox.IsChecked == true;

            if (UndoSizeComboBox?.SelectedItem is ComboBoxItem undoItem &&
                int.TryParse((string)undoItem.Tag, out int newUndoSize))
            {
                s.UndoSize = newUndoSize;
                _mainVm.ApplyUndoSize(newUndoSize);
            }

            if (AutoStartCheckBox != null)
            {
                bool autostart = AutoStartCheckBox.IsChecked == true;
                if (s.StartWithWindows != autostart)
                {
                    s.StartWithWindows = autostart;
                    logbook.Helpers.AutoStartHelper.SetAutoStart(autostart);
                }
            }

            if (MinimizeToTrayCheckBox != null)
                s.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked == true;

            _mainVm.SettingsService.Save();

            logbook.Helpers.ThemeManager.Apply(s.Theme, s.FontFamily, s.FontSize);
            logbook.Helpers.LocalizationManager.Apply(s.Language);
            _mainVm.UpdateNotifications();
            _mainVm.RefreshLastSaveStatus();

            if (SupabaseUrlTextBox != null) s.SupabaseUrl = SupabaseUrlTextBox.Text.Trim();
            if (SupabaseKeyTextBox != null) s.SupabaseKey = SupabaseKeyTextBox.Text.Trim();

            foreach (Window w in Application.Current.Windows)
            {
                logbook.Helpers.DarkTitleBarHelper.Apply(w);
            }
        }

                private void Sync_Click(object sender, RoutedEventArgs e)
        {
            _mainVm.SyncCommand?.Execute(null);
        }

        private void UpdateDataFolderTextBox()
        {
            if (DataFolderTextBox != null)
            {
                DataFolderTextBox.Text = _mainVm.SettingsService.Settings.DataFolder;
            }
        }
    }
}