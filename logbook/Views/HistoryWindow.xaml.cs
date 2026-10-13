using System;
using System.Text;
using System.Windows;
using logbook.Helpers;
using logbook.Services;

namespace logbook.Views
{
    public partial class HistoryWindow : Window
    {
        private readonly StorageService _storage;
        private readonly string _profile;
        private readonly DateTime _endDate;

        public HistoryWindow(StorageService storage, string profile, DateTime endDate)
        {
            InitializeComponent();

            Loaded += (s, e) => logbook.Helpers.DarkTitleBarHelper.Apply(this);

            _storage = storage;
            _profile = profile;
            _endDate = endDate;
            LoadHistory();
        }

        private void LoadHistory()
        {
            StringBuilder sb = new StringBuilder();

            var allDays = _storage.GetAllDays(_profile);

            if (allDays.Count == 0)
            {
                ClearHistoryButton.Visibility = Visibility.Collapsed;
                HistoryText.Text = LocalizationManager.Get("History_NoDays");
                return;
            }

            string flightWord = LocalizationManager.Get("History_Flight");
            string testWord = LocalizationManager.Get("History_Test");
            string tpWord = LocalizationManager.Get("History_TP");

            bool anyDayWritten = false;

            foreach (var dayData in allDays)
            {
                if (dayData.Launches == null || dayData.Launches.Count == 0) continue;

                // Оставляем только реальные непустые запуски (где было ненулевое время работы)
                var validLaunches = dayData.Launches
                    .Where(l => l.Vsu > 0 || l.Zemlya > 0 || l.Vozduh > 0 || l.TP > 0 || l.SamoletLandings > 0)
                    .ToList();

                // Если после фильтра ничего не осталось — день пропускаем
                if (validLaunches.Count == 0) continue;

                string header = dayData.Date;
                if (DateTime.TryParseExact(dayData.Date, "dd.MM.yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var dt))
                {
                    header = dt.ToString("dd.MM.yy");
                }

                sb.AppendLine(header);

                foreach (var launch in validLaunches)
                {
                    bool hasAir = launch.Vozduh > 0;
                    bool hasGround = launch.Zemlya > 0;
                    bool hasTP = launch.TP > 0;

                    string type = hasAir ? flightWord : testWord;

                    string zemlyaStr = hasGround ? launch.Zemlya.ToString() : "";
                    string vozduhStr = hasAir ? launch.Vozduh.ToString() : "";

                    if (hasAir)
                    {
                        sb.AppendLine($"     {type} {launch.Vsu}/ {zemlyaStr}/ {vozduhStr}");
                    }
                    else if (hasGround)
                    {
                        sb.AppendLine($"     {type} {launch.Vsu}/ {zemlyaStr}/");
                    }
                    else if (hasTP)
                    {
                        sb.AppendLine($"     {type} {launch.Vsu}/ {tpWord}:{launch.TP}");
                    }
                    else
                    {
                        sb.AppendLine($"     {type} {launch.Vsu}/");
                    }
                }

                sb.AppendLine();
                anyDayWritten = true;
            }

            if (!anyDayWritten)
            {
                ClearHistoryButton.Visibility = Visibility.Collapsed;
                HistoryText.Text = LocalizationManager.Get("History_NoDays");
                return;
            }

            ClearHistoryButton.Visibility = Visibility.Visible;
            HistoryText.Text = sb.ToString();
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                LocalizationManager.Get("History_ClearConfirm"),
                LocalizationManager.Get("History_Title"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _storage.ClearDaysHistory(_profile);
                LoadHistory();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}