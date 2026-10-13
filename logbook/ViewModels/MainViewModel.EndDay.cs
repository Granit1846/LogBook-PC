using logbook.Helpers;
using logbook.Models;
using System;
using System.Linq;
using System.Windows;

namespace logbook.ViewModels
{
    public partial class MainViewModel
    {
        // ============================================================
        // ==================== ЗАКОНЧИТЬ ДЕНЬ ========================
        // ============================================================

        private void EndDay()
        {
            DateTime recordDate = SelectedDate;
            Save();

            int totalZemlya = Launches.Sum(l => l.Zemlya);
            int totalVozduh = Launches.Sum(l => l.Vozduh);
            int totalVsu = Launches.Sum(l => l.Vsu);
            int totalTP = Launches.Sum(l => l.TP);

            // ===== Счётчики =====
            int chast1ZemlyaLaunches = Launches.Count(l => l.Zemlya > 0);
            int chast1Vertolet = Launches.Count(l => l.Vozduh > 0);
            int chast1Samolet = Launches.Sum(l => l.SamoletLandings);
            int chast1Landings = chast1Vertolet + chast1Samolet;

            int vr80Launches = Launches.Count(l => l.Zemlya > 0);
            int ta14Launches = Launches.Count(l => l.Vsu > 0);
            int vkLaunches = Launches.Count(l => l.Zemlya > 0);

            int groundMinutes = (int)Math.Round(totalZemlya * 0.2);
            int airMinutes = totalVozduh;

            var tomorrow = recordDate.AddDays(1);
            bool monthClosing = recordDate.Month != tomorrow.Month
                             || recordDate.Year != tomorrow.Year;

            string dateKeyShort = recordDate.ToString("dd.MM.yy");
            string dateKeyLong = recordDate.ToString("dd.MM.yyyy");

            if (IsFlightMode)
            {
                // ============================================================
                // ==================== ЧАСТЬ 1 ===============================
                // ============================================================
                var existingChast1 = Chast1Records
                    .FirstOrDefault(r => r.Date == dateKeyShort);

                if (chast1ZemlyaLaunches > 0)
                {
                    string airHM = airMinutes > 0 ? MinutesToHM(airMinutes) : "";

                    // Дневные поля для посадок
                    string col8 = chast1Samolet > 0 ? chast1Samolet.ToString() : "";   // По самолётному
                    string col9 = chast1Vertolet > 0 ? chast1Vertolet.ToString() : ""; // По вертолётному

                    if (existingChast1 == null)
                    {
                        var newRec = new Chast1Record
                        {
                            Date = dateKeyShort,
                            GroundHours = MinutesToHM(groundMinutes),
                            FlightDuration = airHM,
                            Col8 = col8,
                            Col9 = col9,
                            Col11 = col8,        // СНЭ → По самолётному (за день)
                            Landings2 = col9,    // СНЭ → По вертолётному (за день)
                            Pilot = Crew
                        };
                        InsertByDate(Chast1Records, newRec, r => r.Date);
                    }
                    else
                    {
                        existingChast1.GroundHours = MinutesToHM(groundMinutes);
                        existingChast1.FlightDuration = airHM;
                        existingChast1.Col8 = col8;
                        existingChast1.Col9 = col9;
                        existingChast1.Col11 = col8;
                        existingChast1.Landings2 = col9;
                        existingChast1.Pilot = Crew;
                    }
                }
                else
                {
                    if (existingChast1 != null)
                        Chast1Records.Remove(existingChast1);
                }

                RecalculateChast1Records();
                EnsureMonthlyTotals();
                RefreshSNE();
                if (monthClosing) AddMonthlyTotal(recordDate);
                RecalculateMonthlyTotals();
                _storage.SaveChast1Records(SelectedProfile, Chast1Records.ToList());

                // ============================================================
                // ==================== ВР-80 =================================
                // ============================================================
                var existingVr80 = Vr80Records
                    .FirstOrDefault(r => r.Date == dateKeyShort);

                // Условие добавления: Земля > 0 ИЛИ ТП > 0
                bool hasVr80Data = Launches.Any(l => l.Zemlya > 0 || l.TP > 0);

                if (hasVr80Data)
                {
                    string vr80AirHM = totalVozduh > 0 ? MinutesToHM(totalVozduh) : "";

                    if (existingVr80 == null)
                    {
                        var newRec = new Vr80Record
                        {
                            Date = dateKeyShort,
                            GroundHours = MinutesToHM(totalZemlya),
                            Col7 = vr80AirHM
                        };
                        InsertByDate(Vr80Records, newRec, r => r.Date);
                    }
                    else
                    {
                        existingVr80.GroundHours = MinutesToHM(totalZemlya);
                        existingVr80.Col7 = vr80AirHM;
                    }
                }
                else
                {
                    if (existingVr80 != null)
                        Vr80Records.Remove(existingVr80);
                }

                RecalculateVr80Records();
                EnsureMonthlyTotalsVr80();
                if (monthClosing) AddMonthlyTotalVr80(recordDate);
                RecalculateVr80MonthlyTotals();
                _storage.SaveVr80Records(SelectedProfile, Vr80Records.ToList());

                // ============================================================
                // ==================== Та-14 =================================
                // ============================================================
                var existingTa14 = Ta14Records
                    .FirstOrDefault(r => r.Date == dateKeyLong);

                // Условие: ВСУ > 0
                bool hasTa14Data = Launches.Any(l => l.Vsu > 0);

                if (hasTa14Data)
                {
                    if (existingTa14 == null)
                    {
                        var newRec = new Ta14Record
                        {
                            Date = dateKeyLong
                        };
                        InsertByDate(Ta14Records, newRec, r => r.Date);
                    }
                }
                else
                {
                    if (existingTa14 != null)
                        Ta14Records.Remove(existingTa14);
                }

                RecalculateTa14Records();
                EnsureMonthlyTotalsTa14();
                if (monthClosing) AddMonthlyTotalTa14(recordDate);
                RecalculateTa14MonthlyTotals();
                _storage.SaveTa14Records(SelectedProfile, Ta14Records.ToList());

                // ============================================================
                // ==================== ВК-2500-01 / 02 =======================
                // ============================================================
                // На вкладке «Полёты» СНК не вводится — заполняем накопительно
                // от данных полётов (Земля×0.2 + Воздух).
                var emptySnkList = new System.Collections.Generic.List<string>();

                FillVkRecord(
                    Vk250001Records,
                    Vk250001StartRecord,
                    emptySnkList,
                    Vk250001CoeffA,
                    Vk250001CoeffH,
                    recordDate,
                    groundMinutes,
                    airMinutes,
                    vkLaunches);

                FillVkRecord(
                    Vk250002Records,
                    Vk250002StartRecord,
                    emptySnkList,
                    Vk250002CoeffA,
                    Vk250002CoeffH,
                    recordDate,
                    groundMinutes,
                    airMinutes,
                    vkLaunches);

                RecalculateVk250001Records();
                RecalculateVk250002Records();

                EnsureMonthlyTotalsVk2500(Vk250001Records);
                EnsureMonthlyTotalsVk2500(Vk250002Records);

                if (monthClosing)
                {
                    AddMonthlyTotalVk2500(Vk250001Records, recordDate);
                    AddMonthlyTotalVk2500(Vk250002Records, recordDate);
                }

                _storage.SaveVk250001Records(SelectedProfile, Vk250001Records.ToList());
                _storage.SaveVk250002Records(SelectedProfile, Vk250002Records.ToList());
            }
            else // IsSnkMode
            {
                // ============================================================
                // ==================== ВК-2500-01 / 02 =======================
                // ============================================================
                FillVkRecord(
                    Vk250001Records,
                    Vk250001StartRecord,
                    SnkRecords.Select(r => r.LeftValue).ToList(),
                    Vk250001CoeffA,
                    Vk250001CoeffH,
                    recordDate,
                    groundMinutes,
                    airMinutes,
                    vkLaunches);

                FillVkRecord(
                    Vk250002Records,
                    Vk250002StartRecord,
                    SnkRecords.Select(r => r.RightValue).ToList(),
                    Vk250002CoeffA,
                    Vk250002CoeffH,
                    recordDate,
                    groundMinutes,
                    airMinutes,
                    vkLaunches);

                EnsureMonthlyTotalsVk2500(Vk250001Records);
                EnsureMonthlyTotalsVk2500(Vk250002Records);
                if (monthClosing)
                {
                    AddMonthlyTotalVk2500(Vk250001Records, recordDate);
                    AddMonthlyTotalVk2500(Vk250002Records, recordDate);
                }

                _storage.SaveVk250001Records(SelectedProfile, Vk250001Records.ToList());
                _storage.SaveVk250002Records(SelectedProfile, Vk250002Records.ToList());
            }

            // === Очистка и сдвиг ===
            foreach (var l in Launches)
                l.PropertyChanged -= Launch_PropertyChanged;

            Launches.Clear();
            AddLaunchInternal();
            Crew = "";

            InitializeSnkRecords();

            _selectedDate = _selectedDate.AddDays(1);
            OnPropertyChanged(nameof(SelectedDate));
            LoadDayData();
            UpdateDaysInProfile();
            UpdateNotifications();
        }

