using logbook.Helpers;
using logbook.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace logbook.ViewModels
{
    public partial class MainViewModel
    {
        // ============================================================
        // ==================== ЗАГРУЗКА / СОХРАНЕНИЕ =================
        // ============================================================

        private void LoadResourceRecords()
        {
            ResourceRecords.Clear();
            OilRecords.Clear();

            if (string.IsNullOrWhiteSpace(SelectedProfile)) return;

            var resources = _storage.GetResourceRecords(SelectedProfile);
            var oils = _storage.GetOilRecords(SelectedProfile);

            if (resources != null)
                foreach (var r in resources)
                    ResourceRecords.Add(r);

            if (oils != null)
            {
                foreach (var o in oils)
                {
                    if (!IsExchangeProfile && string.IsNullOrWhiteSpace(o.Term))
                        o.Term = "1";

                    OilRecords.Add(o);
                }
            }

            if (!IsExchangeProfile) RefreshSNE();
            if (!IsExchangeProfile) RecalculateResources();
            SubscribeItems(ResourceRecords);
            SubscribeItems(OilRecords);

            IsDirty = false;
        }

        private void SaveResourceRecords()
        {
            RecalculateResources();

            _storage.SaveResourceRecords(SelectedProfile, ResourceRecords.ToList());
            _storage.SaveOilRecords(SelectedProfile, OilRecords.ToList());
        }

        // ============================================================
        // ==================== ПРИВЯЗКА К БОРТУ ======================
        // ============================================================

        /// <summary>
        /// Собирает ResourcesData из текущего профиля.
        /// </summary>
        public ResourcesData BuildResourcesDataFromProfile(string boardNumber)
        {
            return new ResourcesData
            {
                BoardNumber = boardNumber,
                ResourceRecords = ResourceRecords.ToList(),
                OilRecords = OilRecords.ToList(),
                UpdatedAt = _storage.GetResourcesUpdatedAt(SelectedProfile)
            };
        }

        /// <summary>
        /// Применяет ResourcesData к текущему профилю.
        /// </summary>
        public void ApplyResourcesDataToProfile(ResourcesData res)
        {
            if (res == null) return;

            _storage.SaveResourceRecords(SelectedProfile, res.ResourceRecords ?? new List<ResourceRecord>());
            _storage.SaveOilRecords(SelectedProfile, res.OilRecords ?? new List<OilRecord>());

            LoadResourceRecords();
        }

        /// <summary>
        /// Загружает ресурсы по борту из локального sync или создаёт дефолтные для нового борта.
        /// </summary>
        public void ReloadResourcesFromBoard(string boardNumber)
        {
            if (string.IsNullOrWhiteSpace(boardNumber))
                return;

            var fromSync = _storage.LoadResourcesFromSync(boardNumber);

            if (fromSync != null)
            {
                ApplyResourcesDataToProfile(fromSync);
                _storage.SaveResourcesToSync(SelectedProfile, boardNumber);
                return;
            }

            // Новый борт — создаём дефолтные списки
            ResourceRecords.Clear();
            OilRecords.Clear();
            AddDefaultResourceRecords();
            AddDefaultOilRecords();

            RefreshSNE();
            RecalculateResources();
            SubscribeItems(ResourceRecords);
            SubscribeItems(OilRecords);

            _storage.SaveResourceRecords(SelectedProfile, ResourceRecords.ToList());
            _storage.SaveOilRecords(SelectedProfile, OilRecords.ToList());
            _storage.SaveResourcesToSync(SelectedProfile, boardNumber);
        }

        /// <summary>
        /// Крайнее время изменения ресурсов (для сравнения в синхронизации).
        /// </summary>
        private DateTime GetLatestResourcesDate(ResourcesData res)
        {
            return res?.UpdatedAt ?? DateTime.MinValue;
        }

        // ============================================================
        // ==================== ДОБАВЛЕНИЕ / УДАЛЕНИЕ =================
        // ============================================================

        private void AddResourceRecord()
        {
            TakeSnapshot();

            ResourceRecords.Add(new ResourceRecord
            {
                Name = LocalizationManager.Get("Resursy_NewItem"),
                SNE = "",
                ExtendedTo1 = "",
                Remaining1 = "",
                YearsResource = "",
                IssueDate = "",
                ExtendedTo2 = "",
                Remaining2 = "",
                InstalledAtSNE1 = "",
                InstalledAtSNE2 = "",
                Coefficient = ""
            });

            _storage.SaveResourceRecords(SelectedProfile, ResourceRecords.ToList());
        }

        private void RemoveResourceRecord(ResourceRecord record)
        {
            if (record == null) return;

            var result = MessageBox.Show(
                LocalizationManager.Format("Msg_DeleteRow", record.Name),
                LocalizationManager.Get("Msg_ConfirmDeletion"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            TakeSnapshot();

            ResourceRecords.Remove(record);
            _storage.SaveResourceRecords(SelectedProfile, ResourceRecords.ToList());
        }

        private void AddOilRecord()
        {
            TakeSnapshot();

            OilRecords.Add(new OilRecord
            {
                Name = LocalizationManager.Get("Resursy_NewOil"),
                ByHours = "",
                ByYears = "",
                Coefficient = "",
                Term = "1"
            });

            _storage.SaveOilRecords(SelectedProfile, OilRecords.ToList());
        }

        private void RemoveOilRecord(OilRecord record)
        {
            if (record == null) return;

            var result = MessageBox.Show(
                LocalizationManager.Format("Msg_DeleteRow", record.Name),
                LocalizationManager.Get("Msg_ConfirmDeletion"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            TakeSnapshot();

            OilRecords.Remove(record);
            SaveResourceRecords();
        }

        // ============================================================
        // ==================== ПЕРЕСЧЁТ РЕСУРСОВ =====================
        // ============================================================

        /// <summary>
        /// Пересчитывает ресурсы и масло по формулам Excel.
        /// НЕ сохраняет — сохранение отдельно.
        /// </summary>
        private void RecalculateResources()
        {
            bool wasRecalculating = _isRecalculating;
            _isRecalculating = true;
            try
            {
                // ============================================================
                // ============== Верхняя таблица (агрегаты) ==================
                // ============================================================
                string latestSNE = GetLatestSNE();
                int b3Min = ParseHM(latestSNE);

                for (int i = 0; i < ResourceRecords.Count; i++)
                {
                    var rec = ResourceRecords[i];

                    // ===== 1: СНЭ =====
                    if (i == 0)
                    {
                        rec.SNE = latestSNE;
                    }
                    else
                    {
                        int col7 = ParseHM(rec.InstalledAtSNE1);
                        int col8 = ParseHM(rec.InstalledAtSNE2);
                        int sne = b3Min - col7 + col8;
                        rec.SNE = MinutesToHM(sne);
                    }

                    // ===== 3: Остаток = Col2 − Col1 =====
                    int col1 = ParseHM(rec.SNE);
                    int col2 = ParseHM(rec.ExtendedTo1);
                    int rem1 = col2 - col1;

                    rec.Remaining1IsYellowBg = false;
                    rec.Remaining1IsRedText = false;
                    rec.Remaining1IsCritical = false;
                    rec.Remaining1IsNegative = false;

                    if (rem1 < 0)
                    {
                        rec.Remaining1 = "-" + MinutesToHM(Math.Abs(rem1));
                        rec.Remaining1IsNegative = true;
                    }
                    else
                    {
                        rec.Remaining1 = MinutesToHM(rem1);

                        int minutes100 = 100 * 60;
                        int minutes50 = 50 * 60;
                        int minutes25 = 25 * 60;

                        if (rem1 <= minutes25)
                            rec.Remaining1IsCritical = true;
                        else if (rem1 <= minutes50)
                            rec.Remaining1IsRedText = true;
                        else if (rem1 <= minutes100)
                            rec.Remaining1IsYellowBg = true;
                    }

                    // ===== 6: Остаток (дата) =====
                    if (string.IsNullOrWhiteSpace(rec.IssueDate) ||
                        string.IsNullOrWhiteSpace(rec.YearsResource))
                    {
                        rec.Remaining2 = "";
                        rec.Remaining2IsYellowBg = false;
                        rec.Remaining2IsRedText = false;
                        rec.Remaining2IsCritical = false;
                        rec.Remaining2IsNegative = false;
                    }
                    else
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

                            rec.Remaining2IsYellowBg = false;
                            rec.Remaining2IsRedText = false;
                            rec.Remaining2IsCritical = false;
                            rec.Remaining2IsNegative = false;

                            if (totalDays < 30)
                            {
                                rec.Remaining2 = totalDays >= 0
                                    ? $"{totalDays} дн."
                                    : $"{totalDays} дн.";
                            }
                            else
                            {
                                rec.Remaining2 = totalMonths < 0
                                    ? FormatYearsMonthsSigned(totalMonths)
                                    : FormatYearsMonths(totalMonths);
                            }

                            if (totalMonths < 0)
                                rec.Remaining2IsNegative = true;
                            else if (totalMonths <= 1)
                                rec.Remaining2IsCritical = true;
                            else if (totalMonths <= 2)
                                rec.Remaining2IsRedText = true;
                            else if (totalMonths <= 3)
                                rec.Remaining2IsYellowBg = true;
                        }
                        else
                        {
                            rec.Remaining2 = "";
                            rec.Remaining2IsYellowBg = false;
                            rec.Remaining2IsRedText = false;
                            rec.Remaining2IsCritical = false;
                            rec.Remaining2IsNegative = false;
                        }
                    }
                }

                // ============================================================
                // ============== Нижняя таблица (масло) ======================
                // ============================================================
                for (int i = 0; i < OilRecords.Count; i++)
                {
                    var oil = OilRecords[i];

                    // ===== 3: Остаток (чч:мм) =====
                    if (string.IsNullOrWhiteSpace(oil.ByHours))
                    {
                        oil.Remaining1 = LocalizationManager.Get("Resursy_NotProvided");
                        oil.Remaining1IsWarning = false;
                        oil.Remaining1IsCritical = false;
                        oil.Remaining1IsNegative = false;
                    }
                    else
                    {
                        int byHours = ParseHM(oil.ByHours);
                        int coeff = ParseHM(oil.Coefficient);
                        int dOil = byHours + coeff - b3Min;

                        oil.Remaining1IsWarning = false;
                        oil.Remaining1IsCritical = false;
                        oil.Remaining1IsNegative = false;

                        if (dOil < 0)
                        {
                            oil.Remaining1 = "-" + MinutesToHM(Math.Abs(dOil));
                            oil.Remaining1IsNegative = true;
                        }
                        else
                        {
                            oil.Remaining1 = MinutesToHM(dOil);

                            if (dOil <= 20 * 60)
                                oil.Remaining1IsCritical = true;
                            else if (dOil <= 50 * 60)
                                oil.Remaining1IsWarning = true;
                        }
                    }

                    // ===== 4: Остаток (Nг.Mмес. или N дн.) =====
                    if (string.IsNullOrWhiteSpace(oil.ByYears))
                    {
                        oil.Remaining2 = LocalizationManager.Get("Resursy_NotProvided");
                        oil.Remaining2IsWarning = false;
                        oil.Remaining2IsCritical = false;
                        oil.Remaining2IsNegative = false;
                    }
                    else
                    {
                        DateTime date;
                        if (TryParseDate(oil.ByYears, out date))
                        {
                            double termYears = ParseTermYears(oil.Term);
                            int termMonths = (int)Math.Round(termYears * 12);

                            DateTime byYearsPlusTerm = date.AddMonths(termMonths);

                            int totalDays = (int)Math.Round((byYearsPlusTerm - DateTime.Today).TotalDays);
                            int totalMonths = MonthsBetween(DateTime.Today, byYearsPlusTerm);

                            oil.Remaining2IsWarning = false;
                            oil.Remaining2IsCritical = false;
                            oil.Remaining2IsNegative = false;

                            if (totalDays < 30)
                            {
                                string daysSuffix = LocalizationManager.Get("Resursy_DaysShort");
                                oil.Remaining2 = $"{totalDays} {daysSuffix}";
                            }
                            else
                            {
                                if (totalMonths < 0)
                                    oil.Remaining2 = FormatYearsMonthsSigned(totalMonths);
                                else
                                    oil.Remaining2 = FormatYearsMonths(totalMonths);
                            }

                            if (totalMonths < 0)
                                oil.Remaining2IsNegative = true;
                            else if (totalMonths <= 1)
                                oil.Remaining2IsCritical = true;
                            else if (totalMonths <= 2)
                                oil.Remaining2IsWarning = true;
                        }
                        else
                        {
                            oil.Remaining2 = LocalizationManager.Get("Resursy_NotProvided");
                            oil.Remaining2IsWarning = false;
                            oil.Remaining2IsCritical = false;
                            oil.Remaining2IsNegative = false;
                        }
                    }
                }
            }
            finally
            {
                _isRecalculating = wasRecalculating;
            }
        }

        // ============================================================
        // ==================== ДЕФОЛТНЫЕ СПИСКИ ======================
        // ============================================================

        private void AddDefaultResourceRecords()
        {
            ResourceRecords.Add(new ResourceRecord
            {
                Name = "Фюзеляж",
                ExtendedTo1 = "1500:00",
                YearsResource = "20",
                IssueDate = "16.11.2015"
            });
            ResourceRecords.Add(new ResourceRecord
            {
                Name = "Колонка",
                ExtendedTo1 = "1450:00",
                InstalledAtSNE1 = "214:00"
            });
            ResourceRecords.Add(new ResourceRecord
            {
                Name = "ВР-80",
                ExtendedTo1 = "1200:00",
                YearsResource = "15",
                IssueDate = "30.04.2011",
                InstalledAtSNE1 = "795:48",
                InstalledAtSNE2 = "774:11"
            });

            ResourceRecords.Add(new ResourceRecord { Name = "Колонка", SNE = "1422:06", ExtendedTo1 = "1450:00", YearsResource = "10", IssueDate = "15.07.2022", ExtendedTo2 = "12.07.2032", Remaining2 = "5г.9мес.", InstalledAtSNE1 = "214:00", Coefficient = "3650" });
            ResourceRecords.Add(new ResourceRecord { Name = "ВР-80", SNE = "1186:29", ExtendedTo1 = "1200:00", YearsResource = "20", IssueDate = "30.04.2011", ExtendedTo2 = "25.04.2031", Remaining2 = "4г.7мес.", InstalledAtSNE1 = "795:48", InstalledAtSNE2 = "774:11", Coefficient = "7300" });
            ResourceRecords.Add(new ResourceRecord { Name = "ПВР-800", SNE = "1425:23", ExtendedTo1 = "1450:00", YearsResource = "15", IssueDate = "16.07.2015", ExtendedTo2 = "12.07.2030", Remaining2 = "3г.9мес.", InstalledAtSNE1 = "365:09", InstalledAtSNE2 = "582:26", Coefficient = "5475" });
            ResourceRecords.Add(new ResourceRecord { Name = "РС", SNE = "1011:17", ExtendedTo1 = "1100:00", YearsResource = "10", IssueDate = "27.04.2021", ExtendedTo2 = "25.04.2031", Remaining2 = "4г.7мес.", InstalledAtSNE1 = "833:49", InstalledAtSNE2 = "637:00", Coefficient = "3650" });
            ResourceRecords.Add(new ResourceRecord { Name = "Вал соединительный", SNE = "1208:06", ExtendedTo1 = "1500:00", YearsResource = "20", IssueDate = "15.07.2022", ExtendedTo2 = "10.07.2042", Remaining2 = "15г.9мес.", InstalledAtSNE1 = "0:00", InstalledAtSNE2 = "0:00", Coefficient = "7300" });
            ResourceRecords.Add(new ResourceRecord { Name = "Т.с. - 314", SNE = "469:06", ExtendedTo1 = "1000:00", YearsResource = "20", IssueDate = "15.07.2022", ExtendedTo2 = "10.07.2042", Remaining2 = "15г.9мес.", InstalledAtSNE1 = "739:00", Coefficient = "7300" });
            ResourceRecords.Add(new ResourceRecord { Name = "Т.с. - 039", SNE = "706:06", ExtendedTo1 = "1000:00", YearsResource = "20", IssueDate = "15.07.2022", ExtendedTo2 = "10.07.2042", Remaining2 = "15г.9мес.", InstalledAtSNE1 = "502:00", Coefficient = "7300" });
        }

        private void AddDefaultOilRecords()
        {
            OilRecords.Add(new OilRecord
            {
                Name = "Лев. Дв.",
                ByHours = "1066:26",
                ByYears = "10.09.2024",
                Coefficient = "0:00",
                Term = "2"
            });
            OilRecords.Add(new OilRecord
            {
                Name = "Пр. Дв.",
                ByHours = "1066:26",
                ByYears = "10.09.2024",
                Coefficient = "0:00",
                Term = "2"
            });
            OilRecords.Add(new OilRecord
            {
                Name = "ВР-80",
                ByHours = "774:11",
                ByYears = "05.04.2024",
                Coefficient = "0:00",
                Term = "1"
            });
            OilRecords.Add(new OilRecord
            {
                Name = "ПВР(Л)",
                ByHours = "1066:26",
                ByYears = "10.09.2024",
                Coefficient = "0:00",
                Term = "1"
            });
            OilRecords.Add(new OilRecord
            {
                Name = "ПВР(П)",
                ByHours = "1066:26",
                ByYears = "10.09.2024",
                Coefficient = "0:00",
                Term = "1"
            });
        }
    }
}