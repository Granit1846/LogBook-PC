using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using logbook.Helpers;

namespace logbook.Views
{
    public partial class UpdateWindow : Window
    {
        private readonly string? _url;
        private readonly string? _fileName;
        private bool _isDownloading = false;
        private CancellationTokenSource? _cts;

        /// <summary>
        /// Окно «Обновлений нет» — одна кнопка «ОК».
        /// </summary>
        public UpdateWindow(string currentVersion)
        {
            InitializeComponent();

            _url = null;
            _fileName = null;

            Loaded += (s, e) => DarkTitleBarHelper.Apply(this);

            TitleText.Text = LocalizationManager.Get("Update_NoUpdatesTitle");
            CurrentVersionRun.Text = currentVersion;

            // Скрываем блок «новая версия», notes, «Скачать»
            NewVersionBlock.Visibility = Visibility.Collapsed;
            WhatNewLabel.Visibility = Visibility.Collapsed;
            NotesTextBox.Visibility = Visibility.Collapsed;
            DownloadButton.Visibility = Visibility.Collapsed;
            DownloadProgressPanel.Visibility = Visibility.Collapsed;

            CloseButton.Content = LocalizationManager.Get("Btn_OK");
        }

        public UpdateWindow(string currentVersion, string newVersion, string notes, string url)
            : this(currentVersion, newVersion, notes, url, "")
        {
        }

        /// <summary>
        /// Окно «Доступно обновление» — две кнопки.
        /// </summary>
        public UpdateWindow(string currentVersion, string newVersion, string notes, string url, string fileName)
        {
            InitializeComponent();

            _url = url;
            _fileName = fileName;

            Loaded += (s, e) => DarkTitleBarHelper.Apply(this);

            TitleText.Text = LocalizationManager.Get("Update_NewVersionTitle");
            CurrentVersionRun.Text = currentVersion;
            NewVersionRun.Text = newVersion;
            DownloadProgressPanel.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(notes))
            {
                WhatNewLabel.Visibility = Visibility.Collapsed;
                NotesTextBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                NotesTextBox.Text = notes;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                DownloadButton.Visibility = Visibility.Collapsed;
            }
        }

        private async void Download_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_url))
                return;

            _isDownloading = true;
            DownloadButton.IsEnabled = false;
            CloseButton.IsEnabled = false;
            DownloadProgressPanel.Visibility = Visibility.Visible;
            DownloadProgressBar.Value = 0;
            DownloadProgressBar.IsIndeterminate = false;
            DownloadStatusText.Text = LocalizationManager.Get("Update_Downloading");

            _cts = new CancellationTokenSource();

            try
            {
                string targetFileName = !string.IsNullOrWhiteSpace(_fileName)
                    ? _fileName
                    : (_url.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? "Logbook_Setup.exe" : "LogBook_Update.zip");

                string tempPath = Path.Combine(Path.GetTempPath(), targetFileName);

                using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
                {
                    http.DefaultRequestHeaders.Add("User-Agent", "LogBook-PC");
                    using var response = await http.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    using var sourceStream = await response.Content.ReadAsStreamAsync(_cts.Token);
                    using var targetStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

                    byte[] buffer = new byte[81920];
                    long totalRead = 0;
                    int bytesRead;
                    var lastReport = DateTime.UtcNow;

                    while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, _cts.Token)) > 0)
                    {
                        await targetStream.WriteAsync(buffer, 0, bytesRead, _cts.Token);
                        totalRead += bytesRead;

                        if ((DateTime.UtcNow - lastReport).TotalMilliseconds > 80 || totalRead == totalBytes)
                        {
                            lastReport = DateTime.UtcNow;
                            double percent = totalBytes > 0 ? (double)totalRead / totalBytes * 100.0 : 0.0;
                            double mbRead = totalRead / 1048576.0;
                            double mbTotal = totalBytes / 1048576.0;

                            Dispatcher.Invoke(() =>
                            {
                                if (totalBytes > 0)
                                {
                                    DownloadProgressBar.IsIndeterminate = false;
                                    DownloadProgressBar.Value = percent;
                                    DownloadStatusText.Text = $"{LocalizationManager.Get("Update_Downloading")} {percent:F0}% ({mbRead:F1} / {mbTotal:F1} МБ)";
                                }
                                else
                                {
                                    DownloadProgressBar.IsIndeterminate = true;
                                    DownloadStatusText.Text = $"{LocalizationManager.Get("Update_Downloading")} ({mbRead:F1} МБ)";
                                }
                            });
                        }
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.IsIndeterminate = false;
                    DownloadProgressBar.Value = 100;
                    DownloadStatusText.Text = LocalizationManager.Get("Update_StartingInstaller");
                });

                await Task.Delay(400);

                if (tempPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    // Запуск инсталлятора с параметром закрытия предыдущей запущенной версии
                    var psi = new ProcessStartInfo
                    {
                        FileName = tempPath,
                        Arguments = "/CLOSEAPPLICATIONS",
                        UseShellExecute = true
                    };
                    Process.Start(psi);

                    // Закрываем текущее приложение, чтобы инсталлятор мог обновить файлы
                    Application.Current.Shutdown();
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = tempPath,
                        UseShellExecute = true
                    });
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                DownloadProgressPanel.Visibility = Visibility.Collapsed;
                DownloadButton.IsEnabled = true;
                CloseButton.IsEnabled = true;
                _isDownloading = false;

                MessageBox.Show(
                    $"{LocalizationManager.Get("Update_DownloadError")}:\n{ex.Message}",
                    LocalizationManager.Get("Msg_Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (_isDownloading)
            {
                try { _cts?.Cancel(); } catch {}
            }
            DialogResult = false;
            Close();
        }
    }
}