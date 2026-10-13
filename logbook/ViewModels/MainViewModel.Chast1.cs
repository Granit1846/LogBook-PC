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
        // ==================== ЧАСТЬ 1 ===============================
        // ============================================================

        private void SaveStartRecord()
        {
            _storage.SaveChast1StartRecord(SelectedProfile, Chast1StartRecord);

            RecalculateChast1Records();
            EnsureMonthlyTotals();
            RefreshSNE();
            RecalculateMonthlyTotals();
            _storage.SaveChast1Records(SelectedProfile, Chast1Records.ToList());

            MessageBox.Show(
                LocalizationManager.Get("Msg_StartSavedChast1"),
                LocalizationManager.Get("Msg_Info"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddMonthlyTotal(DateTime monthDate)
        {
            string totalLabel = BuildTotalLabel(monthDate);

            bool exists = Chast1Records.Any(r =>
            {
                if (!r.IsTotal) return false;
                int m = ParseMonthFromTotalName(r.Date);
                int y = ParseYearFromTotalName(r.Date);
                return m == monthDate.Month && y == monthDate.Year;
            });
            if (exists) return;

            var monthRecords = Chast1Records
                .Where(r =>
                {
                    if (r.IsTotal) return false;
                    if (!TryParseDate(r.Date, out var d)) return false;
                    return d.Month == monthDate.Month && d.Year == monthDate.Year;
                })
                .ToList();

            if (monthRecords.Count == 0) return;

            var total = BuildMonthlyTotal(monthRecords, monthDate, totalLabel);
            InsertByDate(Chast1Records, total, r => r.Date, r => r.IsTotal);
        }


        private void EnsureMonthlyTotals()
        {
            NormalizeTotals(Chast1Records);
            var months = Chast1Records
                .Where(r => !r.IsTotal && !string.IsNullOrWhiteSpace(r.Date))
                .Select(r => TryParseDate(r.Date, out var d) ? (DateTime?)d : null)
                .Where(d => d.HasValue)
                .Select(d => new { Year = d.Value.Year, Month = d.Value.Month })
                .Distinct()
                .ToList();

            foreach (var m in months)
            {
                DateTime monthDate = new DateTime(m.Year, m.Month, 1);
                AddMonthlyTotal(monthDate);
            }
        }

        private void RecalculateMonthlyTotals()
        {
            // === 1. Обновляем значения итогов ===
            var totals = Chast1Records
                .Where(r => r.IsTotal)
                .ToList();

            foreach (var totalRec in totals)
            {
                int month = ParseMonthFromTotalName(totalRec.Date);
                int year = ParseYearFromTotalName(totalRec.Date);

                if (month == 0 || year == 0) continue;

                DateTime monthDate = new DateTime(year, month, 1);

                var monthRecords = Chast1Records
                    .Where(r =>
                    {
                        if (r.IsTotal) return false;
                        if (!TryParseDate(r.Date, out var d)) return false;
                        return d.Month == month && d.Year == year;
                    })
                    .ToList();

                var total = BuildMonthlyTotal(monthRecords, monthDate, totalRec.Date);

                totalRec.GroundHours = total.GroundHours;
                totalRec.FlightDuration = total.FlightDuration;
                totalRec.Col5 = total.Col5;
                totalRec.ColSNE = total.ColSNE;
                totalRec.Col7 = total.Col7;
                totalRec.Col8 = total.Col8;
                totalRec.Col9 = total.Col9;
                totalRec.Landings = total.Landings;
                totalRec.Col11 = total.Col11;
                totalRec.Landings2 = total.Landings2;
                totalRec.NeedsManualRecalc = total.NeedsManualRecalc;
            }

            // === 2. Пересобираем коллекцию в правильном порядке ===
            var sorted = Chast1Records
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
            Chast1Records.Clear();
            foreach (var r in sorted)
                Chast1Records.Add(r);
        }

        /// <summary>
        /// Строит итоговую запись за месяц.
        ///
        /// Если предыдущий месяц есть:
        ///   накопительные (Col5, ColSNE, Col7, Landings) — РАЗНИЦА крайних;
        ///   за день (GroundHours, FlightDuration, Col8, Col9, Col11, Landings2) — СУММА.
        ///   Значение 0 → "—".
        ///
        /// Если предыдущего месяца нет:
        ///   считаем сложением строк по формулам:
        ///     земля  = сумма всех строк (GroundHours)
        ///     воздух = сумма всех строк (FlightDuration)
        ///     СНЭ    = сумма земли + сумма воздуха
        ///     ППР    = сумма земли + сумма воздуха
        ///     посадки по самолётному (ППР/СНЭ)  = сумма всех строк
        ///     посадки по вертолётному (ППР/СНЭ) = сумма всех строк
        ///   строка помечается NeedsManualRecalc = true.
        /// </summary>
        private Chast1Record BuildMonthlyTotal(
    List<Chast1Record> monthRecords,
    DateTime monthDate,
    string totalLabel)
        {
            var empty = new Chast1Record
            {
                IsTotal = true,
                Date = totalLabel,
                FlightType = "",
                GroundHours = "",
                FlightDuration = "",
                Col5 = "",
                ColSNE = "",
                Col7 = "",
                Col8 = "",
                Col9 = "",
                Landings = "",
                Col11 = "",
                Landings2 = "",
                Pilot = "",
                Mechanic = "",
                Col15 = "",
                NeedsManualRecalc = false
            };

            if (monthRecords.Count == 0) return empty;

            DateTime startOfCurrentMonth = new DateTime(monthDate.Year, monthDate.Month, 1);

            var prevMonthRecords = Chast1Records
                .Where(r =>
                {
                    if (r.IsTotal) return false;
                    if (!TryParseDate(r.Date, out var d)) return false;
                    return new DateTime(d.Year, d.Month, 1) < startOfCurrentMonth;
                })
                .ToList();

            // Объявляем переменные один раз
            int sumGround = 0;
            int sumAir = 0;
            int sumCol8 = 0;
            int sumCol9 = 0;
            int sumCol11 = 0;
            int sumLandings2 = 0;

            int diffCol5 = 0;
            int diffSNE = 0;
            int diffCol7 = 0;
            int diffLandings = 0;

            bool needsManualRecalc = false;

            bool col7Active = !string.IsNullOrWhiteSpace(Chast1StartRecord.Col7);
            bool col5Active = !string.IsNullOrWhiteSpace(Chast1StartRecord.Col5);

            // ===== Нет предыдущего месяца — сложение строк =====
            if (prevMonthRecords.Count == 0)
            {
                sumGround = monthRecords.Sum(r => ParseHM(r.GroundHours));
                sumAir = monthRecords.Sum(r => ParseHM(r.FlightDuration));
                sumCol8 = monthRecords.Sum(r => ParseInt(r.Col8));
                sumCol9 = monthRecords.Sum(r => ParseInt(r.Col9));
                sumCol11 = monthRecords.Sum(r => ParseInt(r.Col11));
                sumLandings2 = monthRecords.Sum(r => ParseInt(r.Landings2));

                needsManualRecalc = true;

                int sumSNE = sumGround + sumAir;

                return new Chast1Record
                {
                    IsTotal = true,
                    Date = totalLabel,
                    FlightType = "",
                    NeedsManualRecalc = needsManualRecalc,

                    GroundHours = sumGround > 0 ? MinutesToHM(sumGround) : "—",
                    FlightDuration = sumAir > 0 ? MinutesToHM(sumAir) : "—",

                    Col5 = col5Active ? (sumSNE > 0 ? MinutesToHM(sumSNE) : "—") : "",
                    ColSNE = sumSNE > 0 ? MinutesToHM(sumSNE) : "—",

                    Col7 = col7Active ? ((sumCol8 + sumCol9) > 0 ? (sumCol8 + sumCol9).ToString() : "—") : "",
                    Col8 = col7Active ? (sumCol8 > 0 ? sumCol8.ToString() : "—") : "",
                    Col9 = col7Active ? (sumCol9 > 0 ? sumCol9.ToString() : "—") : "",

                    Landings = (sumCol8 + sumCol9) > 0 ? (sumCol8 + sumCol9).ToString() : "—",
                    Col11 = sumCol11 > 0 ? sumCol11.ToString() : "",
                    Landings2 = sumLandings2 > 0 ? sumLandings2.ToString() : "",

                    Pilot = "",
                    Mechanic = "",
                    Col15 = ""
                };
            }

            // ===== Есть предыдущий месяц — стандартная логика =====
            var lastPrevDate = prevMonthRecords
                .Select(r =>
                {
                    TryParseDate(r.Date, out var d);
                    return d;
                })
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

            sumGround = monthRecords.Sum(r => ParseHM(r.GroundHours));
            sumAir = monthRecords.Sum(r => ParseHM(r.FlightDuration));
            sumCol8 = monthRecords.Sum(r => ParseInt(r.Col8));
            sumCol9 = monthRecords.Sum(r => ParseInt(r.Col9));
            sumCol11 = monthRecords.Sum(r => ParseInt(r.Col11));
            sumLandings2 = monthRecords.Sum(r => ParseInt(r.Landings2));

            diffCol5 = GetDiffHM(lastCurrentRec.Col5, lastPrevRec.Col5);
            diffSNE = GetDiffHM(lastCurrentRec.ColSNE, lastPrevRec.ColSNE);
            diffCol7 = GetDiffInt(lastCurrentRec.Col7, lastPrevRec.Col7);
            diffLandings = GetDiffInt(lastCurrentRec.Landings, lastPrevRec.Landings);

            return new Chast1Record
            {
                IsTotal = true,
                Date = totalLabel,
                FlightType = "",
                NeedsManualRecalc = false,

                GroundHours = sumGround > 0 ? MinutesToHM(sumGround) : "—",
                FlightDuration = sumAir > 0 ? MinutesToHM(sumAir) : "—",

                Col5 = col5Active ? (diffCol5 > 0 ? MinutesToHM(diffCol5) : "—") : "",
                ColSNE = diffSNE > 0 ? MinutesToHM(diffSNE) : "—",

                Col7 = col7Active ? (diffCol7 > 0 ? diffCol7.ToString() : "—") : "",
                Col8 = col7Active ? (sumCol8 > 0 ? sumCol8.ToString() : "—") : "",
                Col9 = col7Active ? (sumCol9 > 0 ? sumCol9.ToString() : "—") : "",

                Landings = diffLandings > 0 ? diffLandings.ToString() : "—",

                Col11 = sumCol11 > 0 ? sumCol11.ToString() : "",
                Landings2 = sumLandings2 > 0 ? sumLandings2.ToString() : "",

                Pilot = "",
                Mechanic = "",
                Col15 = ""
            };
        }

        private int GetDiffInt(string curr, string prev)
        {
            if (string.IsNullOrWhiteSpace(curr) || string.IsNullOrWhiteSpace(prev))
                return 0;

            int c = ParseInt(curr);
            int p = ParseInt(prev);
            int diff = c - p;

            return diff > 0 ? diff : 0;
        }

        private void RecalculateChast1Records()
        {
            int prevSNEMin = ParseHM(Chast1StartRecord.ColSNE);
            int prevLandingsInt = ParseInt(Chast1StartRecord.Landings);

            bool col5Active = !string.IsNullOrWhiteSpace(Chast1StartRecord.Col5);
            int prevCol5Min = col5Active ? ParseHM(Chast1StartRecord.Col5) : 0;

            bool col7Active = !string.IsNullOrWhiteSpace(Chast1StartRecord.Col7);
            int prevCol7Int = col7Active ? ParseInt(Chast1StartRecord.Col7) : 0;

            foreach (var rec in Chast1Records)
            {
                if (rec.IsTotal)
                    continue;

                DateTime recDate;
                if (!TryParseDate(rec.Date, out recDate))
                    continue;

                string dateKeyFull = recDate.ToString("dd.MM.yyyy");
                var dayData = _storage.GetDayData(SelectedProfile, dateKeyFull);

                int deltaNalet;
                int deltaVertolet;
                int deltaSamolet;
                int deltaPosad;

                if (dayData != null && dayData.Launches != null)
                {
                    int ground = 0;
                    int air = 0;
                    int vertolet = 0;
                    int samolet = 0;

                    foreach (var l in dayData.Launches)
                    {
                        ground += (int)Math.Round(l.Zemlya * 0.2);
                        air += l.Vozduh;

                        if (l.Vozduh > 0) vertolet++;
                        samolet += l.SamoletLandings;
                    }

                    deltaNalet = ground + air;
                    deltaVertolet = vertolet;
                    deltaSamolet = samolet;
                    deltaPosad = samolet + vertolet;

                    rec.GroundHours = MinutesToHM((int)Math.Round(
                        dayData.Launches.Sum(l => l.Zemlya) * 0.2));
                    rec.FlightDuration = air > 0 ? MinutesToHM(air) : "";
                    rec.Landings2 = vertolet > 0 ? vertolet.ToString() : "";
                    rec.Col9 = vertolet > 0 ? vertolet.ToString() : "";
                    rec.Col8 = samolet > 0 ? samolet.ToString() : "";
                    rec.Col11 = samolet > 0 ? samolet.ToString() : "";
                }
                else
                {
                    int recGround = ParseHM(rec.GroundHours);
                    int recAir = ParseHM(rec.FlightDuration);

                    int recSamolet = ParseInt(rec.Col11);
                    if (recSamolet == 0) recSamolet = ParseInt(rec.Col8);

                    int recVertolet = ParseInt(rec.Landings2);
                    if (recVertolet == 0) recVertolet = ParseInt(rec.Col9);

                    deltaNalet = recGround + recAir;
                    deltaVertolet = recVertolet;
                    deltaSamolet = recSamolet;
                    deltaPosad = recSamolet + recVertolet;
                }

                if (col5Active)
                {
                    prevCol5Min += deltaNalet;
                    rec.Col5 = MinutesToHM(prevCol5Min);
                }
                else rec.Col5 = "";

                prevSNEMin += deltaNalet;
                rec.ColSNE = MinutesToHM(prevSNEMin);

                if (col7Active)
                {
                    prevCol7Int += deltaPosad;
                    rec.Col7 = prevCol7Int.ToString();

                    rec.Col8 = deltaSamolet > 0 ? deltaSamolet.ToString() : "";
                    rec.Col9 = deltaVertolet > 0 ? deltaVertolet.ToString() : "";
                }
                else
                {
                    rec.Col7 = "";
                    rec.Col8 = "";
                    rec.Col9 = "";
                }

                prevLandingsInt += deltaPosad;
                rec.Landings = prevLandingsInt.ToString();

                rec.Col11 = deltaSamolet > 0 ? deltaSamolet.ToString() : "";
                rec.Landings2 = deltaVertolet > 0 ? deltaVertolet.ToString() : "";
            }
        }

        private void LoadChast1Records()
        {
            Chast1Records.Clear();

            if (string.IsNullOrWhiteSpace(SelectedProfile))
                return;

            var records = _storage.GetChast1Records(SelectedProfile);
            if (records != null)
            {
                foreach (var rec in records)
                {
                    // Миграция: старый файл без IsTotal — определяем по дате
                    if (!rec.IsTotal && logbook.Services.MonthlyTotals.TryMonth(rec.Date, out _))
                        rec.IsTotal = true;

                    Chast1Records.Add(rec);
                }
            }

            NormalizeTotals(Chast1Records);

            Chast1StartRecord = _storage.GetChast1StartRecord(SelectedProfile)
                                ?? new Chast1Record();

            if (!IsExchangeProfile) RecalculateChast1Records();
            if (!IsExchangeProfile) EnsureMonthlyTotals();
            if (!IsExchangeProfile) RefreshSNE();
            if (!IsExchangeProfile) RecalculateMonthlyTotals();
            SubscribeItems(Chast1Records);

            IsDirty = false;
        }

        private void RemoveChast1Record(Chast1Record record)
        {
            if (record == null) return;

            bool isTotal = record.IsTotal;

            string message = isTotal
                ? LocalizationManager.Format("Msg_DeleteTotal", record.Date) + "\n\n" +
                  LocalizationManager.Get("Msg_DeleteTotalNote")
                : LocalizationManager.Format("Msg_DeleteRecord", record.Date) + "\n\n" +
                  LocalizationManager.Get("Msg_DeleteAllFormulars");

            var result = MessageBox.Show(
                message,
                LocalizationManager.Get("Msg_ConfirmDeletion"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            TakeSnapshot();

            if (isTotal)
            {
                Chast1Records.Remove(record);
                RecalculateChast1Records();
                RecalculateMonthlyTotals();
                _storage.SaveChast1Records(SelectedProfile, Chast1Records.ToList());
            }
            else
            {
                RemoveDateFromAllFormulars(record.Date);
            }
        }

        private string GetLatestSNE()
        {
            for (int i = Chast1Records.Count - 1; i >= 0; i--)
            {
                var rec = Chast1Records[i];
                if (!rec.IsTotal)
                {
                    if (!string.IsNullOrWhiteSpace(rec.ColSNE))
                        return rec.ColSNE;
                }
            }

            return Chast1StartRecord.ColSNE ?? "";
        }

        private void RefreshSNE()
        {
            if (ResourceRecords == null || ResourceRecords.Count == 0) return;

            string latest = GetLatestSNE();
            if (string.IsNullOrWhiteSpace(latest)) return;

            ResourceRecords[0].SNE = latest;
        }
    }
}