using logbook.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace logbook.Services
{
    /// <summary>
    /// Формирует печатные страницы формуляров и отправляет их на печать.
    /// </summary>
    public static class PrintService
    {
        // A4 landscape при 96 DPI
        public const double PageWidth = 1122.5;
        public const double PageHeight = 793.7;

        public const double MarginLeft = 30;
        public const double MarginTop = 30;
        public const double MarginRight = 30;
        public const double MarginBottom = 30;

        public const double ContentWidth = PageWidth - MarginLeft - MarginRight;
        public const double ContentHeight = PageHeight - MarginTop - MarginBottom;

        /// <summary>
        /// Описание одной ячейки шапки (с объединениями и поворотом).
        /// </summary>
        public class HeaderCell
        {
            public int Row { get; set; }           // с 0
            public int Column { get; set; }        // с 0
            public int RowSpan { get; set; } = 1;
            public int ColumnSpan { get; set; } = 1;
            public string Text { get; set; } = "";
            public bool IsVertical { get; set; }   // повернуть на -90°
            public bool IsBold { get; set; } = true;
        }

        /// <summary>
        /// Описание одной печатной вкладки.
        /// </summary>
        public class TabPrintData
        {
            public string Title { get; set; } = "";
            public int HeaderRows { get; set; } = 4;                     // количество уровней в шапке
            public double HeaderHeight { get; set; } = 170;              // высота шапки в px
            public double[] ColumnWidths { get; set; } = Array.Empty<double>();
            public List<HeaderCell> HeaderCells { get; set; } = new();
            public List<string[]> Rows { get; set; } = new();
        }

        /// <summary>
        /// Описание одной страницы.
        /// </summary>
        public class PageDescriptor
        {
            public TabPrintData Tab { get; set; } = new();
            public List<string[]> Rows { get; set; } = new();
            public bool IsFirstPageOfTab { get; set; }
            public int PageNumberInTab { get; set; }
            public int TotalPagesInTab { get; set; }
        }

        /// <summary>
        /// Считает, сколько строк данных влезает на страницу.
        /// </summary>
        public static int GetRowsPerPage(TabPrintData tab)
        {
            double titleHeight = 24; // заголовок вкладки на первой странице
            double available = ContentHeight - tab.HeaderHeight - titleHeight - 16;
            return Math.Max(1, (int)Math.Floor(available / 22));
        }

        /// <summary>
        /// Разбивает данные на страницы.
        /// </summary>
        public static List<PageDescriptor> BuildPageDescriptors(IEnumerable<TabPrintData> tabs)
        {
            var result = new List<PageDescriptor>();

            foreach (var tab in tabs)
            {
                int rowsPerPage = GetRowsPerPage(tab);
                var chunks = new List<List<string[]>>();

                if (tab.Rows.Count == 0)
                {
                    chunks.Add(new List<string[]>());
                }
                else
                {
                    for (int i = 0; i < tab.Rows.Count; i += rowsPerPage)
                        chunks.Add(tab.Rows.Skip(i).Take(rowsPerPage).ToList());
                }

                for (int i = 0; i < chunks.Count; i++)
                {
                    result.Add(new PageDescriptor
                    {
                        Tab = tab,
                        Rows = chunks[i],
                        IsFirstPageOfTab = i == 0,
                        PageNumberInTab = i + 1,
                        TotalPagesInTab = chunks.Count
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Строит FixedPage.
        /// </summary>
        public static FixedPage BuildFixedPage(PageDescriptor desc)
        {
            var page = new FixedPage
            {
                Width = PageWidth,
                Height = PageHeight,
                Background = Brushes.White
            };

            var root = new StackPanel { Width = ContentWidth };

            // Заголовок вкладки (только на первой странице вкладки)
            if (desc.IsFirstPageOfTab)
            {
                root.Children.Add(new TextBlock
                {
                    Text = desc.Tab.Title,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black,
                    Margin = new Thickness(0, 0, 0, 6)
                });
            }

            // Шапка
            root.Children.Add(BuildComplexHeader(desc.Tab));

            // Данные
            root.Children.Add(BuildData(desc.Tab, desc.Rows));

            // Номер страницы
            root.Children.Add(new TextBlock
            {
                Text = $"Страница {desc.PageNumberInTab} из {desc.TotalPagesInTab}",
                FontSize = 9,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 4, 4, 0)
            });

            FixedPage.SetLeft(root, MarginLeft);
            FixedPage.SetTop(root, MarginTop);
            page.Children.Add(root);

            return page;
        }

        // ============================================================
        // ==================== Сложная шапка ====================
        // ============================================================

        private static Grid BuildComplexHeader(TabPrintData tab)
        {
            double totalWidth = tab.ColumnWidths.Sum();
            var grid = new Grid
            {
                Height = tab.HeaderHeight,
                Width = totalWidth,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = Brushes.LightGray,   // ← ВЕРНУТЬ
                ShowGridLines = false
            };

            // Колонки
            foreach (var w in tab.ColumnWidths)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(w) });

            // Строки
            for (int i = 0; i < tab.HeaderRows; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Ячейки
            foreach (var cell in tab.HeaderCells)
            {
                var tb = new TextBlock
                {
                    Text = cell.Text,
                    FontSize = 9,
                    FontWeight = cell.IsBold ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = Brushes.Black,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(1)
                };

                if (cell.IsVertical)
                {
                    tb.LayoutTransform = new RotateTransform(-90);
                }

                var border = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0.5),
                    Background = Brushes.LightGray,
                    Child = tb
                };

                Grid.SetRow(border, cell.Row);
                Grid.SetColumn(border, cell.Column);
                Grid.SetRowSpan(border, cell.RowSpan);
                Grid.SetColumnSpan(border, cell.ColumnSpan);

                grid.Children.Add(border);
            }

            return grid;
        }

        // ============================================================
        // ==================== Данные ====================
        // ============================================================

        private static Grid BuildData(TabPrintData tab, List<string[]> rows)
        {
            double totalWidth = tab.ColumnWidths.Sum();
            var grid = new Grid
            {
                Width = totalWidth,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            foreach (var w in tab.ColumnWidths)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(w) });

            for (int r = 0; r < rows.Count; r++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(22) });
                var row = rows[r];
                for (int c = 0; c < tab.ColumnWidths.Length; c++)
                {
                    string text = c < row.Length ? row[c] : "";
                    var tb = new TextBlock
                    {
                        Text = text,
                        FontSize = 9,
                        Foreground = Brushes.Black,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(1)
                    };
                    var border = new Border
                    {
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0.5),
                        Background = Brushes.White,
                        Child = tb
                    };
                    Grid.SetRow(border, r);
                    Grid.SetColumn(border, c);
                    grid.Children.Add(border);
                }
            }

            return grid;
        }

        /// <summary>
        /// Строит FixedDocument.
        /// </summary>
        public static FixedDocument BuildDocument(IEnumerable<PageDescriptor> descriptors)
        {
            var doc = new FixedDocument();
            doc.DocumentPaginator.PageSize = new Size(PageWidth, PageHeight);

            foreach (var desc in descriptors)
            {
                var page = BuildFixedPage(desc);
                var content = new PageContent();
                ((System.Windows.Markup.IAddChild)content).AddChild(page);
                doc.Pages.Add(content);
            }

            return doc;
        }

        /// <summary>
        /// Печать.
        /// </summary>
        public static void Print(IEnumerable<PageDescriptor> descriptors, string jobName)
        {
            var list = descriptors.ToList();
            if (list.Count == 0)
            {
                MessageBox.Show(
    LocalizationManager.Get("Print_NoPages"),
    LocalizationManager.Get("Print_Title"),
    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var printDialog = new PrintDialog();
            printDialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
            printDialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);

            if (printDialog.ShowDialog() != true)
                return;

            var doc = BuildDocument(list);
            printDialog.PrintDocument(doc.DocumentPaginator, jobName);
        }
    }
}