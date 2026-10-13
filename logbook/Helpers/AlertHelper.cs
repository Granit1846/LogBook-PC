namespace logbook.Helpers
{
    /// <summary>
    /// Помощник для показа уведомлений (с учётом настройки).
    /// </summary>
    public static class AlertHelper
    {
        public static bool Enabled { get; set; } = true;

        /// <summary>
        /// Показывает окно предупреждений, если включены уведомления.
        /// </summary>
        public static void ShowIfEnabled(System.Collections.Generic.List<logbook.Models.AlertMessage> messages)
        {
            if (!Enabled) return;
            if (messages == null || messages.Count == 0) return;

            var window = new logbook.Views.ResourceAlertWindow(messages);
            window.ShowDialog();
        }
    }
}