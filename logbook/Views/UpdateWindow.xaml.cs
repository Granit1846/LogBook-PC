using System;
using System.Windows;
using logbook.Helpers;

namespace logbook.Views
{
    public partial class UpdateWindow : Window
    {
        private readonly string _url;

        /// <summary>
        /// Окно «Обновлений нет» — одна кнопка «ОК».
        /// </summary>
        public UpdateWindow(string currentVersion)
        {
            InitializeComponent();

            _url = null;

            Loaded += (s, e) => DarkTitleBarHelper.Apply(this);

            TitleText.Text = LocalizationManager.Get("Update_NoUpdatesTitle");
            CurrentVersionRun.Text = currentVersion;

            // Скрываем блок «новая версия», notes, «Скачать»
            NewVersionBlock.Visibility = Visibility.Collapsed;
            WhatNewLabel.Visibility = Visibility.Collapsed;
            NotesTextBox.Visibility = Visibility.Collapsed;
            DownloadButton.Visibility = Visibility.Collapsed;

            CloseButton.Content = LocalizationManager.Get("Btn_OK");
        }

        /// <summary>
        /// Окно «Доступно обновление» — две кнопки.
        /// </summary>
        public UpdateWindow(string currentVersion, string newVersion, string notes, string url)
        {
            InitializeComponent();

            _url = url;

            Loaded += (s, e) => DarkTitleBarHelper.Apply(this);

            TitleText.Text = LocalizationManager.Get("Update_NewVersionTitle");
            CurrentVersionRun.Text = currentVersion;
            NewVersionRun.Text = newVersion;

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
                // Нечего скачивать — прячем кнопку
                DownloadButton.Visibility = Visibility.Collapsed;
            }
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_url))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _url,
                        UseShellExecute = true
                    });
                }
            }
            catch
            {
                // Молча игнорируем
            }

            DialogResult = true;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}