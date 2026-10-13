using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using logbook.Helpers;
using logbook.Models;

namespace logbook.Services
{
    public static class MaintenanceJournalService
    {
        public const string Periodic = "PERIODIC";
        public const string Hours50 = "HOURS_50";
        public const string Hours100 = "HOURS_100";
        public const string Hours300 = "HOURS_300";
        public const string Reg12 = "REG_12";
        public const string Reg24 = "REG_24";
        public const string Other = "OTHER";

        public static readonly List<string> Kinds = new()
        {
            Periodic, Hours50, Hours100, Hours300, Reg12, Reg24, Other
        };

        public static string FormatHours(long minutes)
        {
            if (minutes < 0) minutes = 0;
            return $"{minutes / 60}:{(minutes % 60):D2}";
        }

        public static int? ParseHours(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var match = Regex.Match(text.Trim(), @"^(\d+):([0-5]\d)$");
            if (!match.Success) return null;
            if (!long.TryParse(match.Groups[1].Value, out var hours)) return null;
            if (!long.TryParse(match.Groups[2].Value, out var minutes)) return null;
            var total = hours * 60 + minutes;
            if (total > int.MaxValue) return null;
            return (int)total;
        }

        public static string GetKindTitle(string kind)
        {
            return kind switch
            {
                Periodic => LocalizationManager.Get("Journal_Periodic"),
                Hours50 => LocalizationManager.Get("Journal_50"),
                Hours100 => LocalizationManager.Get("Journal_100"),
                Hours300 => LocalizationManager.Get("Journal_300"),
                Reg12 => LocalizationManager.Get("Journal_12"),
                Reg24 => LocalizationManager.Get("Journal_24"),
                _ => LocalizationManager.Get("Journal_Other")
            };
        }

