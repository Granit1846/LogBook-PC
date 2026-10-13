using logbook.Helpers;
using logbook.Services;
using logbook.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace logbook.Views
{
    public partial class PrintWindow : Window
    {
        private readonly MainViewModel _mainVm;
        private readonly int _currentTabIndex;

        private List<PrintService.PageDescriptor> _allDescriptors = new();
        private ObservableCollection<PageItem> _pageItems = new();

        private bool _isInitializing = true;

        public PrintWindow(MainViewModel mainVm, int currentTabIndex)
        {
            InitializeComponent();
            _mainVm = mainVm;
            _currentTabIndex = currentTabIndex;

            Loaded += (s, e) =>
            {
                logbook.Helpers.DarkTitleBarHelper.Apply(this);
                _isInitializing = false;
                Rebuild();
            };

            PagesListBox.ItemsSource = _pageItems;
        }

        private void Rebuild()
        {
            try
            {
                var tabs = new List<PrintService.TabPrintData>();

                if (AllTabsRadio.IsChecked == true)
                {
                    tabs.Add(BuildTabChast1());
                    tabs.Add(BuildTabVr80());
                    tabs.Add(BuildTabTa14());
                    tabs.Add(BuildTabVk250001());
                    tabs.Add(BuildTabVk250002());
                    tabs.Add(BuildTabResursy());
                    tabs.Add(BuildTabResursyOils());
                }
                else
                {
                    tabs.AddRange(BuildTabsByIndex(_currentTabIndex));
                }

                _allDescriptors = PrintService.BuildPageDescriptors(tabs);

                _pageItems.Clear();
                for (int i = 0; i < _allDescriptors.Count; i++)
                {
                    var desc = _allDescriptors[i];
                    string name = $"{desc.Tab.Title} — {LocalizationManager.Get("Print_Page")} {desc.PageNumberInTab}/{desc.TotalPagesInTab}";
                    _pageItems.Add(new PageItem
                    {
                        Index = i,
                        DisplayName = name,
                        IsSelected = true
                    });
                }

                PagesInfoText.Text = LocalizationManager.Format("Print_TotalPages", _allDescriptors.Count);
                UpdateSelectedCount();

                UpdatePreview();

                if (_pageItems.Count > 0)
                    PagesListBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationManager.Get("Msg_PageBuildError")}:\n{ex.Message}\n\n{ex.StackTrace}",
                    LocalizationManager.Get("Msg_Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePreview()
        {
            try
            {
                var selected = _allDescriptors
                    .Where((d, i) => i < _pageItems.Count && _pageItems[i].IsSelected)
                    .ToList();

                if (selected.Count == 0)
                {
                    PreviewViewer.Document = null;
                    return;
                }

                var doc = PrintService.BuildDocument(selected);
                PreviewViewer.Document = doc;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationManager.Get("Msg_PreviewError")}:\n{ex.Message}\n\n{ex.StackTrace}",
                    LocalizationManager.Get("Msg_Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        // ==================== События UI ====================
        // ============================================================

        private void Options_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            Rebuild();
        }

        private void PageCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            UpdateSelectedCount();
            UpdatePreview();
        }

        private void PagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _pageItems) item.IsSelected = true;
            UpdateSelectedCount();
            UpdatePreview();
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _pageItems) item.IsSelected = false;
            UpdateSelectedCount();
            UpdatePreview();
        }

        private void UpdateSelectedCount()
        {
            int count = _pageItems.Count(p => p.IsSelected);
            SelectedCountText.Text = LocalizationManager.Format("Print_SelectedCount", count);
        }

        // ============================================================
        // ==================== Печать ====================
        // ============================================================

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = _allDescriptors
                    .Where((d, i) => i < _pageItems.Count && _pageItems[i].IsSelected)
                    .ToList();

                if (selected.Count == 0)
                {
                    MessageBox.Show(
                        LocalizationManager.Get("Print_NoPages"),
                        LocalizationManager.Get("Print_Title"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string jobName = $"{LocalizationManager.Get("Print_JobName")} — {_mainVm.SelectedProfile}";
                PrintService.Print(selected, jobName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationManager.Get("Msg_PrintError")}:\n{ex.Message}\n\n{ex.StackTrace}",
                    LocalizationManager.Get("Msg_Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ============================================================
        // ==================== Построение данных ====================
        // ============================================================

        private List<PrintService.TabPrintData> BuildTabsByIndex(int index)
        {
            switch (index)
            {
                case 1: return new() { BuildTabChast1() };
                case 2: return new() { BuildTabVr80() };
                case 3: return new() { BuildTabTa14() };
                case 4: return new() { BuildTabVk250001() };
                case 5: return new() { BuildTabVk250002() };
                case 6: return new() { BuildTabResursy(), BuildTabResursyOils() };
                default: return new() { BuildTabChast1() };
            }
        }

        private PrintService.TabPrintData BuildTabChast1()
        {
            var widths = new double[] { 50, 56, 50, 50, 55, 55, 60, 50, 50, 60, 50, 50, 80, 80, 62, 30 };

            var cells = new List<PrintService.HeaderCell>
            {
                new() { Row = 0, Column = 0, RowSpan = 4, Text = LocalizationManager.Get("Header_Date"), IsVertical = true },
                new() { Row = 0, Column = 1, RowSpan = 4, Text = LocalizationManager.Get("Header_FlightType"), IsVertical = true },
                new() { Row = 0, Column = 2, RowSpan = 4, Text = $"{LocalizationManager.Get("Header_Chast1_Ground1")} {LocalizationManager.Get("Header_Chast1_Ground2")} {LocalizationManager.Get("Header_Chast1_Ground3")}", IsVertical = true },
                new() { Row = 0, Column = 3, RowSpan = 4, Text = $"{LocalizationManager.Get("Header_Chast1_Duration1")} {LocalizationManager.Get("Header_Chast1_Duration2")}", IsVertical = true },

                new() { Row = 0, Column = 4, ColumnSpan = 8, Text = LocalizationManager.Get("Header_TotalHours") },
                new() { Row = 0, Column = 12, ColumnSpan = 2, Text = LocalizationManager.Get("Header_Surname") },
                new() { Row = 0, Column = 14, RowSpan = 4, Text = LocalizationManager.Get("Header_Signature"), IsVertical = true },

                new() { Row = 1, Column = 4, ColumnSpan = 2, Text = LocalizationManager.Get("Header_ByFlight") },
                new() { Row = 1, Column = 6, ColumnSpan = 6, Text = LocalizationManager.Get("Header_ByLandings") },
                new() { Row = 1, Column = 12, RowSpan = 3, Text = LocalizationManager.Get("Header_Pilot"), IsVertical = true },
                new() { Row = 1, Column = 13, RowSpan = 3, Text = LocalizationManager.Get("Header_Mechanic"), IsVertical = true },

                new() { Row = 2, Column = 4, RowSpan = 2, Text = LocalizationManager.Get("Header_AfterRepair"), IsVertical = true },
                new() { Row = 2, Column = 5, RowSpan = 2, Text = LocalizationManager.Get("Header_SinceOp"), IsVertical = true },
                new() { Row = 2, Column = 6, ColumnSpan = 3, Text = LocalizationManager.Get("Header_AfterRepair") },
                new() { Row = 2, Column = 9, ColumnSpan = 3, Text = LocalizationManager.Get("Header_SinceOp") },

                new() { Row = 3, Column = 6, Text = LocalizationManager.Get("Header_Total"), IsVertical = true },
                new() { Row = 3, Column = 7, Text = LocalizationManager.Get("Header_FixedWing"), IsVertical = true },
                new() { Row = 3, Column = 8, Text = LocalizationManager.Get("Header_Rotorcraft"), IsVertical = true },
                new() { Row = 3, Column = 9, Text = LocalizationManager.Get("Header_Total"), IsVertical = true },
                new() { Row = 3, Column = 10, Text = LocalizationManager.Get("Header_FixedWing"), IsVertical = true },
                new() { Row = 3, Column = 11, Text = LocalizationManager.Get("Header_Rotorcraft"), IsVertical = true },
            };

            var rows = _mainVm.Chast1Records.Select(r => new[]
            {
                r.Date ?? "", r.FlightType ?? "", r.GroundHours ?? "", r.FlightDuration ?? "",
                r.Col5 ?? "", r.ColSNE ?? "", r.Col7 ?? "", r.Col8 ?? "", r.Col9 ?? "",
                r.Landings ?? "", r.Col11 ?? "", r.Landings2 ?? "",
                r.Pilot ?? "", r.Mechanic ?? "", r.Col15 ?? "", ""
            }).ToList();

            return new PrintService.TabPrintData
            {
                Title = $"{LocalizationManager.Get("Tab_Chast1")} — {_mainVm.SelectedProfile}",
                HeaderRows = 4,
                HeaderHeight = 170,
                ColumnWidths = widths,
                HeaderCells = cells,
                Rows = rows
            };
        }

        private PrintService.TabPrintData BuildTabVr80()
        {
            var widths = new double[] { 70, 70, 70, 50, 50, 50, 70, 50, 50, 60, 70, 50, 50, 50, 30 };

            var cells = new List<PrintService.HeaderCell>
            {
                new() { Row = 0, Column = 0, RowSpan = 4, Text = LocalizationManager.Get("Header_Date"), IsVertical = true },
                new() { Row = 0, Column = 1, RowSpan = 4, Text = LocalizationManager.Get("Header_StartCount"), IsVertical = true },
                new() { Row = 0, Column = 2, ColumnSpan = 4, Text = $"{LocalizationManager.Get("Header_Vr80_GroundWork1")} {LocalizationManager.Get("Header_Vr80_GroundWork2")}" },
                new() { Row = 0, Column = 6, ColumnSpan = 3, Text = $"{LocalizationManager.Get("Header_Vr80_AirWork1")} {LocalizationManager.Get("Header_Vr80_AirWork2")}" },
                new() { Row = 0, Column = 9, ColumnSpan = 2, Text = $"{LocalizationManager.Get("Header_Vr80_TotalEngine1")} {LocalizationManager.Get("Header_Vr80_TotalEngine2")} {LocalizationManager.Get("Header_Vr80_TotalEngine3")}" },
                new() { Row = 0, Column = 11, ColumnSpan = 2, Text = $"{LocalizationManager.Get("Header_Vr80_TotalTurbine1")} {LocalizationManager.Get("Header_Vr80_TotalTurbine2")} {LocalizationManager.Get("Header_Vr80_TotalTurbine3")}" },
                new() { Row = 0, Column = 13, RowSpan = 4, Text = LocalizationManager.Get("Header_Signature"), IsVertical = true },

                new() { Row = 1, Column = 2, ColumnSpan = 3, Text = LocalizationManager.Get("Header_Vr80_TwoEngines") },
                new() { Row = 1, Column = 5, RowSpan = 3, Text = LocalizationManager.Get("Header_Vr80_FromTurbine"), IsVertical = true },
                new() { Row = 1, Column = 6, RowSpan = 3, Text = LocalizationManager.Get("Header_Vr80_FullTime"), IsVertical = true },
                new() { Row = 1, Column = 7, ColumnSpan = 2, Text = LocalizationManager.Get("Header_Vr80_OnModes") },
                new() { Row = 1, Column = 9, RowSpan = 3, Text = LocalizationManager.Get("Header_AfterRepair"), IsVertical = true },
                new() { Row = 1, Column = 10, RowSpan = 3, Text = LocalizationManager.Get("Header_SinceOp"), IsVertical = true },
                new() { Row = 1, Column = 11, RowSpan = 3, Text = LocalizationManager.Get("Header_AfterRepair"), IsVertical = true },
                new() { Row = 1, Column = 12, RowSpan = 3, Text = LocalizationManager.Get("Header_SinceOp"), IsVertical = true },

                new() { Row = 2, Column = 2, RowSpan = 2, Text = LocalizationManager.Get("Header_Vr80_FullTime"), IsVertical = true },
                new() { Row = 2, Column = 3, ColumnSpan = 2, Text = LocalizationManager.Get("Header_Vr80_OnModes") },
                new() { Row = 2, Column = 7, RowSpan = 2, Text = LocalizationManager.Get("Header_Vr80_Nominal"), IsVertical = true },
                new() { Row = 2, Column = 8, RowSpan = 2, Text = LocalizationManager.Get("Header_Vr80_Takeoff"), IsVertical = true },

                new() { Row = 3, Column = 3, Text = LocalizationManager.Get("Header_Vr80_Nominal"), IsVertical = true },
                new() { Row = 3, Column = 4, Text = LocalizationManager.Get("Header_Vr80_Takeoff"), IsVertical = true },
            };

            var rows = _mainVm.Vr80Records.Select(r => new[]
            {
                r.Date ?? "", r.LaunchCount ?? "", r.GroundHours ?? "", r.Col4 ?? "",
                r.Col5 ?? "", r.Col6 ?? "", r.Col7 ?? "", r.Col8 ?? "", r.Col9 ?? "",
                r.Col24 ?? "", r.ColSNE ?? "", r.Col26 ?? "", r.Col27 ?? "", r.Col28 ?? "", ""
            }).ToList();

            return new PrintService.TabPrintData
            {
                Title = $"{LocalizationManager.Get("Tab_Vr80")} — {_mainVm.SelectedProfile}",
                HeaderRows = 4,
                HeaderHeight = 170,
                ColumnWidths = widths,
                HeaderCells = cells,
                Rows = rows
            };
        }

        private PrintService.TabPrintData BuildTabTa14()
        {
            var widths = new double[] { 70, 70, 100, 80, 90, 70, 65, 65, 70, 65, 90, 30 };

            var cells = new List<PrintService.HeaderCell>
            {
                new() { Row = 0, Column = 0, RowSpan = 2, Text = LocalizationManager.Get("Header_Date"), IsVertical = true },
                new() { Row = 0, Column = 1, RowSpan = 2, Text = LocalizationManager.Get("Header_StartCountShort"), IsVertical = true },
                new() { Row = 0, Column = 2, RowSpan = 2, Text = $"{LocalizationManager.Get("Header_Ta14_Counter1")} {LocalizationManager.Get("Header_Ta14_Counter2")} {LocalizationManager.Get("Header_Ta14_Counter3")}", IsVertical = true },
                new() { Row = 0, Column = 3, RowSpan = 2, Text = $"{LocalizationManager.Get("Header_Ta14_GroundWork1")} {LocalizationManager.Get("Header_Ta14_GroundWork2")} {LocalizationManager.Get("Header_Ta14_GroundWork3")}", IsVertical = true },
                new() { Row = 0, Column = 4, RowSpan = 2, Text = $"{LocalizationManager.Get("Header_Ta14_AirWork1")} {LocalizationManager.Get("Header_Ta14_AirWork2")} {LocalizationManager.Get("Header_Ta14_AirWork3")}", IsVertical = true },
                new() { Row = 0, Column = 5, RowSpan = 2, Text = $"{LocalizationManager.Get("Header_Ta14_Total1")} {LocalizationManager.Get("Header_Ta14_Total2")} {LocalizationManager.Get("Header_Ta14_Total3")}", IsVertical = true },
                new() { Row = 0, Column = 6, ColumnSpan = 4, Text = $"{LocalizationManager.Get("Header_Ta14_Grand1")} {LocalizationManager.Get("Header_Ta14_Grand2")}" },
                new() { Row = 0, Column = 10, RowSpan = 2, Text = LocalizationManager.Get("Header_Signature"), IsVertical = true },

                new() { Row = 1, Column = 6, ColumnSpan = 2, Text = $"{LocalizationManager.Get("Header_Ta14_AfterCap1")} {LocalizationManager.Get("Header_Ta14_AfterCap2")}" },
                new() { Row = 1, Column = 8, ColumnSpan = 2, Text = $"{LocalizationManager.Get("Header_Ta14_Since1")} {LocalizationManager.Get("Header_Ta14_Since2")}" },
            };

            var rows = _mainVm.Ta14Records.Select(r => new[]
            {
                r.Date ?? "", r.LaunchCount ?? "", r.Col3 ?? "", r.ColVremya ?? "",
                r.Col5 ?? "", r.ColSNE ?? "", r.Col7 ?? "", r.Col8 ?? "",
                r.ColSNE2 ?? "", r.LaunchCount2 ?? "", r.Col11 ?? "", ""
            }).ToList();

            return new PrintService.TabPrintData
            {
                Title = $"{LocalizationManager.Get("Tab_Ta14")} — {_mainVm.SelectedProfile}",
                HeaderRows = 2,
                HeaderHeight = 170,
                ColumnWidths = widths,
                HeaderCells = cells,
                Rows = rows
            };
        }

        private PrintService.TabPrintData BuildTabVk250001()
        {
            var widths = new double[] { 70, 60, 40, 60, 40, 70, 40, 40, 60, 40, 40, 40, 40, 60, 40, 60, 40, 70, 40, 50, 60, 60, 30 };

            var cells = BuildVkCells();

            var rows = _mainVm.Vk250001Records.Select(r => new[]
            {
                r.Date ?? "", r.I ?? "", "", r.G ?? "", "", r.A ?? "", "", "", r.B ?? "",
                "", "", "", "", r.D ?? "", "", r.E ?? "", "", r.H ?? "", "", ""
            }).ToList();

            return new PrintService.TabPrintData
            {
                Title = $"{LocalizationManager.Get("Tab_Vk01")} — {_mainVm.SelectedProfile}",
                HeaderRows = 3,
                HeaderHeight = 190,
                ColumnWidths = widths,
                HeaderCells = cells,
                Rows = rows
            };
        }

        private PrintService.TabPrintData BuildTabVk250002()
        {
            var widths = new double[] { 70, 60, 40, 60, 40, 70, 40, 40, 60, 40, 40, 40, 40, 60, 40, 60, 40, 70, 40, 50, 60, 60, 30 };

            var cells = BuildVkCells();

            var rows = _mainVm.Vk250002Records.Select(r => new[]
            {
                r.Date ?? "", r.I ?? "", "", r.G ?? "", "", r.A ?? "", "", "", r.B ?? "",
                "", "", "", "", r.D ?? "", "", r.E ?? "", "", r.H ?? "", "", ""
            }).ToList();

            return new PrintService.TabPrintData
            {
                Title = $"{LocalizationManager.Get("Tab_Vk02")} — {_mainVm.SelectedProfile}",
                HeaderRows = 3,
                HeaderHeight = 190,
                ColumnWidths = widths,
                HeaderCells = cells,
                Rows = rows
            };
        }

        private List<PrintService.HeaderCell> BuildVkCells()
        {
            return new List<PrintService.HeaderCell>
            {
                new() { Row = 0, Column = 0, RowSpan = 3, Text = LocalizationManager.Get("Header_Date"), IsVertical = true },
                new() { Row = 0, Column = 1, RowSpan = 3, Text = LocalizationManager.Get("Header_Vk_StartCountI"), IsVertical = true },
                new() { Row = 0, Column = 2, ColumnSpan = 17, Text = LocalizationManager.Get("Header_Vk_EngineHours") },

                new() { Row = 1, Column = 2, ColumnSpan = 2, Text = $"{LocalizationManager.Get("Header_Vk_Equivalent1")} {LocalizationManager.Get("Header_Vk_Equivalent2")} {LocalizationManager.Get("Header_Vk_Equivalent3")}" },
                new() { Row = 1, Column = 4, ColumnSpan = 3, Text = $"{LocalizationManager.Get("Header_Vk_TotalA1")} {LocalizationManager.Get("Header_Vk_TotalA2")} {LocalizationManager.Get("Header_Vk_TotalA3")}" },
                new() { Row = 1, Column = 7, ColumnSpan = 2, Text = $"{LocalizationManager.Get("Header_Vk_TakeoffMode1")} {LocalizationManager.Get("Header_Vk_TakeoffMode2")} {LocalizationManager.Get("Header_Vk_TakeoffMode3")}" },
                new() { Row = 1, Column = 9, ColumnSpan = 3, Text = $"{LocalizationManager.Get("Header_Vk_MaxMode1")} {LocalizationManager.Get("Header_Vk_MaxMode2")} {LocalizationManager.Get("Header_Vk_MaxMode3")}" },
                new() { Row = 1, Column = 12, ColumnSpan = 2, Text = $"{LocalizationManager.Get("Header_Vk_HighTemp1")} {LocalizationManager.Get("Header_Vk_HighTemp2")} {LocalizationManager.Get("Header_Vk_HighTemp3")}" },
                new() { Row = 1, Column = 14, ColumnSpan = 2, Text = $"{LocalizationManager.Get("Header_Vk_BarkOff1")} {LocalizationManager.Get("Header_Vk_BarkOff2")} {LocalizationManager.Get("Header_Vk_BarkOff3")}" },
                new() { Row = 1, Column = 16, ColumnSpan = 3, Text = $"{LocalizationManager.Get("Header_Vk_Cyclic1")} {LocalizationManager.Get("Header_Vk_Cyclic2")} {LocalizationManager.Get("Header_Vk_Cyclic3")}" },
                new() { Row = 0, Column = 19, RowSpan = 3, Text = LocalizationManager.Get("Header_Signature"), IsVertical = true },
                new() { Row = 0, Column = 20, RowSpan = 3, Text = LocalizationManager.Get("Header_CoeffA"), IsVertical = true },
                new() { Row = 0, Column = 21, RowSpan = 3, Text = LocalizationManager.Get("Header_CoeffH"), IsVertical = true },

                new() { Row = 2, Column = 3, Text = LocalizationManager.Get("Header_Vk_Total1"), IsVertical = true },
                new() { Row = 2, Column = 5, Text = LocalizationManager.Get("Header_Vk_SinceOp1") },
                new() { Row = 2, Column = 6, Text = LocalizationManager.Get("Header_Vk_AfterCap1"), IsVertical = true },
                new() { Row = 2, Column = 7, Text = LocalizationManager.Get("Header_Vk_From6To15_1") },
                new() { Row = 2, Column = 8, Text = LocalizationManager.Get("Header_Vk_Total2_1") },
                new() { Row = 2, Column = 9, Text = LocalizationManager.Get("Header_Vk_Total"), IsVertical = true },
                new() { Row = 2, Column = 10, Text = LocalizationManager.Get("Header_Vk_OnGround"), IsVertical = true },
                new() { Row = 2, Column = 11, Text = LocalizationManager.Get("Header_Vk_InFlight"), IsVertical = true },
                new() { Row = 2, Column = 13, Text = LocalizationManager.Get("Header_Vk_Total3_1") },
                new() { Row = 2, Column = 15, Text = LocalizationManager.Get("Header_Vk_Total4_1") },
                new() { Row = 2, Column = 17, Text = LocalizationManager.Get("Header_Vk_CyclicSince1") },
                new() { Row = 2, Column = 18, Text = LocalizationManager.Get("Header_Vk_AfterCap2_1"), IsVertical = true },
            };
        }

        private PrintService.TabPrintData BuildTabResursy()
        {
            var widths = new double[] { 140, 80, 80, 80, 90, 90, 100, 80, 80 };

            var cells = new List<PrintService.HeaderCell>
            {
                new() { Row = 0, Column = 0, Text = $"{LocalizationManager.Get("Resursy_ItemName1")} {LocalizationManager.Get("Resursy_ItemName2")}" },
                new() { Row = 0, Column = 1, Text = LocalizationManager.Get("Header_SNE") },
                new() { Row = 0, Column = 2, Text = $"{LocalizationManager.Get("Resursy_Assigned1")} {LocalizationManager.Get("Resursy_Assigned2")} {LocalizationManager.Get("Resursy_Assigned3")}" },
                new() { Row = 0, Column = 3, Text = $"{LocalizationManager.Get("Resursy_RemainingHh1")} {LocalizationManager.Get("Resursy_RemainingHh2")}" },
                new() { Row = 0, Column = 4, Text = $"{LocalizationManager.Get("Resursy_AssignedYears1")} {LocalizationManager.Get("Resursy_AssignedYears2")}" },
                new() { Row = 0, Column = 5, Text = LocalizationManager.Get("Header_IssueDate") },
                new() { Row = 0, Column = 6, Text = $"{LocalizationManager.Get("Resursy_RemainingDate1")} {LocalizationManager.Get("Resursy_RemainingDate2")}" },
                new() { Row = 0, Column = 7, Text = $"{LocalizationManager.Get("Resursy_InstalledResource1")} {LocalizationManager.Get("Resursy_InstalledResource2")} {LocalizationManager.Get("Resursy_InstalledResource3")}" },
                new() { Row = 0, Column = 8, Text = $"{LocalizationManager.Get("Resursy_BeforeInstall1")} {LocalizationManager.Get("Resursy_BeforeInstall2")} {LocalizationManager.Get("Resursy_BeforeInstall3")}" },
            };

            var rows = _mainVm.ResourceRecords.Select(r => new[]
            {
                r.Name ?? "", r.SNE ?? "", r.ExtendedTo1 ?? "", r.Remaining1 ?? "",
                r.YearsResource ?? "", r.IssueDate ?? "", r.Remaining2 ?? "",
                r.InstalledAtSNE1 ?? "", r.InstalledAtSNE2 ?? ""
            }).ToList();

            return new PrintService.TabPrintData
            {
                Title = $"{LocalizationManager.Get("Resursy_AggregatesTitle")} — {_mainVm.SelectedProfile}",
                HeaderRows = 1,
                HeaderHeight = 60,
                ColumnWidths = widths,
                HeaderCells = cells,
                Rows = rows
            };
        }

        private PrintService.TabPrintData BuildTabResursyOils()
        {
            var widths = new double[] { 100, 80, 90, 80, 80, 80, 60 };

            var cells = new List<PrintService.HeaderCell>
            {
                new() { Row = 0, Column = 0, Text = LocalizationManager.Get("Resursy_OilWhere") },
                new() { Row = 0, Column = 1, Text = $"{LocalizationManager.Get("Resursy_OilSNE1")} {LocalizationManager.Get("Resursy_OilSNE2")}" },
                new() { Row = 0, Column = 2, Text = $"{LocalizationManager.Get("Resursy_OilDate1")} {LocalizationManager.Get("Resursy_OilDate2")}" },
                new() { Row = 0, Column = 3, Text = $"{LocalizationManager.Get("Resursy_OilRem1")} {LocalizationManager.Get("Resursy_OilRem2")}" },
                new() { Row = 0, Column = 4, Text = $"{LocalizationManager.Get("Resursy_OilRemDate1")} {LocalizationManager.Get("Resursy_OilRemDate2")}" },
                new() { Row = 0, Column = 5, Text = $"{LocalizationManager.Get("Resursy_OilRes1")} {LocalizationManager.Get("Resursy_OilRes2")}" },
                new() { Row = 0, Column = 6, Text = $"{LocalizationManager.Get("Resursy_OilResY1")} {LocalizationManager.Get("Resursy_OilResY2")}" },
            };

            var rows = _mainVm.OilRecords.Select(r => new[]
            {
                r.Name ?? "", r.ByHours ?? "", r.ByYears ?? "",
                r.Remaining1 ?? "", r.Remaining2 ?? "",
                r.Coefficient ?? "", r.Term ?? ""
            }).ToList();

            return new PrintService.TabPrintData
            {
                Title = $"{LocalizationManager.Get("Resursy_OilsTitle")} — {_mainVm.SelectedProfile}",
                HeaderRows = 1,
                HeaderHeight = 60,
                ColumnWidths = widths,
                HeaderCells = cells,
                Rows = rows
            };
        }
    }

    /// <summary>
    /// Элемент списка страниц.
    /// </summary>
    public class PageItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;

        public int Index { get; set; }
        public string DisplayName { get; set; } = "";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}