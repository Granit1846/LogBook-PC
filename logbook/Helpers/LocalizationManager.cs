using System;
using System.Linq;
using System.Windows;

namespace logbook.Helpers
{
    /// <summary>
    /// Управление локализацией — подмена языковых словарей.
    /// </summary>
    public static class LocalizationManager
    {
        private const string RuPath = "Localization/Lang.ru.xaml";
        private const string EnPath = "Localization/Lang.en.xaml";

        /// <summary>
        /// Текущий язык (ru/en).
        /// </summary>
        public static string CurrentLanguage { get; private set; } = "ru";

        /// <summary>
        /// Применить язык.
        /// </summary>
        public static event Action LanguageChanged;
        public static void Apply(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                language = "ru";

            language = language.ToLowerInvariant();
            if (language != "ru" && language != "en")
                language = "ru";

            CurrentLanguage = language;

            var app = Application.Current;
            if (app == null) return;

            // Удаляем старый языковой словарь
            var old = app.Resources.MergedDictionaries
                .FirstOrDefault(d =>
                {
                    var src = d.Source?.OriginalString ?? "";
                    return src.IndexOf("Lang.ru.xaml", StringComparison.OrdinalIgnoreCase) >= 0
                        || src.IndexOf("Lang.en.xaml", StringComparison.OrdinalIgnoreCase) >= 0;
                });

            if (old != null)
                app.Resources.MergedDictionaries.Remove(old);

            // Подключаем новый
            string path = language == "en" ? EnPath : RuPath;
            var dict = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/{path}", UriKind.Absolute)
            };
            app.Resources.MergedDictionaries.Add(dict);

            LanguageChanged?.Invoke();
        }

        /// <summary>
        /// Получить строку по ключу.
        /// </summary>
        public static string Get(string key)
        {
            var app = Application.Current;
            if (app == null) return key;

            var value = app.TryFindResource(key);
            return value as string ?? key;
        }

        /// <summary>
        /// Получить строку с форматированием.
        /// </summary>
        public static string Format(string key, params object[] args)
        {
            string template = Get(key);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }
    }
}