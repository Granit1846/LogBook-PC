namespace logbook.Models
{
    /// <summary>
    /// Сообщение для окна предупреждений.
    /// Используется и в ResourceAlertWindow (агрегаты), и в OilAlertWindow (масла).
    /// Порядок вывода в XAML:
    ///   ResourceAlertWindow: Prefix → Name → Time → Suffix
    ///   OilAlertWindow:      Prefix → Time → Middle → Name → Suffix
    /// </summary>
    public class AlertMessage
    {
        /// <summary>Начало сообщения (обычный текст).</summary>
        public string Prefix { get; set; } = "";

        /// <summary>Имя агрегата/масла (жирный).</summary>
        public string Name { get; set; } = "";

        /// <summary>Время/дни (красный + жирный).</summary>
        public string Time { get; set; } = "";

        /// <summary>Середина (обычный текст). Только для масел.</summary>
        public string Middle { get; set; } = "";

        /// <summary>Конец сообщения (обычный текст).</summary>
        public string Suffix { get; set; } = "";
    }
}