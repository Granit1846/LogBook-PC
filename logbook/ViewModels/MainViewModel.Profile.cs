using logbook.Helpers;
using logbook.Models;
using logbook.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace logbook.ViewModels
{
    public partial class MainViewModel
    {
        /// <summary>
        /// Устанавливает стартовый профиль из ProfileSelectWindow.
        /// </summary>
        public void SetInitialProfile(string profile)
        {
            if (string.IsNullOrWhiteSpace(profile)) return;

            if (Profiles.Contains(profile))
            {
                SelectedProfile = profile;
            }
        }

        /// <summary>
        /// Перезагружает список профилей (после удаления).
        /// </summary>
        public void RefreshProfiles()
        {
            Profiles.Clear();
            var saved = _storage.GetAllProfiles();
            foreach (var p in saved)
                Profiles.Add(p);

            if (Profiles.Count > 0 && !Profiles.Contains(SelectedProfile))
                SelectedProfile = Profiles[0];
        }

        private void AddProfile()
        {
            var dialog = new logbook.Views.AddProfileWindow
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            bool? result = dialog.ShowDialog();
            if (result != true) return;

            string profileName = dialog.ProfileName;
            string iconName = dialog.IconName;

            if (string.IsNullOrWhiteSpace(profileName)) return;

            bool created = _storage.CreateProfile(profileName, iconName);

            if (!created)
            {
                MessageBox.Show(
                    LocalizationManager.Format("Msg_ProfileExists", profileName),
                    LocalizationManager.Get("Msg_Attention"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Profiles.Contains(profileName))
                Profiles.Add(profileName);

            SelectedProfile = profileName;

            // Открыть окно настройки агрегатов для нового профиля
            // Откладываем на следующий цикл Dispatcher, чтобы SelectedProfile
            // успел примениться и MainWindow был загружен
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                new Action(() => CheckAndShowSetupAggregates()),
                System.Windows.Threading.DispatcherPriority.Background);
        }

        private void DeleteProfile()
        {
            if (Profiles.Count <= 1)
            {
                MessageBox.Show(
                    LocalizationManager.Get("Msg_DeleteLastProfile"),
                    LocalizationManager.Get("Msg_Attention"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string profileToDelete = SelectedProfile;

            if (string.IsNullOrWhiteSpace(profileToDelete))
            {
                MessageBox.Show(
                    LocalizationManager.Get("Msg_NoProfileSelected"),
                    LocalizationManager.Get("Msg_Attention"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"{LocalizationManager.Format("Msg_DeleteProfileConfirm", profileToDelete)}\n" +
                LocalizationManager.Get("Msg_DeleteProfileWarning"),
                LocalizationManager.Get("Msg_ConfirmDeletion"),
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            _storage.DeleteProfile(profileToDelete);
            Profiles.Remove(profileToDelete);

            if (Profiles.Count == 0)
            {
                SelectedProfile = null;
                return;
            }

            SelectedProfile = Profiles[0];
        }

        /// <summary>
        /// Обновляет количество уникальных дней в текущем профиле.
        /// </summary>
        public void UpdateDaysInProfile()
        {
            if (string.IsNullOrWhiteSpace(SelectedProfile))
            {
                DaysInProfile = 0;
                return;
            }

            var uniqueDates = Chast1Records
                .Where(r => !r.IsTotal && !string.IsNullOrWhiteSpace(r.Date))
                .Select(r => r.Date)
                .Distinct()
                .Count();

            DaysInProfile = uniqueDates;
        }

        private void LoadDayData()
        {
            if (string.IsNullOrWhiteSpace(SelectedProfile))
                return;

            string dateKey = SelectedDate.ToString("dd.MM.yyyy");
            var dayData = _storage.GetDayData(SelectedProfile, dateKey);

            foreach (var l in Launches)
                l.PropertyChanged -= Launch_PropertyChanged;

            Launches.Clear();

            if (dayData != null)
            {
                Crew = dayData.Crew ?? "";

                if (dayData.Launches != null && dayData.Launches.Count > 0)
                {
                    foreach (var launch in dayData.Launches)
                    {
                        var l = new Launch
                        {
                            ExchangeId = launch.ExchangeId,
                            Vsu = launch.Vsu,
                            Zemlya = launch.Zemlya,
                            Vozduh = launch.Vozduh,
                            TP = launch.TP,
                            SamoletLandings = launch.SamoletLandings
                        };
                        l.PropertyChanged += Launch_PropertyChanged;
                        Launches.Add(l);
                    }
                }
                else
                {
                    AddLaunchInternal();
                }
            }
            else
            {
                Crew = "";
                AddLaunchInternal();
            }

            // СНК
            InitializeSnkRecords();

            if (dayData != null && dayData.SnkValues != null && dayData.SnkValues.Count > 0)
            {
                foreach (var sv in dayData.SnkValues)
                {
                    var rec = SnkRecords.FirstOrDefault(r => r.Label == sv.Label);
                    if (rec != null)
                    {
                        rec.LeftValue = ExchangeCodec.CleanSnk(sv.LeftValue, sv.Label);
                        rec.RightValue = ExchangeCodec.CleanSnk(sv.RightValue, sv.Label);
                    }
                }
            }

            RecalculateTotals();
        }

        private void ChangeDay(int offset)
        {
            DateTime newDate = _selectedDate.AddDays(offset);

            if (newDate.Date > DateTime.Today)
            {
                MessageBox.Show(
                    LocalizationManager.Get("Msg_FutureDate"),
                    LocalizationManager.Get("Msg_Attention"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (newDate.Date < DateTime.Today.AddDays(-30))
            {
                MessageBox.Show(
                    LocalizationManager.Get("Msg_HistoryUnavailable"),
                    LocalizationManager.Get("Msg_Attention"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedDate = newDate;
        }

        private void ShowHistory()
        {
            var historyWindow = new logbook.Views.HistoryWindow(_storage, SelectedProfile, SelectedDate);
            historyWindow.Owner = Application.Current.MainWindow;
            historyWindow.ShowDialog();
        }

        private void RecalculateTotals()
        {
            TotalVsu = Launches.Sum(l => l.Vsu);
            TotalZemlya = Launches.Sum(l => l.Zemlya);
            TotalVozduh = Launches.Sum(l => l.Vozduh);
        }

        private void InitializeSnkRecords()
        {
            SnkRecords.Clear();
            foreach (var label in new[] { "A", "B", "D", "E", "G", "H", "I", "K" })
            {
                SnkRecords.Add(new SnkRecord { Label = label });
            }
        }

        // ============================================================
        // ==================== ОБРАБОТЧИКИ LAUNCHES ==================
        // ============================================================

        private void Launches_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Launch item in e.NewItems)
                    item.PropertyChanged += Launch_PropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (Launch item in e.OldItems)
                    item.PropertyChanged -= Launch_PropertyChanged;
            }

            RecalculateTotals();
        }

        private void Launch_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RecalculateTotals();
        }

        private void AddLaunchInternal()
        {
            var launch = new Launch();
            launch.PropertyChanged += Launch_PropertyChanged;
            Launches.Add(launch);
        }
    }
}