        public static List<WorkDueItem> CalculateDue(List<MaintenanceWork> works, int counterMinutes, DateTime today)
        {
            var result = new List<WorkDueItem>();
            var activeKinds = Kinds.Where(k => k != Other).ToList();

            foreach (var kind in activeKinds)
            {
                var candidates = works
                    .Where(w => w.Kind == kind || (kind == Hours50 && w.Kind == Hours100))
                    .ToList();

                MaintenanceWork? last;
                if (kind == Hours50 || kind == Hours100 || kind == Hours300)
                {
                    last = candidates
                        .OrderByDescending(w => w.CounterMinutes)
                        .ThenByDescending(w => w.CreatedAt)
                        .FirstOrDefault();
                }
                else
                {
                    last = candidates
                        .OrderByDescending(w => DateTime.TryParse(w.Date, out var dt) ? dt : DateTime.MinValue)
                        .ThenByDescending(w => w.CreatedAt)
                        .FirstOrDefault();
                }

                var item = new WorkDueItem
                {
                    Kind = kind,
                    Title = GetKindTitle(kind),
                    Last = last
                };

                if (last == null)
                {
                    item.State = DueState.NeedsBaseline;
                    item.StatusText = LocalizationManager.Get("Journal_NoBaselineTriplet");
                    item.StatusColorHex = "#888888";
                    result.Add(item);
                    continue;
                }

                if (kind == Hours50 || kind == Hours100 || kind == Hours300)
                {
                    var (offLow, offNom, offUp) = kind switch
                    {
                        Hours50 => (45L, 50L, 60L),
                        Hours100 => (90L, 100L, 120L),
                        _ => (285L, 300L, 330L)
                    };

                    long lowerM = last.CounterMinutes + offLow * 60;
                    long nominalM = last.CounterMinutes + offNom * 60;
                    long upperM = last.CounterMinutes + offUp * 60;

                    item.Lower = FormatHours(lowerM);
                    item.Nominal = FormatHours(nominalM);
                    item.Upper = FormatHours(upperM);

                    long currentM = counterMinutes;
                    if (currentM < lowerM)
                        item.State = DueState.Later;
                    else if (currentM > upperM)
                        item.State = DueState.Overdue;
                    else
                        item.State = DueState.Due;

                    long elapsedM = counterMinutes - last.CounterMinutes;
                    long totalM = offUp * 60;
                    int third = 0;
                    if (elapsedM * 3 < totalM) third = 0;
                    else if (elapsedM * 3 < totalM * 2) third = 1;
                    else third = 2;

                    item.StatusColorHex = third switch
                    {
                        0 => "#2E7D32", // Зеленый
                        1 => "#F9A825", // Желтый
                        _ => "#C62828"  // Красный
                    };

                    long roundHours = (long)Math.Round(elapsedM / 60.0);
                    item.StatusText = $"{offLow}–{offNom}–{offUp} ({roundHours}) ч";
                }
                else
                {
                    if (!DateTime.TryParse(last.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var baseDate))
                    {
                        DateTime.TryParse(last.Date, out baseDate);
                    }

                    DateTime bLow, bNom, bUp;
                    if (kind == Periodic)
                    {
                        bLow = baseDate.AddDays(15);
                        bNom = baseDate.AddDays(20);
                        bUp = baseDate.AddDays(25);
                    }
                    else if (kind == Reg12)
                    {
                        bLow = baseDate.AddMonths(12);
                        bNom = baseDate.AddMonths(12);
                        bUp = baseDate.AddMonths(13);
                    }
                    else
                    {
                        bLow = baseDate.AddMonths(24);
                        bNom = baseDate.AddMonths(24);
                        bUp = baseDate.AddMonths(26);
                    }

                    item.Lower = bLow.ToString("yyyy-MM-dd");
                    item.Nominal = bNom.ToString("yyyy-MM-dd");
                    item.Upper = bUp.ToString("yyyy-MM-dd");

                    if (today.Date < bLow.Date)
                        item.State = DueState.Later;
                    else if (today.Date > bUp.Date)
                        item.State = DueState.Overdue;
                    else
                        item.State = DueState.Due;

                    long totalDays = (long)(bUp.Date - baseDate.Date).TotalDays;
                    long elapsedDays = (long)(today.Date - baseDate.Date).TotalDays;
                    long lowDays = (long)(bLow.Date - baseDate.Date).TotalDays;
                    long nomDays = (long)(bNom.Date - baseDate.Date).TotalDays;
                    long upDays = (long)(bUp.Date - baseDate.Date).TotalDays;

                    int third = 0;
                    if (elapsedDays * 3 < totalDays) third = 0;
                    else if (elapsedDays * 3 < totalDays * 2) third = 1;
                    else third = 2;

                    item.StatusColorHex = third switch
                    {
                        0 => "#2E7D32",
                        1 => "#F9A825",
                        _ => "#C62828"
                    };

                    item.StatusText = $"{lowDays}–{nomDays}–{upDays} ({elapsedDays}) дн";
                }

                result.Add(item);
            }

            return result;
        }

        public static List<MaintenanceWork> FilterActiveWorks(List<MaintenanceWork> works)
        {
            var activeWorks = new List<MaintenanceWork>();
            foreach (var kind in Kinds)
            {
                var candidates = works.Where(w => w.Kind == kind).ToList();
                MaintenanceWork? last;
                if (kind == Hours50 || kind == Hours100 || kind == Hours300)
                {
                    last = candidates.OrderByDescending(w => w.CounterMinutes)
                                     .ThenByDescending(w => w.CreatedAt)
                                     .FirstOrDefault();
                }
                else
                {
                    last = candidates.OrderByDescending(w => DateTime.TryParse(w.Date, out var dt) ? dt : DateTime.MinValue)
                                     .ThenByDescending(w => w.CreatedAt)
                                     .FirstOrDefault();
                }

                if (last != null && !activeWorks.Any(w => w.Id == last.Id))
                {
                    activeWorks.Add(last);
                }
            }
            return activeWorks;
        }
    }
}