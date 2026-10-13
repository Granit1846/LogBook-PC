using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace logbook.Helpers
{
    /// <summary>
    /// Применяет тему, шрифт и размер текста ко всему приложению.
    /// </summary>
    public static class ThemeManager
    {
        private const string LightThemePath = "Themes/LightTheme.xaml";
        private const string DarkThemePath = "Themes/DarkTheme.xaml";
        private const string NexusThemePath = "Themes/NexusTheme.xaml";

        /// <summary>
        /// Текущая тема — тёмная?
        /// </summary>
        public static bool IsDarkTheme { get; private set; }

        /// <summary>
        /// Применить все настройки интерфейса.
        /// </summary>
        public static void Apply(string theme, string fontFamily, string fontSize)
        {
            var app = Application.Current;
            if (app == null) return;

            ApplyTheme(app, theme);
            ApplyFont(app, fontFamily, fontSize);
        }

        /// <summary>
        /// Применить тему — подменяет MergedDictionaries и дублирует кисти
        /// в Application.Resources, чтобы DynamicResource находил их мгновенно.
        /// </summary>
        private static void ApplyTheme(Application app, string theme)
        {
            if (string.IsNullOrWhiteSpace(theme))
                theme = "Light";

            // «Системная» → светлая (пока)
            if (theme == "Системная" || theme == "System")
                theme = "Light";

            bool isNexus = theme.Equals("Nexus", StringComparison.OrdinalIgnoreCase);
            bool isDark = isNexus || theme == "Тёмная" || theme == "Dark";
            IsDarkTheme = isDark;

            string newPath;
            if (isNexus)
                newPath = NexusThemePath;
            else if (isDark)
                newPath = DarkThemePath;
            else
                newPath = LightThemePath;

            // Ищем уже подключённый словарь тем (Light, Dark или Nexus)
            var existing = app.Resources.MergedDictionaries
                .FirstOrDefault(d =>
                {
                    var src = d.Source?.OriginalString ?? "";
                    return src.EndsWith(LightThemePath, StringComparison.OrdinalIgnoreCase)
                        || src.EndsWith(DarkThemePath, StringComparison.OrdinalIgnoreCase)
                        || src.EndsWith(NexusThemePath, StringComparison.OrdinalIgnoreCase);
                });

            // Если уже подключена нужная — всё равно дублируем кисти (на случай,
            // если их кто-то перезаписал) и выходим
            if (existing != null)
            {
                var currentSrc = existing.Source?.OriginalString ?? "";
                if (currentSrc.EndsWith(newPath, StringComparison.OrdinalIgnoreCase))
                {
                    CopyAllBrushes(app, existing);
                    return;
                }

                // Иначе — удаляем старую
                app.Resources.MergedDictionaries.Remove(existing);
            }

            // Загружаем новый словарь через pack:// — это гарантирует,
            // что словарь загружается немедленно, а не «откладывается»
            var dict = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/{newPath}", UriKind.Absolute)
            };
            app.Resources.MergedDictionaries.Add(dict);

            // Дублируем кисти в Application.Resources — тогда DynamicResource
            // в любом окне (созданном после Apply) найдёт их сразу
            CopyAllBrushes(app, dict);
        }

        /// <summary>
        /// Копирует все ключи из словаря темы в Application.Resources.
        /// </summary>
        private static void CopyAllBrushes(Application app, ResourceDictionary dict)
        {
            string[] keys =
            {
                "WindowBackgroundBrush",
                "WindowForegroundBrush",
                "CellBackgroundBrush",
                "CellForegroundBrush",
                "HeaderCellBackgroundBrush",
                "HeaderCellForegroundBrush",
                "StartCellBackgroundBrush",
                "StartCellForegroundBrush",
                "FixedCellBackgroundBrush",
                "FixedCellForegroundBrush",
                "BorderBrush",
                "GridLineBrush",
                "AccentBackgroundBrush",
                "AccentForegroundBrush",
                "WarningBackgroundBrush",
                "WarningForegroundBrush",
                "ResourceHeaderBrush",
                "ResourceHeaderForegroundBrush",
                "OilCellBackgroundBrush",
                "AlertBackgroundBrush",
                "WarningCellBackgroundBrush",
                "WarningCellForegroundBrush",
                "CriticalCellBackgroundBrush",
                "CriticalCellForegroundBrush",
                "RedTextBrush",
                "CaretBrush",
                "HeaderTextShadow"
            };

            foreach (var key in keys)
            {
                if (dict.Contains(key))
                    app.Resources[key] = dict[key];
            }
        }

        /// <summary>
        /// Применить шрифт и размер.
        /// </summary>
        private static void ApplyFont(Application app, string fontFamily, string fontSize)
        {
            if (string.IsNullOrWhiteSpace(fontFamily))
                fontFamily = "Segoe UI";

            double small, medium, large;

            switch (fontSize)
            {
                case "Маленький":
                case "Small":
                    small = 9; medium = 11; large = 13;
                    break;
                case "Средний":
                case "Medium":
                    small = 11; medium = 13; large = 15;
                    break;
                case "Большой":
                case "Large":
                    small = 13; medium = 15; large = 17;
                    break;
                case "Очень большой":
                case "VeryLarge":
                    small = 15; medium = 17; large = 19;
                    break;
                default:
                    small = 11; medium = 13; large = 15;
                    break;
            }

            app.Resources["AppFontFamily"] = new FontFamily(fontFamily);
            app.Resources["AppFontSizeSmall"] = small;
            app.Resources["AppFontSizeMedium"] = medium;
            app.Resources["AppFontSizeLarge"] = large;

            // Оставляем AppFontSize как синоним AppFontSizeMedium (для совместимости)
            app.Resources["AppFontSize"] = medium;
        }
    }
}