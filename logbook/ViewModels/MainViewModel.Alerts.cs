using logbook.Helpers;
using logbook.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace logbook.ViewModels
{
    public partial class MainViewModel
    {
        // ============================================================
        // ==================== РЕСУРСЫ (АГРЕГАТЫ) ====================
        // ============================================================

        /// <summary>
        /// Формирует список предупреждений по остаткам ресурсов (вкладка «Агрегаты»).
        /// </summary>
        public List<AlertMessage> BuildResourceAlerts()
        {
            var messages = new List<AlertMessage>();

            string prefixExhausted = LocalizationManager.Get("Alert_ResourceExhausted");
            string prefixRemaining = LocalizationManager.Get("Alert_ResourceRemaining");
            string suffixExtend = LocalizationManager.Get("Alert_ResourceExtend");
            string suffixReminder = LocalizationManager.Get("Alert_ResourceReminder");

            foreach (var rec in ResourceRecords)
            {
                if (string.IsNullOrWhiteSpace(rec.Name)) continue;

                // ===== Колонка 3: Остаток (чч:мм) =====
                if (!string.IsNullOrWhiteSpace(rec.Remaining1) &&
                    rec.Remaining1 != "не предусмотрено")
                {
                    int rem1 = ParseHM(rec.Remaining1);
                    int absRem1 = Math.Abs(rem1);
                    int h = absRem1 / 60;
                    int m = absRem1 % 60;
                    string timeStr = FormatHoursMinutes(h, m);

                    if (rec.Remaining1IsNegative || rem1 < 0)
                    {
                        messages.Add(new AlertMessage
                        {
                            Prefix = prefixExhausted.Replace("{0}", rec.Name),
                            Name = rec.Name,
                            Time = timeStr,
                            Middle = " ",
                            Suffix = LocalizationManager.Get("Alert_ResourceAgo")
                        });
                    }
                    else if (rem1 <= 15 * 60)
                    {
                        messages.Add(new AlertMessage
                        {
                            Prefix = prefixRemaining.Replace("{0}", rec.Name),
                            Name = rec.Name,
                            Middle = ": ",
                            Time = timeStr,
                            Suffix = suffixExtend.Replace("{0}", rec.Name)
                        });
                    }
                    else if (rem1 <= 50 * 60)
                    {
                        messages.Add(new AlertMessage
                        {
                            Prefix = prefixRemaining.Replace("{0}", rec.Name),
                            Name = rec.Name,
                            Middle = ": ",
                            Time = timeStr,
                            Suffix = suffixReminder.Replace("{0}", rec.Name)
                        });
                    }
                }

                // ===== Колонка 6: Остаток (дни / месяцы) =====
                if (!string.IsNullOrWhiteSpace(rec.IssueDate) &&
                    !string.IsNullOrWhiteSpace(rec.YearsResource))
                {
                    DateTime issueDate;
                    double years;
                    if (TryParseDate(rec.IssueDate, out issueDate) &&
                        double.TryParse(rec.YearsResource.Replace(',', '.'),
                            NumberStyles.Any, CultureInfo.InvariantCulture, out years))
                    {
                        DateTime targetDate = issueDate.AddDays(years * 365);
                        int totalDays = (int)Math.Round((targetDate - DateTime.Today).TotalDays);
                        int totalMonths = MonthsBetween(DateTime.Today, targetDate);
                        string daysStr = PluralizeDays(Math.Abs(totalDays));

                        if (totalDays < 0)
                        {
                            messages.Add(new AlertMessage
                            {
                                Prefix = prefixExhausted.Replace("{0}", rec.Name),
                                Name = rec.Name,
                                Time = daysStr,
                                Middle = " ",
                                Suffix = LocalizationManager.Get("Alert_ResourceAgo")
                            });
                        }
                        else if (totalMonths <= 1)
                        {
                            messages.Add(new AlertMessage
                            {
                                Prefix = prefixRemaining.Replace("{0}", rec.Name),
                                Name = rec.Name,
                                Middle = ": ",
                                Time = daysStr,
                                Suffix = suffixExtend.Replace("{0}", rec.Name)
                            });
                        }
                        else if (totalMonths <= 2)
                        {
                            messages.Add(new AlertMessage
                            {
                                Prefix = prefixRemaining.Replace("{0}", rec.Name),
                                Name = rec.Name,
                                Middle = ": ",
                                Time = daysStr,
                                Suffix = suffixReminder.Replace("{0}", rec.Name)
                            });
                        }
                    }
                }
            }

            return messages
                .GroupBy(m => $"{m.Prefix}|{m.Name}|{m.Time}|{m.Suffix}|{m.Middle}")
                .Select(g => g.First())
                .ToList();
        }

        // ============================================================
        // ==================== МАСЛА =================================
        // ============================================================

        public List<AlertMessage> BuildOilAlerts()
        {
            var messages = new List<AlertMessage>();

            string prefixHas = LocalizationManager.Get("Alert_Oil_Has");
            string prefixIn = LocalizationManager.Get("Alert_Oil_In");
            string suffixReplace = LocalizationManager.Get("Alert_Oil_Replace");
            string suffixExpires = LocalizationManager.Get("Alert_Oil_Expires");
            string suffixExpired = LocalizationManager.Get("Alert_Oil_Expired");

            foreach (var oil in OilRecords)
            {
                if (string.IsNullOrWhiteSpace(oil.Name)) continue;

                // ===== Колонка 3: Остаток (чч:мм) =====
                if (!string.IsNullOrWhiteSpace(oil.Remaining1) &&
                    oil.Remaining1 != "не предусмотрено")
                {
                    int rem1 = ParseHM(oil.Remaining1);
                    int absRem1 = Math.Abs(rem1);
                    int h = absRem1 / 60;
                    int m = absRem1 % 60;
                    string timeStr = FormatHoursMinutes(h, m);

                    if (oil.Remaining1IsNegative || rem1 < 0)
                    {
                        messages.Add(new AlertMessage
                        {
                            Prefix = "",
                            Time = "",
                            Middle = "",
                            Name = oil.Name,
                            Suffix = suffixExpired.Replace("{0}", oil.Name)
                        });
                    }
                    else if (rem1 <= 10 * 60)
                    {
                        messages.Add(new AlertMessage
                        {
                            Prefix = prefixHas.Replace("{0}", oil.Name),
                            Name = oil.Name,
                            Middle = " ",
                            Time = timeStr,
                            Suffix = suffixReplace
                        });
                    }
                    else if (rem1 <= 20 * 60)
                    {
                        messages.Add(new AlertMessage
                        {
                            Prefix = prefixIn.Replace("{0}", oil.Name),
                            Name = oil.Name,
                            Middle = " ",
                            Time = timeStr,
                            Suffix = suffixExpires
                        });
                    }
                }

                // ===== Колонка 4: Остаток (дни / месяцы) =====
                if (!string.IsNullOrWhiteSpace(oil.Remaining2) &&
                    oil.Remaining2 != "не предусмотрено")
                {
                    int totalDays = ParseRemaining2Days(oil.Remaining2);
                    bool isNegative = oil.Remaining2IsNegative;
                    string daysStr = PluralizeDays(Math.Abs(totalDays));

                    if (isNegative || totalDays < 0)
                    {
                        messages.Add(new AlertMessage
                        {
                            Prefix = "",
                            Time = "",
                            Middle = "",
                            Name = oil.Name,
                            Suffix = suffixExpired.Replace("{0}", oil.Name)
                        });
                    }
                    else if (totalDays <= 15)
                    {
                        messages.Add(new AlertMessage
                        {
                            Prefix = prefixHas.Replace("{0}", oil.Name),
                            Name = oil.Name,
                            Middle = " ",
                            Time = daysStr,
                            Suffix = suffixReplace
                        });
                    }
                    else if (totalDays <= 30)
                    {
                        messages.Add(new AlertMessage
                        {
                            Prefix = prefixIn.Replace("{0}", oil.Name),
                            Name = oil.Name,
                            Middle = " ",
                            Time = daysStr,
                            Suffix = suffixExpires
                        });
                    }
                }
            }

            return messages
                .GroupBy(m => $"{m.Prefix}|{m.Name}|{m.Time}|{m.Suffix}|{m.Middle}")
                .Select(g => g.First())
                .ToList();
        }

        // ============================================================
        // ==================== ВСПОМОГАТЕЛЬНЫЕ =======================
        // ============================================================

        private string FormatHoursMinutes(int hours, int minutes)
        {
            string hourStr = Pluralize(hours, "час", "часа", "часов");
            if (minutes == 0)
                return $"{hours} {hourStr}";

            string minuteStr = Pluralize(minutes, "минута", "минуты", "минут");
            return $"{hours} {hourStr} {minutes} {minuteStr}";
        }

        private string PluralizeDays(int days)
        {
            string dayStr = Pluralize(days, "день", "дня", "дней");
            return $"{days} {dayStr}";
        }

        private string Pluralize(int number, string form1, string form2, string form5)
        {
            int mod10 = number % 10;
            int mod100 = number % 100;

            if (mod10 == 1 && mod100 != 11)
                return form1;
            if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20))
                return form2;
            return form5;
        }

        /// <summary>
        /// Парсит Remaining2 ("45 дн." или "2г.3мес.") в количество дней.
        /// </summary>
        private int ParseRemaining2Days(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            value = value.Trim();

            if (value.EndsWith("дн."))
            {
                var numPart = value.Replace("дн.", "").Trim();
                if (int.TryParse(numPart, out int days))
                    return days;
                return 0;
            }

            if (value.Contains("г.") && value.Contains("мес."))
            {
                var parts = value.Replace("г.", "|").Replace("мес.", "").Split('|');
                if (parts.Length == 2)
                {
                    int years = 0, months = 0;
                    int.TryParse(parts[0].Trim(), out years);
                    int.TryParse(parts[1].Trim(), out months);
                    return years * 365 + months * 30;
                }
            }

            if (value.EndsWith("г."))
            {
                var numPart = value.Replace("г.", "").Trim();
                if (int.TryParse(numPart, out int years))
                    return years * 365;
            }

            if (int.TryParse(value, out int n))
                return n;

            return 0;
        }
    }
}