using logbook.Helpers;
using logbook.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace logbook.ViewModels
{
    public partial class MainViewModel
    {
        // ============================================================
        // ==================== ВР-80 =================================
        // ============================================================

        private void SaveVr80StartRecord()
        {
            _storage.SaveVr80StartRecord(SelectedProfile, Vr80StartRecord);

            RecalculateVr80Records();
            EnsureMonthlyTotalsVr80();
            RecalculateVr80MonthlyTotals();
            _storage.SaveVr80Records(SelectedProfile, Vr80Records.ToList());

            MessageBox.Show(
                LocalizationManager.Get("Msg_StartSavedVr80"),
                LocalizationManager.Get("Msg_Info"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void AddMonthlyTotalVr80(DateTime monthDate)
        {
            string totalLabel = BuildTotalLabel(monthDate);

            bool exists = Vr80Records.Any(r =>
            {
                if (!r.IsTotal) return false;
                int m = ParseMonthFromTotalName(r.Date);
                int y = ParseYearFromTotalName(r.Date);
                return m == monthDate.Month && y == monthDate.Year;
            });
            if (exists) return;

            var monthRecords = Vr80Records
                .Where(r =>
                {
                    if (r.IsTotal) return false;
                    if (!TryParseDate(r.Date, out var d)) return false;
                    return d.Month == monthDate.Month && d.Year == monthDate.Year;
                })
                .ToList();

            if (monthRecords.Count == 0) return;

            var total = BuildMonthlyTotalVr80(monthRecords, monthDate, totalLabel);
            InsertByDate(Vr80Records, total, r => r.Date, r => r.IsTotal);
        }



        private void EnsureMonthlyTotalsVr80()
        {
            NormalizeTotals(Vr80Records);
            var months = Vr80Records
                .Where(r => !r.IsTotal && !string.IsNullOrWhiteSpace(r.Date))
                .Select(r => TryParseDate(r.Date, out var d) ? (DateTime?)d : null)
                .Where(d => d.HasValue)
                .Select(d => new { Year = d.Value.Year, Month = d.Value.Month })
                .Distinct()
                .ToList();

            foreach (var m in months)
            {
                DateTime monthDate = new DateTime(m.Year, m.Month, 1);
                AddMonthlyTotalVr80(monthDate);
            }
        }


        private void RecalculateVr80MonthlyTotals()
        {
            // === 1. Обновляем значения итогов ===
            var totals = Vr80Records
                .Where(r => r.IsTotal)
                .ToList();

            foreach (var totalRec in totals)
            {
                int month = ParseMonthFromTotalName(totalRec.Date);
                int year = ParseYearFromTotalName(totalRec.Date);

                if (month == 0 || year == 0) continue;

                DateTime monthDate = new DateTime(year, month, 1);

                var monthRecords = Vr80Records
                    .Where(r =>
                    {
                        if (r.IsTotal) return false;
                        if (!TryParseDate(r.Date, out var d)) return false;
                        return d.Month == month && d.Year == year;
                    })
                    .ToList();

                var total = BuildMonthlyTotalVr80(monthRecords, monthDate, totalRec.Date);

                totalRec.LaunchCount = total.LaunchCount;
                totalRec.GroundHours = total.GroundHours;
                totalRec.Col6 = total.Col6;
                totalRec.Col7 = total.Col7;
                totalRec.Col24 = total.Col24;
                totalRec.ColSNE = total.ColSNE;
                totalRec.Col26 = total.Col26;
                totalRec.Col27 = total.Col27;
                totalRec.Col28 = total.Col28;
                totalRec.Col15 = total.Col15;
                totalRec.NeedsManualRecalc = total.NeedsManualRecalc;
            }

            // === 2. Пересобираем коллекцию в правильном порядке ===
            var sorted = Vr80Records
                .Where(r => !r.IsTotal)
                .OrderBy(r =>
                {
                    TryParseDate(r.Date, out var d);
                    return d;
                })
                .ToList();

            // Вставляем каждый итог после последней записи его месяца
            foreach (var total in totals)
            {
                int month = ParseMonthFromTotalName(total.Date);
                int year = ParseYearFromTotalName(total.Date);

                // Ищем последнюю регулярную запись этого месяца (пропускаем итоги)
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
            Vr80Records.Clear();
            foreach (var r in sorted)
                Vr80Records.Add(r);
        }


        /// <summary>
        /// Строит итоговую запись за месяц для ВР-80.
        ///
        /// Если предыдущий месяц есть:
        ///   Сумма:   LaunchCount, GroundHours, Col6, Col7
        ///   Разница: Col24, ColSNE, Col26, Col27
        ///   Значение 0 → "—".
        ///
        /// Если предыдущего месяца нет:
        ///   Сумма:   LaunchCount, GroundHours, Col6, Col7
        ///   ColSNE/Col24 — ПУСТО (нельзя сложить из-за ограничения 20%)
        ///   Col26/Col27 — ПУСТО
        ///   Строка помечается NeedsManualRecalc = true.
        /// </summary>
        private Vr80Record BuildMonthlyTotalVr80(
    List<Vr80Record> monthRecords,
    DateTime monthDate,
    string totalLabel)
        {
            var empty = new Vr80Record
            {
                IsTotal = true,
                Date = totalLabel,
                LaunchCount = "",
                GroundHours = "",
                Col4 = "",
                Col5 = "",
                Col6 = "",
                Col7 = "",
                Col8 = "",
                Col9 = "",
                Col10 = "",
                Col24 = "",
                ColSNE = "",
                Col26 = "",
                Col27 = "",
                Col28 = "",
                Col15 = "",
                NeedsManualRecalc = false
            };

            if (monthRecords.Count == 0)
                return empty;

            DateTime startOfCurrentMonth = new DateTime(monthDate.Year, monthDate.Month, 1);

            var prevMonthRecords = Vr80Records
                .Where(r =>
                {
                    if (r.IsTotal) return false;
                    if (!TryParseDate(r.Date, out var d)) return false;
                    return new DateTime(d.Year, d.Month, 1) < startOfCurrentMonth;
                })
                .ToList();

            // Объявляем переменные один раз
            int sumLaunch = 0;
            int sumGround = 0;
            int sumTp = 0;
            int sumAir = 0;

            int diffCol24 = 0;
            int diffSNE = 0;
            int diffCol26 = 0;
            int diffCol27 = 0;

            bool needsManualRecalc = false;

            // Если предыдущего месяца нет — только суммы, СНЭ/ППР пустые
            if (prevMonthRecords.Count == 0)
            {
                sumLaunch = monthRecords.Sum(r => ParseInt(r.LaunchCount));
                sumGround = monthRecords.Sum(r => ParseHM(r.GroundHours));
                sumTp = monthRecords.Sum(r => ParseHM(r.Col6));
                sumAir = monthRecords.Sum(r => ParseHM(r.Col7));

                needsManualRecalc = true;
            }
            else
            {
                // Есть предыдущий месяц — считаем суммы и разницы
                sumLaunch = monthRecords.Sum(r => ParseInt(r.LaunchCount));
                sumGround = monthRecords.Sum(r => ParseHM(r.GroundHours));
                sumTp = monthRecords.Sum(r => ParseHM(r.Col6));
                sumAir = monthRecords.Sum(r => ParseHM(r.Col7));

                var lastCurrentRec = monthRecords
                    .OrderBy(r => { TryParseDate(r.Date, out var d); return d; })
                    .LastOrDefault();

                var lastPrevRec = prevMonthRecords
                    .Where(r =>
                    {
                        TryParseDate(r.Date, out var d);
                        return d.Year == monthDate.Year && d.Month == monthDate.Month - 1 ||
                               (monthDate.Month == 1 && d.Year == monthDate.Year - 1 && d.Month == 12);
                    })
                    .OrderBy(r => { TryParseDate(r.Date, out var d); return d; })
                    .LastOrDefault();

                if (lastCurrentRec != null && lastPrevRec != null)
                {
                    diffCol24 = GetDiffHM(lastCurrentRec.Col24, lastPrevRec.Col24);
                    diffSNE = GetDiffHM(lastCurrentRec.ColSNE, lastPrevRec.ColSNE);
                    diffCol26 = GetDiffHM(lastCurrentRec.Col26, lastPrevRec.Col26);
                    diffCol27 = GetDiffHM(lastCurrentRec.Col27, lastPrevRec.Col27);
                }
                else
                {
                    // Не удалось найти крайние записи — помечаем как требующий ручного пересчёта
                    needsManualRecalc = true;
                }
            }

            return new Vr80Record
            {
                IsTotal = true,
                Date = totalLabel,
                NeedsManualRecalc = needsManualRecalc,

                LaunchCount = sumLaunch > 0 ? sumLaunch.ToString() : "—",
                GroundHours = sumGround > 0 ? MinutesToHM(sumGround) : "—",
                Col6 = sumTp > 0 ? MinutesToHM(sumTp) : "—",
                Col7 = sumAir > 0 ? MinutesToHM(sumAir) : "—",

                Col4 = "",
                Col5 = "",
                Col8 = "",
                Col9 = "",
                Col10 = "",
                Col28 = "",
                Col15 = "",

                Col24 = diffCol24 > 0 ? MinutesToHM(diffCol24) : "—",
                ColSNE = diffSNE > 0 ? MinutesToHM(diffSNE) : "—",
                Col26 = diffCol26 > 0 ? MinutesToHM(diffCol26) : "—",
                Col27 = diffCol27 > 0 ? MinutesToHM(diffCol27) : "—"
            };
        }

        private int GetDiffHM(string curr, string prev)
        {
            if (string.IsNullOrWhiteSpace(curr) || string.IsNullOrWhiteSpace(prev))
                return 0;

            int c = ParseHM(curr);
            int p = ParseHM(prev);
            int diff = c - p;

            return diff > 0 ? diff : 0;
        }


        /// <summary>
        /// Пересчёт ВР-80.
        ///
        /// Col27 = prev + ΔТП; при нуле — пусто.
        /// </summary>
        private void RecalculateVr80Records()
        {
            int prevSNEMin = ParseHM(Vr80StartRecord.ColSNE);
            int prevCol27Min = ParseHM(Vr80StartRecord.Col27);

            bool col24Active = !string.IsNullOrWhiteSpace(Vr80StartRecord.Col24);
            bool col26Active = !string.IsNullOrWhiteSpace(Vr80StartRecord.Col26);

            int prevCol24Min = col24Active ? ParseHM(Vr80StartRecord.Col24) : 0;
            int prevCol26Min = col26Active ? ParseHM(Vr80StartRecord.Col26) : 0;

            foreach (var rec in Vr80Records)
            {
                if (rec.IsTotal)
                    continue;

                DateTime recDate;
                if (!TryParseDate(rec.Date, out recDate))
                    continue;

                string dateKeyFull = recDate.ToString("dd.MM.yyyy");
                var dayData = _storage.GetDayData(SelectedProfile, dateKeyFull);

                int deltaGR;
                int deltaTP;

                if (dayData != null && dayData.Launches != null)
                {
                    int sumZemlya = 0;
                    int sumVozduh = 0;
                    int launchCount = 0;
                    int sumTP = 0;

                    foreach (var l in dayData.Launches)
                    {
                        sumZemlya += l.Zemlya;
                        sumVozduh += l.Vozduh;
                        sumTP += l.TP;

                        if (l.Zemlya > 0 || l.TP > 0)
                            launchCount++;
                    }

                    rec.LaunchCount = launchCount > 0 ? launchCount.ToString() : "";
                    rec.GroundHours = sumZemlya > 0 ? MinutesToHM(sumZemlya) : "";
                    rec.Col6 = sumTP > 0 ? MinutesToHM(sumTP) : "";
                    rec.Col7 = sumVozduh > 0 ? MinutesToHM(sumVozduh) : "";

                    deltaGR = (int)Math.Round(sumZemlya * 0.2) + sumVozduh;
                    deltaTP = sumTP;
                }
                else
                {
                    int recZemlya = ParseHM(rec.GroundHours);
                    int recVozduh = ParseHM(rec.Col7);
                    int recTP = ParseHM(rec.Col6);

                    deltaGR = (int)Math.Round(recZemlya * 0.2) + recVozduh;
                    deltaTP = recTP;
                }

                // ===== ColSNE — накопительное (0 → 0:00) =====
                prevSNEMin += deltaGR;
                rec.ColSNE = MinutesToHM(prevSNEMin);

                // ===== Col24 — накопительное ППР =====
                if (col24Active)
                {
                    prevCol24Min += deltaGR;
                    rec.Col24 = MinutesToHM(prevCol24Min);
                }
                else rec.Col24 = "";

                // ===== Col27 — накопительное (0 → пусто) =====
                prevCol27Min += deltaTP;
                rec.Col27 = prevCol27Min > 0 ? MinutesToHM(prevCol27Min) : "";

                // ===== Col26 — накопительное ППР =====
                if (col26Active)
                {
                    prevCol26Min += deltaTP;
                    rec.Col26 = MinutesToHM(prevCol26Min);
                }
                else rec.Col26 = "";
            }
        }

        private void LoadVr80Records()
        {
            Vr80Records.Clear();

            if (string.IsNullOrWhiteSpace(SelectedProfile))
                return;

            var records = _storage.GetVr80Records(SelectedProfile);
            if (records != null)
            {
                foreach (var rec in records)
                {
                    // Миграция: старый файл без IsTotal — определяем по дате
                    if (!rec.IsTotal && logbook.Services.MonthlyTotals.TryMonth(rec.Date, out _))
                        rec.IsTotal = true;

                    Vr80Records.Add(rec);
                }
            }

            NormalizeTotals(Vr80Records);

            Vr80StartRecord = _storage.GetVr80StartRecord(SelectedProfile)
                              ?? new Vr80Record();

            if (!IsExchangeProfile) RecalculateVr80Records();
            if (!IsExchangeProfile) EnsureMonthlyTotalsVr80();
            if (!IsExchangeProfile) RecalculateVr80MonthlyTotals();
            SubscribeItems(Vr80Records);

            IsDirty = false;
        }

        private void RemoveVr80Record(Vr80Record record)
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
                Vr80Records.Remove(record);
                RecalculateVr80Records();
                RecalculateVr80MonthlyTotals();
                _storage.SaveVr80Records(SelectedProfile, Vr80Records.ToList());
            }
            else
            {
                RemoveDateFromAllFormulars(record.Date);
            }
        }
    }
}