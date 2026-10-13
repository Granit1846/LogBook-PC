using logbook.Helpers;
using logbook.Models;
using logbook.Services;
using System;
using System.Linq;
using System.Windows;

namespace logbook.ViewModels
{
    public partial class MainViewModel
    {
        // ============================================================
        // ==================== БАЗОВОЕ СОХРАНЕНИЕ ====================
        // ============================================================

        private void Save()
        {
            string dateKey = SelectedDate.ToString("dd.MM.yyyy");

            var realLaunches = Launches
                .Where(l => l.Vsu > 0 || l.Zemlya > 0 || l.Vozduh > 0 || l.TP > 0 || l.SamoletLandings > 0)
                .Select(l => new Launch
                {
                    ExchangeId = l.ExchangeId,
                    Vsu = l.Vsu,
                    Zemlya = l.Zemlya,
                    Vozduh = l.Vozduh,
                    TP = l.TP,
                    SamoletLandings = l.SamoletLandings
                }).ToList();

            bool hasRealLaunches = realLaunches.Count > 0;
            bool hasCrew = !string.IsNullOrWhiteSpace(Crew);
            bool hasSnk = SnkRecords.Any(r => ExchangeCodec.HasSnkValue(r.LeftValue) || ExchangeCodec.HasSnkValue(r.RightValue));
            bool hasInput = hasCrew || hasRealLaunches || hasSnk;

            if (hasInput)
            {
                var dayData = new DayData
                {
                    Date = dateKey,
                    Profile = SelectedProfile,
                    Crew = Crew,
                    Launches = realLaunches,
                    SnkValues = SnkRecords.Select(r => new SnkValue
                    {
                        Label = r.Label,
                        LeftValue = ExchangeCodec.CleanSnk(r.LeftValue, r.Label),
                        RightValue = ExchangeCodec.CleanSnk(r.RightValue, r.Label)
                    }).ToList()
                };
                _storage.SaveDayData(dayData);
            }
            else
            {
                // Пустой день не должен сохраняться как день с фиктивными запусками
                _storage.DeleteDayData(SelectedProfile, dateKey);
            }
        }

        // ============================================================
        // ==================== ПОЛНОЕ СОХРАНЕНИЕ (Ctrl+S) ============
        // ============================================================

        private void SaveAll()
        {
            try
            {
                Save();

                _storage.SaveChast1StartRecord(SelectedProfile, Chast1StartRecord);
                _storage.SaveVr80StartRecord(SelectedProfile, Vr80StartRecord);
                _storage.SaveTa14StartRecord(SelectedProfile, Ta14StartRecord);
                _storage.SaveVk250001StartRecord(SelectedProfile, Vk250001StartRecord);
                _storage.SaveVk250002StartRecord(SelectedProfile, Vk250002StartRecord);

                _storage.SaveVk250001Coefficients(SelectedProfile, Vk250001CoeffA, Vk250001CoeffH);
                _storage.SaveVk250002Coefficients(SelectedProfile, Vk250002CoeffA, Vk250002CoeffH);

                _storage.SaveChast1Records(SelectedProfile, Chast1Records.ToList());
                _storage.SaveVr80Records(SelectedProfile, Vr80Records.ToList());
                _storage.SaveTa14Records(SelectedProfile, Ta14Records.ToList());
                _storage.SaveVk250001Records(SelectedProfile, Vk250001Records.ToList());
                _storage.SaveVk250002Records(SelectedProfile, Vk250002Records.ToList());

                RecalculateResources();
                _storage.SaveResourceRecords(SelectedProfile, ResourceRecords.ToList());
                _storage.SaveOilRecords(SelectedProfile, OilRecords.ToList());

                IsDirty = false;      // <-- ДОБАВЛЕНО

                MessageBox.Show(
                    LocalizationManager.Get("Msg_AllSaved"),
                    LocalizationManager.Get("Msg_Save"),
                    MessageBoxButton.OK, MessageBoxImage.Information);

                UpdateLastSaveStatus();
                UpdateDaysInProfile();
                UpdateNotifications();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationManager.Get("Msg_SaveError")}\n{ex.Message}",
                    LocalizationManager.Get("Msg_Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Сохранение без MessageBox — для закрытия окна.
        /// </summary>
        public bool SaveAllSilent(bool recomputeResources = true)
        {
            try
            {
                Save();

                _storage.SaveChast1StartRecord(SelectedProfile, Chast1StartRecord);
                _storage.SaveVr80StartRecord(SelectedProfile, Vr80StartRecord);
                _storage.SaveTa14StartRecord(SelectedProfile, Ta14StartRecord);
                _storage.SaveVk250001StartRecord(SelectedProfile, Vk250001StartRecord);
                _storage.SaveVk250002StartRecord(SelectedProfile, Vk250002StartRecord);

                _storage.SaveVk250001Coefficients(SelectedProfile, Vk250001CoeffA, Vk250001CoeffH);
                _storage.SaveVk250002Coefficients(SelectedProfile, Vk250002CoeffA, Vk250002CoeffH);

                _storage.SaveChast1Records(SelectedProfile, Chast1Records.ToList());
                _storage.SaveVr80Records(SelectedProfile, Vr80Records.ToList());
                _storage.SaveTa14Records(SelectedProfile, Ta14Records.ToList());
                _storage.SaveVk250001Records(SelectedProfile, Vk250001Records.ToList());
                _storage.SaveVk250002Records(SelectedProfile, Vk250002Records.ToList());

                if (recomputeResources && (!IsExchangeProfile || IsDirty)) RecalculateResources();
                _storage.SaveResourceRecords(SelectedProfile, ResourceRecords.ToList());
                _storage.SaveOilRecords(SelectedProfile, OilRecords.ToList());

                IsDirty = false;      // <-- ДОБАВЛЕНО

                UpdateLastSaveStatus();
                UpdateDaysInProfile();
                UpdateNotifications();
                return true;
            }
            catch
            {
                return false; // Синхронизация проверяет результат сохранения
            }
        }

        public void RefreshLastSaveStatus()
        {
            OnPropertyChanged(nameof(LastSaveStatus));
        }

        // ============================================================
        // ==================== АВТОСОХРАНЕНИЕ ========================
        // ============================================================

        private void AutoSaveTick()
        {
            if (_exchangeOpen || (IsExchangeProfile && !IsDirty)) return;
            if (!_settingsService.Settings.AutoSave)
                return;

            try
            {
                SaveAllSilent();
                UpdateLastSaveStatus();
                UpdateNotifications();
            }
            catch
            {
                // Игнорируем ошибки автосохранения
            }
        }

        public void StopAutoSave()
        {
            _autoSaveTimer?.Stop();
        }

        public void StopRecalcTimer()
        {
            _recalcTimer?.Stop();
        }

        // ============================================================
        // ==================== СТАТУС СОХРАНЕНИЯ =====================
        // ============================================================

        public void UpdateLastSaveStatus()
        {
            LastSaveStatus = LocalizationManager.Get("Main_Saved")
                 + DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        }
    }
}