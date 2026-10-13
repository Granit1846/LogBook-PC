namespace logbook.Models
{
    public class AppSettings
    {
        /// <summary>
        /// Путь к папке с данными.
        /// </summary>
        public string DataFolder { get; set; } = "";

        // === Интерфейс ===
        public string Language { get; set; } = "ru";
        public string Theme { get; set; } = "Light";
        public string FontSize { get; set; } = "Medium";
        public string FontFamily { get; set; } = "Segoe UI";
        public bool Notifications { get; set; } = true;

        // === Дополнительно ===
        public bool StartWithWindows { get; set; } = false;
        public bool MinimizeToTray { get; set; } = false;
        public bool AutoSave { get; set; } = true;
        public int UndoSize { get; set; } = 15;

        /// <summary>
        /// Проверять обновления автоматически при запуске.
        /// </summary>
        public bool AutoCheckUpdates { get; set; } = true;

        /// <summary>
        /// URL проекта Supabase.
        /// </summary>
        public string SupabaseUrl { get; set; } = "https://zjagjauwmpuzcwzidhkn.supabase.co";

        /// <summary>
        /// Публичный ключ Supabase (anon).
        /// </summary>
        public string SupabaseKey { get; set; } = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InpqYWdqYXV3bXB1emN3emlkaGtuIiwicm9sZSI6ImFub24iLCJpYXQiOjE3OTA1MjUwNTYsImV4cCI6MjEwNjEwMTA1Nn0.VIHdNpnEZUBVO6HvNabQIREj45lenfT6WpgkUHGjnQ4";

        public bool ShowDuplicateAggregateNotifications { get; set; } = true;
    }
}