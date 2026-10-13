using logbook.Helpers;
using logbook.Models;
using logbook.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace logbook.ViewModels
{
    public partial class MainViewModel
    {
        // ============================================================
        // ==================== ВК-2500-01 / 02 =======================
        // ============================================================

        private void LoadVk2500Coefficients()
        {
            var (a1, h1) = _storage.GetVk250001Coefficients(SelectedProfile);
            Vk250001CoeffA = a1;
            Vk250001CoeffH = h1;

            var (a2, h2) = _storage.GetVk250002Coefficients(SelectedProfile);
            Vk250002CoeffA = a2;
            Vk250002CoeffH = h2;
        }

        private void SaveVk250001StartRecord()
        {
            _storage.SaveVk250001StartRecord(SelectedProfile, Vk250001StartRecord);

            RecalculateVk250001Records();
            EnsureMonthlyTotalsVk2500(Vk250001Records);
            _storage.SaveVk250001Records(SelectedProfile, Vk250001Records.ToList());

            MessageBox.Show(
                LocalizationManager.Get("Msg_StartSavedVk01"),
                LocalizationManager.Get("Msg_Info"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveVk250002StartRecord()
        {
            _storage.SaveVk250002StartRecord(SelectedProfile, Vk250002StartRecord);

            RecalculateVk250002Records();
            EnsureMonthlyTotalsVk2500(Vk250002Records);
            _storage.SaveVk250002Records(SelectedProfile, Vk250002Records.ToList());

            MessageBox.Show(
                LocalizationManager.Get("Msg_StartSavedVk02"),
                LocalizationManager.Get("Msg_Info"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveVk250001Coefficients()
        {
            _storage.SaveVk250001Coefficients(SelectedProfile, Vk250001CoeffA, Vk250001CoeffH);

            RecalculateVk250001Records();
            _storage.SaveVk250001Records(SelectedProfile, Vk250001Records.ToList());

            MessageBox.Show(
                LocalizationManager.Get("Msg_CoeffSavedVk01"),
                LocalizationManager.Get("Msg_Info"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveVk250002Coefficients()
        {
            _storage.SaveVk250002Coefficients(SelectedProfile, Vk250002CoeffA, Vk250002CoeffH);

            RecalculateVk250002Records();
            _storage.SaveVk250002Records(SelectedProfile, Vk250002Records.ToList());

            MessageBox.Show(
                LocalizationManager.Get("Msg_CoeffSavedVk02"),
                LocalizationManager.Get("Msg_Info"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadVk250001Records()
        {
            Vk250001Records.Clear();

            if (string.IsNullOrWhiteSpace(SelectedProfile))
                return;

            var records = _storage.GetVk250001Records(SelectedProfile);
            if (records != null)
            {
                foreach (var r in records)
                {
                    // Миграция: старый файл без IsTotal — определяем по дате
                    if (!r.IsTotal && logbook.Services.MonthlyTotals.TryMonth(r.Date, out _))
                        r.IsTotal = true;

                    Vk250001Records.Add(r);
                }
            }

            NormalizeTotals(Vk250001Records);

            Vk250001StartRecord = _storage.GetVk250001StartRecord(SelectedProfile)
                                  ?? new Vk250001Record();
            RecalculateVk250001Records();
            SubscribeItems(Vk250001Records);

            IsDirty = false;
        }

        private void LoadVk250002Records()
        {
            Vk250002Records.Clear();

            if (string.IsNullOrWhiteSpace(SelectedProfile))
                return;

            var records = _storage.GetVk250002Records(SelectedProfile);
            if (records != null)
            {
                foreach (var r in records)
                {
                    if (!r.IsTotal && logbook.Services.MonthlyTotals.TryMonth(r.Date, out _))
                        r.IsTotal = true;

                    Vk250002Records.Add(r);
                }
            }

            NormalizeTotals(Vk250002Records);

            Vk250002StartRecord = _storage.GetVk250002StartRecord(SelectedProfile)
                                  ?? new Vk250002Record();
            RecalculateVk250002Records();
            SubscribeItems(Vk250002Records);

            IsDirty = false;
        }

        private void RemoveVk250001Record(Vk250001Record record)
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
                Vk250001Records.Remove(record);
                _storage.SaveVk250001Records(SelectedProfile, Vk250001Records.ToList());
            }
            else
            {
                RemoveDateFromAllFormulars(record.Date);
            }
        }

        private void RemoveVk250002Record(Vk250002Record record)
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
                Vk250002Records.Remove(record);
                _storage.SaveVk250002Records(SelectedProfile, Vk250002Records.ToList());
            }
            else
            {
                RemoveDateFromAllFormulars(record.Date);
            }
        }

        // ============================================================
        // ==================== ПЕРЕСЧЁТ ВК ===========================
        // ============================================================

        private void RecalculateVk250001Records()
        {
            RecalculateVkRecords(Vk250001Records, Vk250001StartRecord,
                Vk250001CoeffA, Vk250001CoeffH,
                useLeftSnk: true);

            RecalculateVk2500MonthlyTotals(Vk250001Records);
        }

        private void RecalculateVk250002Records()
        {
            RecalculateVkRecords(Vk250002Records, Vk250002StartRecord,
                Vk250002CoeffA, Vk250002CoeffH,
                useLeftSnk: false);

            RecalculateVk2500MonthlyTotals(Vk250002Records);
        }

        /// <summary>
        /// Пересчёт ВК-2500.
        /// </summary>
        private void RecalculateVkRecords<T>(
            ObservableCollection<T> records,
            dynamic startRecord,
            string coeffA,
            string coeffH,
            bool useLeftSnk) where T : class
        {
            string baseA = (string)(startRecord.A ?? "");
            string baseH = (string)(startRecord.H ?? "");
            string baseI = (string)(startRecord.I ?? "");

            string baseAPpr = (string)(startRecord.A_PPR ?? "");
            string baseHPpr = (string)(startRecord.H_PPR ?? "");

            string baseG = (string)(startRecord.G ?? "");
            string baseB = (string)(startRecord.B ?? "");
            string baseD = (string)(startRecord.D ?? "");
            string baseE = (string)(startRecord.E ?? "");

            bool aPprActive = !string.IsNullOrWhiteSpace(baseAPpr);
            bool hPprActive = !string.IsNullOrWhiteSpace(baseHPpr);

            int aPprBaseMin = aPprActive ? ParseHM(baseAPpr) : 0;
            double hPprBaseVal = hPprActive ? ParseDecimal(baseHPpr) : 0;

            double ca = ParseDecimal(string.IsNullOrWhiteSpace(coeffA) ? "1" : coeffA);
            double ch = ParseDecimal(string.IsNullOrWhiteSpace(coeffH) ? "1" : coeffH);

            string GetSnk(DayData dayData, string label)
            {
                if (dayData?.SnkValues == null) return "";
                var sv = dayData.SnkValues.FirstOrDefault(s => s.Label == label);
                if (sv == null) return "";
                var val = useLeftSnk ? (sv.LeftValue ?? "") : (sv.RightValue ?? "");
                return ExchangeCodec.CleanSnk(val, label);
            }

            // === Сортируем записи по дате (кроме итогов) ===
            var sorted = records
                .Where(r =>
                {
                    dynamic rr = r;
                    return !rr.IsTotal;
                })
                .Select(r =>
                {
                    dynamic rr = r;
                    DateTime d;
                    TryParseDate((string)rr.Date, out d);
                    return new { Record = r, Date = d };
                })
                .Where(x => x.Date != default(DateTime))
                .OrderBy(x => x.Date)
                .ToList();

            string prevA = baseA;
            double prevH = ParseDecimal(baseH);
            int prevI = ParseInt(baseI);

            string prevG = baseG;
            string prevB = baseB;
            string prevD = baseD;
            string prevE = baseE;

            foreach (var item in sorted)
            {
                dynamic rec = item.Record;
                string dateKeyFull = item.Date.ToString("dd.MM.yyyy");

                var dayData = _storage.GetDayData(SelectedProfile, dateKeyFull);
                if (dayData == null)
                {
                    if (!string.IsNullOrWhiteSpace((string)rec.A)) prevA = (string)rec.A;
                    if (!string.IsNullOrWhiteSpace((string)rec.H)) prevH = ParseDecimal((string)rec.H);
                    if (!string.IsNullOrWhiteSpace((string)rec.I)) prevI = ParseInt((string)rec.I);
                    if (!string.IsNullOrWhiteSpace((string)rec.G)) prevG = (string)rec.G;
                    if (!string.IsNullOrWhiteSpace((string)rec.B)) prevB = (string)rec.B;
                    if (!string.IsNullOrWhiteSpace((string)rec.D)) prevD = (string)rec.D;
                    if (!string.IsNullOrWhiteSpace((string)rec.E)) prevE = (string)rec.E;
                    continue;
                }

                var snkA = GetSnk(dayData, "A");
                var snkI = GetSnk(dayData, "I");
                var snkH = GetSnk(dayData, "H");
                var snkG = GetSnk(dayData, "G");
                var snkB = GetSnk(dayData, "B");
                var snkD = GetSnk(dayData, "D");
                var snkE = GetSnk(dayData, "E");

                bool hasA = ExchangeCodec.HasSnkValue(snkA);
                bool hasI = ExchangeCodec.HasSnkValue(snkI);
                bool hasH = ExchangeCodec.HasSnkValue(snkH);
                bool hasG = ExchangeCodec.HasSnkValue(snkG);
                bool hasB = ExchangeCodec.HasSnkValue(snkB);
                bool hasD = ExchangeCodec.HasSnkValue(snkD);
                bool hasE = ExchangeCodec.HasSnkValue(snkE);

                int groundMinutes = 0;
                int airMinutes = 0;
                int zemlyaLaunches = 0;

                if (dayData.Launches != null)
                {
                    foreach (var l in dayData.Launches)
                    {
                        groundMinutes += (int)Math.Round(l.Zemlya * 0.2);
                        airMinutes += l.Vozduh;
                        if (l.Zemlya > 0) zemlyaLaunches++;
                    }
                }

                int rawDelta = groundMinutes + airMinutes;

                // ===== A =====
                if (hasA)
                {
                    rec.A = ConvertDecimalHoursToHM(snkA);
                    prevA = (string)rec.A;
                }
                else if (!string.IsNullOrWhiteSpace(prevA))
                {
                    int deltaA = (int)Math.Round(rawDelta * ca, MidpointRounding.AwayFromZero);
                    rec.A = AddMinutesToHM(prevA, deltaA);
                    prevA = (string)rec.A;
                }
                else
                {
                    rec.A = "";
                }

                // ===== A_PPR =====
                if (aPprActive && !string.IsNullOrWhiteSpace((string)rec.A))
                {
                    int aDayMin = ParseHM((string)rec.A);
                    rec.A_PPR = MinutesToHM(aDayMin - aPprBaseMin);
                }
                else rec.A_PPR = "";

                // ===== I =====
                if (hasI)
                {
                    int iVal = ParseInt(snkI);
                    rec.I = iVal > 0 ? iVal.ToString() : "";
                    prevI = iVal;
                }
                else if (!string.IsNullOrWhiteSpace(baseI) || prevI != 0)
                {
                    int newI = prevI + zemlyaLaunches;
                    rec.I = newI.ToString();
                    prevI = newI;
                }
                else
                {
                    rec.I = "";
                }

                // ===== H =====
                if (hasH)
                {
                    rec.H = RoundDecimal(snkH, 1);
                    prevH = ParseDecimal(snkH);
                }
                else if (!string.IsNullOrWhiteSpace(baseH) || prevH != 0)
                {
                    double deltaH = rawDelta * ch;
                    prevH += deltaH;
                    rec.H = RoundDecimal(FormatDecimal(prevH), 1);
                }
                else
                {
                    rec.H = "";
                }

                // ===== H_PPR =====
                if (hPprActive && !string.IsNullOrWhiteSpace((string)rec.H))
                {
                    double hDay = ParseDecimal((string)rec.H);
                    rec.H_PPR = RoundDecimal(FormatDecimal(hDay - hPprBaseVal), 1);
                }
                else rec.H_PPR = "";

                // ===== G, B, D, E =====
                if (hasG) prevG = snkG;
                if (hasB) prevB = snkB;
                if (hasD) prevD = snkD;
                if (hasE) prevE = snkE;

                rec.G = prevG;
                rec.B = prevB;
                rec.D = prevD;
                rec.E = prevE;
            }
        }

        // ============================================================
        // ==================== ВСПОМОГАТЕЛЬНЫЕ ВК ====================
        // ============================================================

        private string ConvertDecimalHoursToHM(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            value = value.Replace(',', '.');
            if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double hours))
                return "";

            int totalMinutes = (int)Math.Round(hours * 60, MidpointRounding.AwayFromZero);
            return MinutesToHM(totalMinutes);
        }

        private double ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            value = value.Replace(',', '.');
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                return d;
            return 0;
        }

        private string FormatDecimal(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        // ============================================================
        // ==================== ИТОГИ ВК ==============================
        // ============================================================

        private void EnsureMonthlyTotalsVk2500<T>(ObservableCollection<T> records) where T : class, new()
        {
            NormalizeTotals(records);
            var months = records
                .Where(r =>
                {
                    dynamic rr = r;
                    return !rr.IsTotal;
                })
                .Select(r =>
                {
                    dynamic rr = r;
                    return TryParseDate((string)rr.Date, out var d) ? (DateTime?)d : null;
                })
                .Where(d => d.HasValue)
                .Select(d => new { Year = d.Value.Year, Month = d.Value.Month })
                .Distinct()
                .ToList();

            foreach (var m in months)
            {
                DateTime monthDate = new DateTime(m.Year, m.Month, 1);
                AddMonthlyTotalVk2500(records, monthDate);
            }
        }

        private void AddMonthlyTotalVk2500<T>(
    ObservableCollection<T> records,
    DateTime monthDate) where T : class, new()
        {
            string totalLabel = BuildTotalLabel(monthDate);

            bool exists = records.Any(r =>
            {
                dynamic rr = r;
                if (!rr.IsTotal) return false;
                int m = ParseMonthFromTotalName((string)rr.Date);
                int y = ParseYearFromTotalName((string)rr.Date);
                return m == monthDate.Month && y == monthDate.Year;
            });
            if (exists) return;

            bool hasRecords = records.Any(r =>
            {
                dynamic rr = r;
                if (rr.IsTotal) return false;
                if (!TryParseDate((string)rr.Date, out var d)) return false;
                return d.Month == monthDate.Month && d.Year == monthDate.Year;
            });

            if (!hasRecords) return;

            dynamic totalRec = BuildMonthlyTotalVk2500(records, monthDate, totalLabel);
            InsertByDate(records, (T)totalRec, r => ((dynamic)r).Date, r => ((dynamic)r).IsTotal);
        }

        /// <summary>
        /// Строит (или обновляет) итоговую запись ВК за месяц.
        /// </summary>
        private dynamic BuildMonthlyTotalVk2500<T>(
            ObservableCollection<T> records,
            DateTime monthDate,
            string totalLabel) where T : class, new()
        {
            dynamic totalRec = new T();
            totalRec.IsTotal = true;
            totalRec.Date = totalLabel;

            // === Текущий месяц ===
            var currentMonthRecords = new List<dynamic>();
            foreach (var r in records)
            {
                dynamic rr = r;
                if (rr.IsTotal) continue;
                DateTime d;
                if (TryParseDate((string)rr.Date, out d) &&
                    d.Month == monthDate.Month && d.Year == monthDate.Year)
                {
                    currentMonthRecords.Add(rr);
                }
            }

            if (currentMonthRecords.Count == 0)
            {
                totalRec.A = "";
                totalRec.A_PPR = "";
                totalRec.B = "";
                totalRec.D = "";
                totalRec.E = "";
                totalRec.G = "";
                totalRec.H = "";
                totalRec.H_PPR = "";
                totalRec.I = "";
                return totalRec;
            }

            // === Ближайший предыдущий месяц с записями ===
            DateTime startOfCurrentMonth = new DateTime(monthDate.Year, monthDate.Month, 1);

            var prevMonthRecords = new List<dynamic>();
            foreach (var r in records)
            {
                dynamic rr = r;
                if (rr.IsTotal) continue;
                DateTime d;
                if (TryParseDate((string)rr.Date, out d) &&
                    new DateTime(d.Year, d.Month, 1) < startOfCurrentMonth)
                {
                    prevMonthRecords.Add(rr);
                }
            }

            if (prevMonthRecords.Count == 0)
            {
                totalRec.A = "";
                totalRec.A_PPR = "";
                totalRec.B = "";
                totalRec.D = "";
                totalRec.E = "";
                totalRec.G = "";
                totalRec.H = "";
                totalRec.H_PPR = "";
                totalRec.I = "";
                totalRec.NeedsManualRecalc = true;
                return totalRec;
            }

            DateTime lastPrevDate = prevMonthRecords
                .Select(r => { TryParseDate((string)r.Date, out var d); return d; })
                .Max();

            DateTime prevMonthDate = new DateTime(lastPrevDate.Year, lastPrevDate.Month, 1);

            dynamic prevMonthLast = null;
            foreach (var r in prevMonthRecords)
            {
                DateTime d;
                if (!TryParseDate((string)r.Date, out d)) continue;
                if (d.Year == prevMonthDate.Year && d.Month == prevMonthDate.Month)
                {
                    prevMonthLast = r;
                }
            }

            if (prevMonthLast == null)
            {
                totalRec.A = "";
                totalRec.A_PPR = "";
                totalRec.B = "";
                totalRec.D = "";
                totalRec.E = "";
                totalRec.G = "";
                totalRec.H = "";
                totalRec.H_PPR = "";
                totalRec.I = "";
                return totalRec;
            }

            dynamic last = currentMonthRecords[currentMonthRecords.Count - 1];

            // СНЭ-колонки — показываем всегда (в т.ч. "—" при 0)
            totalRec.A = DiffHM((string)last.A, (string)prevMonthLast.A);

            // ППР-колонки — если "—" или 0, оставляем пусто
            string aPpr = DiffHM((string)last.A_PPR, (string)prevMonthLast.A_PPR);
            totalRec.A_PPR = (aPpr == "—" || aPpr == "0:00") ? "" : aPpr;

            string b = DiffDec((string)last.B, (string)prevMonthLast.B);
            totalRec.B = b == "—" ? "—" : RoundDecimal(b, 3);

            totalRec.D = DiffDec((string)last.D, (string)prevMonthLast.D);

            string e = DiffDec((string)last.E, (string)prevMonthLast.E);
            totalRec.E = e == "—" ? "—" : RoundDecimal(e, 2);

            string g = DiffDec((string)last.G, (string)prevMonthLast.G);
            totalRec.G = g == "—" ? "—" : RoundDecimal(g, 2);

            string h = DiffDec((string)last.H, (string)prevMonthLast.H);
            totalRec.H = h == "—" ? "—" : RoundDecimal(h, 1);

            string hPpr = DiffDec((string)last.H_PPR, (string)prevMonthLast.H_PPR);
            totalRec.H_PPR = (hPpr == "—" || hPpr == "0") ? "" : RoundDecimal(hPpr, 1);

            int cI = 0, pI = 0;
            int.TryParse((string)last.I, out cI);
            int.TryParse((string)prevMonthLast.I, out pI);
            int dI = cI - pI;
            totalRec.I = dI == 0 ? "—" : dI.ToString();

            totalRec.NeedsManualRecalc = false;
            return totalRec;
        }

        /// <summary>
        /// Разница крайних значений времени. Если один пуст — "—". Если 0 — "—".
        /// </summary>
        private string DiffHM(string curr, string prev)
        {
            if (string.IsNullOrWhiteSpace(curr) || string.IsNullOrWhiteSpace(prev))
                return "—";
            int c = ParseHM(curr);
            int p = ParseHM(prev);
            int diff = c - p;
            if (diff == 0) return "—";
            if (diff < 0) diff = -diff;
            return MinutesToHM(diff);
        }

        /// <summary>
        /// Разница крайних значений числа. Если один пуст — "—". Если 0 — "—".
        /// </summary>
        private string DiffDec(string curr, string prev)
        {
            if (string.IsNullOrWhiteSpace(curr) || string.IsNullOrWhiteSpace(prev))
                return "—";
            double c = ParseDecimal(curr);
            double p = ParseDecimal(prev);
            double diff = c - p;
            if (Math.Abs(diff) < 0.0001) return "—";
            if (diff < 0) diff = -diff;
            return FormatDecimal(diff);
        }

        // ============================================================
        // ==================== ЗАПОЛНЕНИЕ ВК =========================
        // ============================================================

        private void FillVkRecord<T>(
            ObservableCollection<T> records,
            dynamic startRecord,
            List<string> snkValues,
            string coeffA,
            string coeffH,
            DateTime recordDate,
            int groundMinutes,
            int airMinutes,
            int zemlyaLaunches) where T : class, new()
        {
            string dateKey = recordDate.ToString("dd.MM.yy");

            dynamic prev = null;
            for (int i = records.Count - 1; i >= 0; i--)
            {
                dynamic r = records[i];
                if (!r.IsTotal)
                {
                    prev = r;
                    break;
                }
            }

            string baseA = prev?.A ?? startRecord.A ?? "";
            string baseB = prev?.B ?? startRecord.B ?? "";
            string baseD = prev?.D ?? startRecord.D ?? "";
            string baseE = prev?.E ?? startRecord.E ?? "";
            string baseG = prev?.G ?? startRecord.G ?? "";
            string baseH = prev?.H ?? startRecord.H ?? "";
            string baseI = prev?.I ?? startRecord.I ?? "";

            string snkA = snkValues.Count > 0 ? snkValues[0] : "";
            string snkB = snkValues.Count > 1 ? snkValues[1] : "";
            string snkD = snkValues.Count > 2 ? snkValues[2] : "";
            string snkE = snkValues.Count > 3 ? snkValues[3] : "";
            string snkG = snkValues.Count > 4 ? snkValues[4] : "";
            string snkH = snkValues.Count > 5 ? snkValues[5] : "";
            string snkI = snkValues.Count > 6 ? snkValues[6] : "";

            bool hasA = ExchangeCodec.HasSnkValue(snkA);
            bool hasB = ExchangeCodec.HasSnkValue(snkB);
            bool hasD = ExchangeCodec.HasSnkValue(snkD);
            bool hasE = ExchangeCodec.HasSnkValue(snkE);
            bool hasG = ExchangeCodec.HasSnkValue(snkG);
            bool hasH = ExchangeCodec.HasSnkValue(snkH);
            bool hasI = ExchangeCodec.HasSnkValue(snkI);

            bool aActive = !string.IsNullOrWhiteSpace(baseA);
            bool hActive = !string.IsNullOrWhiteSpace(baseH);
            bool iActive = !string.IsNullOrWhiteSpace(baseI);

            string newA, newB, newD, newE, newG, newH, newI;

            // ===== A =====
            if (hasA) newA = ConvertDecimalHoursToHM(snkA);
            else if (aActive)
            {
                double coeff = ParseDecimal(string.IsNullOrWhiteSpace(coeffA) ? "1" : coeffA);
                int delta = (int)Math.Round((groundMinutes + airMinutes) * coeff,
                                            MidpointRounding.AwayFromZero);
                newA = AddMinutesToHM(baseA, delta);
            }
            else newA = "";

            // ===== I =====
            if (hasI)
            {
                int iVal = ParseInt(snkI);
                newI = iVal > 0 ? iVal.ToString() : "";
            }
            else if (iActive)
            {
                int baseIInt = ParseInt(baseI);
                newI = (baseIInt + zemlyaLaunches).ToString();
            }
            else newI = "";

            // ===== H =====
            if (hasH) newH = RoundDecimal(snkH, 1);
            else if (hActive)
            {
                double coeff = ParseDecimal(string.IsNullOrWhiteSpace(coeffH) ? "1" : coeffH);
                double baseHDouble = ParseDecimal(baseH);
                double delta = (groundMinutes + airMinutes) * coeff;
                newH = RoundDecimal(FormatDecimal(baseHDouble + delta), 1);
            }
            else newH = "";

            newB = RoundDecimal(hasB ? snkB : baseB, 3);
            newD = hasD ? snkD : baseD;
            newE = RoundDecimal(hasE ? snkE : baseE, 2);
            newG = RoundDecimal(hasG ? snkG : baseG, 2);

            // ===== A_PPR =====
            string newAPpr = "";
            string baseAPpr = (string)(startRecord.A_PPR ?? "");
            if (!string.IsNullOrWhiteSpace(baseAPpr) && !string.IsNullOrWhiteSpace(newA))
            {
                int aDayMin = ParseHM(newA);
                int aPprBase = ParseHM(baseAPpr);
                newAPpr = MinutesToHM(aDayMin - aPprBase);
            }

            // ===== H_PPR =====
            string newHPpr = "";
            string baseHPpr = (string)(startRecord.H_PPR ?? "");
            if (!string.IsNullOrWhiteSpace(baseHPpr) && !string.IsNullOrWhiteSpace(newH))
            {
                double hDay = ParseDecimal(newH);
                double hPprBase = ParseDecimal(baseHPpr);
                newHPpr = RoundDecimal(FormatDecimal(hDay - hPprBase), 1);
            }

            dynamic existing = null;
            foreach (var r in records)
            {
                dynamic rr = r;
                if (rr.Date == dateKey) { existing = rr; break; }
            }

            if (existing != null)
            {
                existing.A = newA;
                existing.A_PPR = newAPpr;
                existing.B = newB;
                existing.D = newD;
                existing.E = newE;
                existing.G = newG;
                existing.H = newH;
                existing.H_PPR = newHPpr;
                existing.I = newI;
            }
            else
            {
                dynamic rec = new T();
                rec.Date = dateKey;
                rec.A = newA;
                rec.A_PPR = newAPpr;
                rec.B = newB;
                rec.D = newD;
                rec.E = newE;
                rec.G = newG;
                rec.H = newH;
                rec.H_PPR = newHPpr;
                rec.I = newI;

                InsertByDate(records, (T)rec, r => ((dynamic)r).Date, r => ((dynamic)r).IsTotal);
            }
        }

        /// <summary>
        /// Пересчитывает существующие итоговые строки ВК.
        /// </summary>
        private void RecalculateVk2500MonthlyTotals<T>(ObservableCollection<T> records) where T : class, new()
        {
            var totals = records
                .Where(r =>
                {
                    dynamic rr = r;
                    return rr.IsTotal;
                })
                .ToList();

            foreach (var totalRec in totals)
            {
                dynamic tr = totalRec;

                int month = ParseMonthFromTotalName((string)tr.Date);
                int year = ParseYearFromTotalName((string)tr.Date);
                if (month == 0 || year == 0) continue;

                DateTime monthDate = new DateTime(year, month, 1);

                var updated = BuildMonthlyTotalVk2500(records, monthDate, (string)tr.Date);

                tr.A = updated.A;
                tr.A_PPR = updated.A_PPR;
                tr.B = updated.B;
                tr.D = updated.D;
                tr.E = updated.E;
                tr.G = updated.G;
                tr.H = updated.H;
                tr.H_PPR = updated.H_PPR;
                tr.I = updated.I;
                tr.NeedsManualRecalc = updated.NeedsManualRecalc;
            }
        }
    }
}