        // ============================================================
        // ==================== УДАЛЕНИЕ ПО ДАТЕ ======================
        // ============================================================

        private void RemoveDateFromAllFormulars(string dateKey)
        {
            if (string.IsNullOrWhiteSpace(dateKey)) return;

            DateTime targetDate;
            if (!TryParseDate(dateKey, out targetDate))
                return;

            for (int i = Chast1Records.Count - 1; i >= 0; i--)
            {
                var r = Chast1Records[i];
                if (string.IsNullOrWhiteSpace(r.Date)) continue;
                if (r.IsTotal) continue;
                DateTime d;
                if (TryParseDate(r.Date, out d) && d.Date == targetDate.Date)
                    Chast1Records.RemoveAt(i);
            }
            RecalculateChast1Records();
            RecalculateMonthlyTotals();

            for (int i = Vr80Records.Count - 1; i >= 0; i--)
            {
                var r = Vr80Records[i];
                if (string.IsNullOrWhiteSpace(r.Date)) continue;
                if (r.IsTotal) continue;
                DateTime d;
                if (TryParseDate(r.Date, out d) && d.Date == targetDate.Date)
                    Vr80Records.RemoveAt(i);
            }
            RecalculateVr80Records();
            RecalculateVr80MonthlyTotals();

            for (int i = Ta14Records.Count - 1; i >= 0; i--)
            {
                var r = Ta14Records[i];
                if (string.IsNullOrWhiteSpace(r.Date)) continue;
                if (r.IsTotal) continue;
                DateTime d;
                if (TryParseDate(r.Date, out d) && d.Date == targetDate.Date)
                    Ta14Records.RemoveAt(i);
            }
            RecalculateTa14Records();
            RecalculateTa14MonthlyTotals();

            for (int i = Vk250001Records.Count - 1; i >= 0; i--)
            {
                var r = Vk250001Records[i];
                if (string.IsNullOrWhiteSpace(r.Date)) continue;
                if (r.IsTotal) continue;
                DateTime d;
                if (TryParseDate(r.Date, out d) && d.Date == targetDate.Date)
                    Vk250001Records.RemoveAt(i);
            }

            for (int i = Vk250002Records.Count - 1; i >= 0; i--)
            {
                var r = Vk250002Records[i];
                if (string.IsNullOrWhiteSpace(r.Date)) continue;
                if (r.IsTotal) continue;
                DateTime d;
                if (TryParseDate(r.Date, out d) && d.Date == targetDate.Date)
                    Vk250002Records.RemoveAt(i);
            }

            string dateKeyFull = targetDate.ToString("dd.MM.yyyy");
            _storage.DeleteDayData(SelectedProfile, dateKeyFull);

            _storage.SaveChast1Records(SelectedProfile, Chast1Records.ToList());
            _storage.SaveVr80Records(SelectedProfile, Vr80Records.ToList());
            _storage.SaveTa14Records(SelectedProfile, Ta14Records.ToList());
            _storage.SaveVk250001Records(SelectedProfile, Vk250001Records.ToList());
            _storage.SaveVk250002Records(SelectedProfile, Vk250002Records.ToList());
        }
    }
}