using logbook.Helpers;
using logbook.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace logbook.ViewModels
{
    public partial class MainViewModel
    {
        // ============================================================
        // ==================== ВРЕМЯ =================================
        // ============================================================

        private string MinutesToHM(int totalMinutes)
        {
            if (totalMinutes < 0) totalMinutes = 0;
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            return $"{hours}:{minutes:D2}";
        }

        private int ParseHM(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            var parts = value.Split(':');
            if (parts.Length == 2
                && int.TryParse(parts[0], out int hours)
                && int.TryParse(parts[1], out int minutes))
            {
                return hours * 60 + minutes;
            }
            return 0;
        }

        private int ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            value = value.Replace(',', '.').Trim();
            if (int.TryParse(value, out int n)) return n;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                return (int)Math.Round(d);
            return 0;
        }

        private string AddMinutesToHM(string hm, int additionalMinutes)
        {
            int total = ParseHM(hm) + additionalMinutes;
            return MinutesToHM(total);
        }

        // ============================================================
        // ==================== ДАТЫ ==================================
        // ============================================================

        private bool TryParseDate(string value, out DateTime date)
        {
            date = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(value)) return false;

            if (DateTime.TryParseExact(value, "dd.MM.yy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date))
                return true;

            if (DateTime.TryParseExact(value, "dd.MM.yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date))
                return true;

            return false;
        }

        /// <summary>
        /// Вставляет запись в коллекцию по порядку даты.
        ///
        /// Логика:
        /// - Обычная запись — вставляется по дате среди обычных записей.
        /// - Итоговая запись (IsTotal) — вставляется так, чтобы быть ПОСЛЕ
        ///   всех обычных записей своего месяца и ПЕРЕД первой записью
        ///   следующего месяца (обычной или итоговой).
        /// - Если подходящего места нет — в конец.
        /// </summary>
        private void InsertByDate<T>(
    ObservableCollection<T> collection,
    T record,
    Func<T, string> dateSelector,
    Func<T, bool> isTotalSelector = null) where T : class
        {
            bool isTotalNew = isTotalSelector != null && isTotalSelector(record);
            string newDateStr = dateSelector(record);

            // ===== Ветка итоговой записи =====
            if (isTotalNew)
            {
                int newMonth = ParseMonthFromTotalName(newDateStr);
                int newYear = ParseYearFromTotalName(newDateStr);
                if (newMonth == 0 || newYear == 0)
                {
                    collection.Add(record);
                    return;
                }

                for (int i = 0; i < collection.Count; i++)
                {
                    var existing = collection[i];
                    bool isTotalExisting = isTotalSelector != null && isTotalSelector(existing);
                    string existingDate = dateSelector(existing);

                    int exMonth, exYear;
                    if (isTotalExisting)
                    {
                        exMonth = ParseMonthFromTotalName(existingDate);
                        exYear = ParseYearFromTotalName(existingDate);
                    }
                    else
                    {
                        if (!TryParseDate(existingDate, out var d)) continue;
                        exMonth = d.Month;
                        exYear = d.Year;
                    }

                    if (exMonth == 0 || exYear == 0) continue;

                    // Первая запись, чей месяц больше — вставляем перед ней
                    if (exYear > newYear || (exYear == newYear && exMonth > newMonth))
                    {
                        collection.Insert(i, record);
                        return;
                    }

                    // Если это итог ТОГО ЖЕ месяца — вставляем сразу после него
                    if (exYear == newYear && exMonth == newMonth && isTotalExisting)
                    {
                        collection.Insert(i + 1, record);
                        return;
                    }
                }

                collection.Add(record);
                return;
            }

            // ===== Ветка обычной записи =====
            DateTime newDate;
            if (!TryParseDate(newDateStr, out newDate))
            {
                collection.Add(record);
                return;
            }

            for (int i = 0; i < collection.Count; i++)
            {
                var existing = collection[i];
                bool isTotalExisting = isTotalSelector != null && isTotalSelector(existing);
                string existingDate = dateSelector(existing);

                if (isTotalExisting)
                {
                    int exMonth = ParseMonthFromTotalName(existingDate);
                    int exYear = ParseYearFromTotalName(existingDate);

                    // Итог того же месяца или позже — вставляем ПЕРЕД ним
                    if (exYear > newDate.Year ||
                        (exYear == newDate.Year && exMonth >= newDate.Month))
                    {
                        collection.Insert(i, record);
                        return;
                    }

                    // Итог раньше месяца новой записи — идём дальше
                    continue;
                }

                // Обычная существующая
                if (TryParseDate(existingDate, out var d) && d > newDate)
                {
                    collection.Insert(i, record);
                    return;
                }
            }

            collection.Add(record);
        }

        /// <summary>
        /// Количество полных месяцев между двумя датами. Может быть отрицательным.
        /// </summary>
        private int MonthsBetween(DateTime from, DateTime to)
        {
            int months = (to.Year - from.Year) * 12 + (to.Month - from.Month);

            if (to.Day < from.Day)
                months--;

            return months;
        }

        /// <summary>
        /// Возвращает строку "Nг.Mмес." — разницу между двумя датами.
        /// </summary>
        private string YearsMonthsBetween(DateTime from, DateTime to)
        {
            if (to <= from) return "0г.0мес.";

            int years = to.Year - from.Year;
            int months = to.Month - from.Month;

            if (to.Day < from.Day)
                months--;

            if (months < 0)
            {
                years--;
                months += 12;
            }

            return $"{years}г.{months}мес.";
        }

        // ============================================================
        // ==================== ФОРМАТИРОВАНИЕ ========================
        // ============================================================

        private string RoundDecimal(string value, int digits)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            if (value == "-") return value;

            string normalized = value.Replace(',', '.');
            if (!double.TryParse(normalized, NumberStyles.Any,
                CultureInfo.InvariantCulture, out double d))
                return value;

            double rounded = Math.Round(d, digits, MidpointRounding.AwayFromZero);
            return rounded.ToString("F" + digits, CultureInfo.InvariantCulture);
        }

        private string FormatYearsMonths(int totalMonths)
        {
            if (totalMonths < 0) totalMonths = 0;

            int years = totalMonths / 12;
            int months = totalMonths % 12;

            string y = LocalizationManager.Get("Resursy_YearsShort");
            string m = LocalizationManager.Get("Resursy_MonthsShort");
            return $"{years}{y}{months}{m}";
        }

        private string FormatYearsMonthsSigned(int totalMonths)
        {
            int abs = Math.Abs(totalMonths);
            int years = abs / 12;
            int months = abs % 12;

            string y = LocalizationManager.Get("Resursy_YearsShort");
            string m = LocalizationManager.Get("Resursy_MonthsShort");
            return $"-{years}{y}{months}{m}";
        }

        private double ParseTermYears(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            string normalized = value.Replace(',', '.');
            if (double.TryParse(normalized, NumberStyles.Any,
                CultureInfo.InvariantCulture, out double d))
                return d;

            return 0;
        }

        // ============================================================
        // ==================== МЕСЯЦЫ ================================
        // ============================================================

        /// <summary>
        /// Статический словарь названий месяцев (русский + английский).
        /// Используется для распознавания итоговых строк, созданных
        /// на любом языке — независимо от текущей локализации.
        /// </summary>
        private static readonly Dictionary<string, int> MonthNames =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "январь", 1 }, { "февраль", 2 }, { "март", 3 },
                { "апрель", 4 }, { "май", 5 }, { "июнь", 6 },
                { "июль", 7 }, { "август", 8 }, { "сентябрь", 9 },
                { "октябрь", 10 }, { "ноябрь", 11 }, { "декабрь", 12 },
                { "january", 1 }, { "february", 2 }, { "march", 3 },
                { "april", 4 }, { "may", 5 }, { "june", 6 },
                { "july", 7 }, { "august", 8 }, { "september", 9 },
                { "october", 10 }, { "november", 11 }, { "december", 12 }
            };

        private string GetMonthNameRu(int month)
        {
            string[] keys = {
                "Month_January", "Month_February", "Month_March", "Month_April",
                "Month_May", "Month_June", "Month_July", "Month_August",
                "Month_September", "Month_October", "Month_November", "Month_December"
            };

            if (month < 1 || month > 12) return "";
            return LocalizationManager.Get(keys[month - 1]);
        }

        /// <summary>
        /// Парсит номер месяца из названия итоговой строки.
        /// Работает для любого языка: сначала проверяет статический
        /// словарь (ru + en), затем — текущую локализацию.
        /// </summary>
        private int ParseMonthFromTotalName(string totalName)
        {
            return logbook.Services.MonthlyTotals.TryMonth(totalName, out var month) ? month.Month : 0;
        }

        private int ParseYearFromTotalName(string totalName)
        {
            if (string.IsNullOrWhiteSpace(totalName)) return 0;

            var parts = totalName.Split(' ');
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                if (int.TryParse(parts[i], out int year) && year > 1900 && year < 3000)
                    return year;
            }
            return 0;
        }

        /// <summary>
        /// Единый ярлык для итоговой строки: "Итого за Сентябрь 2026" /
        /// "Total for September 2026" — в зависимости от текущего языка.
        /// </summary>
        private string BuildTotalLabel(DateTime monthDate)
        {
            return logbook.Services.MonthlyTotals.Label(monthDate, LocalizationManager.CurrentLanguage);
        }

        // ============================================================
        // ==================== ПОДСЧЁТ ЗА ДЕНЬ =======================
        // ============================================================

        /// <summary>
        /// Количество посадок по вертолётному (Воздух > 0).
        /// </summary>
        private int CountVertoletLandings(IEnumerable<Launch> launches)
        {
            return launches.Count(l => l.Vozduh > 0);
        }

        /// <summary>
        /// Количество посадок по самолётному (сумма SamoletLandings).
        /// </summary>
        private int CountSamoletLandings(IEnumerable<Launch> launches)
        {
            return launches.Sum(l => l.SamoletLandings);
        }

        /// <summary>
        /// Общее количество посадок за день (самолётные + вертолётные).
        /// </summary>
        private int CountTotalLandings(IEnumerable<Launch> launches)
        {
            return CountVertoletLandings(launches) + CountSamoletLandings(launches);
        }

        /// <summary>
        /// Сумма ТП за день (в минутах).
        /// </summary>
        private int SumTP(IEnumerable<Launch> launches)
        {
            return launches.Sum(l => l.TP);
        }

        /// <summary>
        /// Налёт за день (минуты): Земля × 0.2 + Воздух.
        /// </summary>
        private int SumNalet(IEnumerable<Launch> launches)
        {
            int ground = 0;
            int air = 0;
            foreach (var l in launches)
            {
                ground += (int)Math.Round(l.Zemlya * 0.2);
                air += l.Vozduh;
            }
            return ground + air;
        }

        /// <summary>
        /// Сумма ВСУ за день (для Та-14, минуты).
        /// </summary>
        private int SumVsu(IEnumerable<Launch> launches)
        {
            return launches.Sum(l => l.Vsu);
        }

        /// <summary>
        /// Количество запусков ВСУ > 0 (для Та-14).
        /// </summary>
        private int CountVsuLaunches(IEnumerable<Launch> launches)
        {
            return launches.Count(l => l.Vsu > 0);
        }

        /// <summary>
        /// Упорядочивает и локализует итоги, сохраняя их значения и идентификаторы.
        /// </summary>
        private void NormalizeTotals<T>(ObservableCollection<T> records) where T : class
        {
            logbook.Services.MonthlyTotals.Normalize(records,
                r => (string)((dynamic)r).Date, (r, value) => ((dynamic)r).Date = value,
                r => (bool)((dynamic)r).IsTotal, (r, value) => ((dynamic)r).IsTotal = value,
                LocalizationManager.CurrentLanguage);
        }

        private void RebuildTotalLabels()
        {
            var wasLoading = _loadingProfile;
            _loadingProfile = true;
            try {
                NormalizeTotals(Chast1Records);
                NormalizeTotals(Vr80Records);
                NormalizeTotals(Ta14Records);
                NormalizeTotals(Vk250001Records);
                NormalizeTotals(Vk250002Records);
            } finally { _loadingProfile = wasLoading; }
        }
    }
}
