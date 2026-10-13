using logbook.Helpers;
using logbook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace logbook.ViewModels
{
    public partial class MainViewModel
    {
        // ============================================================
        // ==================== Та-14 =================================
        // ============================================================

        private void SaveTa14StartRecord()
        {
            _storage.SaveTa14StartRecord(SelectedProfile, Ta14StartRecord);

            RecalculateTa14Records();
            EnsureMonthlyTotalsTa14();
            RecalculateTa14MonthlyTotals();
            _storage.SaveTa14Records(SelectedProfile, Ta14Records.ToList());

            MessageBox.Show(
                LocalizationManager.Get("Msg_StartSavedTa14"),
                LocalizationManager.Get("Msg_Info"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddMonthlyTotalTa14(DateTime monthDate)
        {
            string totalLabel = BuildTotalLabel(monthDate);

            bool exists = Ta14Records.Any(r =>
            {
                if (!r.IsTotal) return false;
                int m = ParseMonthFromTotalName(r.Date);
                int y = ParseYearFromTotalName(r.Date);
                return m == monthDate.Month && y == monthDate.Year;
            });
            if (exists) return;

            var monthRecords = Ta14Records
                .Where(r =>
                {
                    if (r.IsTotal) return false;
                    if (!TryParseDate(r.Date, out var d)) return false;
                    return d.Month == monthDate.Month && d.Year == monthDate.Year;
                })
                .ToList();

            if (monthRecords.Count == 0) return;

            var total = BuildMonthlyTotalTa14(monthRecords, monthDate, totalLabel);
            InsertByDate(Ta14Records, total, r => r.Date, r => r.IsTotal);
        }


        private void EnsureMonthlyTotalsTa14()
        {
            NormalizeTotals(Ta14Records);
            var months = Ta14Records
                .Where(r => !r.IsTotal && !string.IsNullOrWhiteSpace(r.Date))
                .Select(r => TryParseDate(r.Date, out var d) ? (DateTime?)d : null)
                .Where(d => d.HasValue)
                .Select(d => new { Year = d.Value.Year, Month = d.Value.Month })
                .Distinct()
                .ToList();

            foreach (var m in months)
            {
                DateTime monthDate = new DateTime(m.Year, m.Month, 1);
                AddMonthlyTotalTa14(monthDate);
            }
        }


        private void RecalculateTa14MonthlyTotals()
        {
            // === 1. Обновляем значения итогов ===
            var totals = Ta14Records
                .Where(r => r.IsTotal)
                .ToList();

            foreach (var totalRec in totals)
            {
                int month = ParseMonthFromTotalName(totalRec.Date);
                int year = ParseYearFromTotalName(totalRec.Date);

                if (month == 0 || year == 0) continue;

                DateTime monthDate = new DateTime(year, month, 1);

                var monthRecords = Ta14Records
                    .Where(r =>
                    {
                        if (r.IsTotal) return false;
                        if (!TryParseDate(r.Date, out var d)) return false;
                        return d.Month == month && d.Year == year;
                    })
                    .ToList();

                var total = BuildMonthlyTotalTa14(monthRecords, monthDate, totalRec.Date);

                totalRec.LaunchCount = total.LaunchCount;
                totalRec.ColVremya = total.ColVremya;
                totalRec.ColSNE = total.ColSNE;
                totalRec.Col7 = total.Col7;
                totalRec.Col8 = total.Col8;
                totalRec.ColSNE2 = total.ColSNE2;
                totalRec.LaunchCount2 = total.LaunchCount2;
                totalRec.NeedsManualRecalc = total.NeedsManualRecalc;
            }

            // === 2. Пересобираем коллекцию в правильном порядке ===
            var sorted = Ta14Records
                .Where(r => !r.IsTotal)
                .OrderBy(r =>
                {
                    TryParseDate(r.Date, out var d);
                    return d;
                })
                .ToList();

            foreach (var total in totals)
            {
                int month = ParseMonthFromTotalName(total.Date);
                int year = ParseYearFromTotalName(total.Date);

                int insertIdx = -1;
                for (int i = sorted.Count - 1; i >= 0; i--)
                {
                    if (sorted[i].IsTotal) continue;

                    if (TryParseDate(sorted[i].Date, out var d)
                        && d.Month == month && d.Year == year)
                    {
                        insertIdx = i;
                        break;
                    }
                }

                if (insertIdx >= 0)
                    sorted.Insert(insertIdx + 1, total);
                else
                    sorted.Add(total);
            }

            // === 3. Заменяем содержимое коллекции ===
            Ta14Records.Clear();
            foreach (var r in sorted)
                Ta14Records.Add(r);
        }


        /// <summary>
        /// Строит итоговую запись за месяц для Та-14.
        ///
        /// Если предыдущий месяц есть:
        ///   Сумма:   LaunchCount, ColVremya
        ///   Разница: ColSNE, ColSNE2, Col7, Col8, LaunchCount2
        ///   Значение 0 → "—".
        ///
        /// Если предыдущего месяца нет:
        ///   LaunchCount  = сумма всех строк
        ///   ColVremya    = сумма всех строк
        ///   ColSNE       = ColVremya
        ///   ColSNE2      = ColVremya
        ///   LaunchCount2 = LaunchCount
        ///   Остальные (Col7, Col8) — пусто
        ///   Строка помечается NeedsManualRecalc = true.
        /// </summary>
        private Ta14Record BuildMonthlyTotalTa14(
    List<Ta14Record> monthRecords,
    DateTime monthDate,
    string totalLabel)
        {
            var empty = new Ta14Record
            {
                IsTotal = true,
                Date = totalLabel,
                LaunchCount = "",
                Col3 = "",
                ColVremya = "",
                Col5 = "",
                ColSNE = "",
                Col7 = "",
                Col8 = "",
                ColSNE2 = "",
                LaunchCount2 = "",
                Col11 = "",
                NeedsManualRecalc = false
            };

            if (monthRecords.Count == 0) return empty;

            DateTime startOfCurrentMonth = new DateTime(monthDate.Year, monthDate.Month, 1);

            var prevMonthRecords = Ta14Records
                .Where(r =>
                {
                    if (r.IsTotal) return false;
                    if (!TryParseDate(r.Date, out var d)) return false;
                    return new DateTime(d.Year, d.Month, 1) < startOfCurrentMonth;
                })
                .ToList();

            // Объявляем переменные один раз
            int sumLaunch = 0;
            int sumVremya = 0;

            int diffSNE = 0;
            int diffCol7 = 0;
            int diffSNE2 = 0;
            int diffCol8 = 0;
            int diffLaunchCount2 = 0;

            bool needsManualRecalc = false;

            // ===== Нет предыдущего месяца — сложение строк =====
            if (prevMonthRecords.Count == 0)
            {
                sumLaunch = monthRecords.Sum(r => ParseInt(r.LaunchCount));
                sumVremya = monthRecords.Sum(r => ParseHM(r.ColVremya));

                needsManualRecalc = true;

                return new Ta14Record
                {
                    IsTotal = true,
                    Date = totalLabel,
                    NeedsManualRecalc = needsManualRecalc,

                    LaunchCount = sumLaunch > 0 ? sumLaunch.ToString() : "—",
                    Col3 = "",
                    ColVremya = sumVremya > 0 ? MinutesToHM(sumVremya) : "—",
                    Col5 = "",

                    ColSNE = sumVremya > 0 ? MinutesToHM(sumVremya) : "—",
                    Col7 = "",
                    Col8 = "",
                    ColSNE2 = sumVremya > 0 ? MinutesToHM(sumVremya) : "—",
                    LaunchCount2 = sumLaunch > 0 ? sumLaunch.ToString() : "—",
                    Col11 = ""
                };
            }

            // ===== Есть предыдущий месяц — стандартная логика =====
            var lastPrevDate = prevMonthRecords
                .Select(r => { TryParseDate(r.Date, out var d); return d; })
                .Max();

            DateTime prevMonthDate = new DateTime(lastPrevDate.Year, lastPrevDate.Month, 1);

            var lastCurrentRec = monthRecords
                .OrderBy(r => { TryParseDate(r.Date, out var d); return d; })
                .LastOrDefault();

            var lastPrevRec = prevMonthRecords
                .Where(r =>
                {
                    TryParseDate(r.Date, out var d);
                    return d.Year == prevMonthDate.Year && d.Month == prevMonthDate.Month;
                })
                .OrderBy(r => { TryParseDate(r.Date, out var d); return d; })
                .LastOrDefault();

            if (lastCurrentRec == null || lastPrevRec == null)
            {
                empty.NeedsManualRecalc = true;
                return empty;
            }

            sumLaunch = monthRecords.Sum(r => ParseInt(r.LaunchCount));
            sumVremya = monthRecords.Sum(r => ParseHM(r.ColVremya));

            diffSNE = GetDiffHM(lastCurrentRec.ColSNE, lastPrevRec.ColSNE);
            diffCol7 = GetDiffHM(lastCurrentRec.Col7, lastPrevRec.Col7);
            diffSNE2 = GetDiffHM(lastCurrentRec.ColSNE2, lastPrevRec.ColSNE2);
            diffCol8 = GetDiffInt(lastCurrentRec.Col8, lastPrevRec.Col8);
            diffLaunchCount2 = GetDiffInt(lastCurrentRec.LaunchCount2, lastPrevRec.LaunchCount2);

            return new Ta14Record
            {
                IsTotal = true,
                Date = totalLabel,
                NeedsManualRecalc = false,

                LaunchCount = sumLaunch > 0 ? sumLaunch.ToString() : "—",
                Col3 = "",
                ColVremya = sumVremya > 0 ? MinutesToHM(sumVremya) : "—",
                Col5 = "",

                ColSNE = diffSNE > 0 ? MinutesToHM(diffSNE) : "—",
                Col7 = diffCol7 > 0 ? MinutesToHM(diffCol7) : "—",
                Col8 = diffCol8 > 0 ? diffCol8.ToString() : "—",
                ColSNE2 = diffSNE2 > 0 ? MinutesToHM(diffSNE2) : "—",
                LaunchCount2 = diffLaunchCount2 > 0 ? diffLaunchCount2.ToString() : "—",
                Col11 = ""
            };
        }


        private void RecalculateTa14Records()
        {
            int prevSNEMin = ParseHM(Ta14StartRecord.ColSNE);
            int prevSNE2Min = ParseHM(Ta14StartRecord.ColSNE2);
            int prevLaunch2 = ParseInt(Ta14StartRecord.LaunchCount2);

            bool col7Active = !string.IsNullOrWhiteSpace(Ta14StartRecord.Col7);
            bool col8Active = !string.IsNullOrWhiteSpace(Ta14StartRecord.Col8);

            int prevCol7Min = col7Active ? ParseHM(Ta14StartRecord.Col7) : 0;
            int prevCol8Int = col8Active ? ParseInt(Ta14StartRecord.Col8) : 0;

            foreach (var rec in Ta14Records)
            {
                if (rec.IsTotal)
                    continue;

                DateTime recDate;
                if (!TryParseDate(rec.Date, out recDate))
                    continue;

                string dateKeyFull = recDate.ToString("dd.MM.yyyy");
                var dayData = _storage.GetDayData(SelectedProfile, dateKeyFull);

                int deltaNarabotka;
                int deltaZapuski;

                if (dayData != null && dayData.Launches != null)
                {
                    int sumVsu = 0;
                    int launchCount = 0;

                    foreach (var l in dayData.Launches)
                    {
                        if (l.Vsu > 0)
                        {
                            sumVsu += l.Vsu;
                            launchCount++;
                        }
                    }

                    rec.LaunchCount = launchCount > 0 ? launchCount.ToString() : "";
                    rec.ColVremya = sumVsu > 0 ? MinutesToHM(sumVsu) : "";

                    deltaNarabotka = sumVsu;
                    deltaZapuski = launchCount;
                }
                else
                {
                    int recVremya = ParseHM(rec.ColVremya);
                    int recLaunch = ParseInt(rec.LaunchCount);

                    deltaNarabotka = recVremya;
                    deltaZapuski = recLaunch;
                }

                prevSNEMin += deltaNarabotka;
                rec.ColSNE = MinutesToHM(prevSNEMin);

                prevSNE2Min += deltaNarabotka;
                rec.ColSNE2 = MinutesToHM(prevSNE2Min);

                if (col7Active)
                {
                    prevCol7Min += deltaNarabotka;
                    rec.Col7 = MinutesToHM(prevCol7Min);
                }
                else rec.Col7 = "";

                if (col8Active)
                {
                    prevCol8Int += deltaZapuski;
                    rec.Col8 = prevCol8Int.ToString();
                }
                else rec.Col8 = "";

                prevLaunch2 += deltaZapuski;
                rec.LaunchCount2 = prevLaunch2.ToString();
            }
        }

        private void LoadTa14Records()
        {
            Ta14Records.Clear();

            if (string.IsNullOrWhiteSpace(SelectedProfile))
                return;

            var records = _storage.GetTa14Records(SelectedProfile);
            if (records != null)
            {
                foreach (var rec in records)
                {
                    // Миграция: старый файл без IsTotal — определяем по дате
                    if (!rec.IsTotal && logbook.Services.MonthlyTotals.TryMonth(rec.Date, out _))
                        rec.IsTotal = true;

                    Ta14Records.Add(rec);
                }
            }

            NormalizeTotals(Ta14Records);

            Ta14StartRecord = _storage.GetTa14StartRecord(SelectedProfile)
                              ?? new Ta14Record();

            if (!IsExchangeProfile) RecalculateTa14Records();
            if (!IsExchangeProfile) EnsureMonthlyTotalsTa14();
            if (!IsExchangeProfile) RecalculateTa14MonthlyTotals();
            SubscribeItems(Ta14Records);

            IsDirty = false;
        }

        private void RemoveTa14Record(Ta14Record record)
        {
            if (record == null) return;

            bool isTotal = record.IsTotal;

            string message = isTotal
                ? LocalizationManager.Format("Msg_DeleteTotal", record.Date)
                : $"{LocalizationManager.Format("Msg_DeleteRecord", record.Date)}\n\n" +
                  LocalizationManager.Get("Msg_DeleteAllFormulars");

            var result = MessageBox.Show(message,
                LocalizationManager.Get("Msg_ConfirmDeletion"),
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            TakeSnapshot();

            if (isTotal)
            {
                Ta14Records.Remove(record);
                RecalculateTa14Records();
                RecalculateTa14MonthlyTotals();
                _storage.SaveTa14Records(SelectedProfile, Ta14Records.ToList());
            }
            else
            {
                RemoveDateFromAllFormulars(record.Date);
            }
        }
    }
}