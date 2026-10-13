using System;

namespace logbook.Models
{
    /// <summary>
    /// Запись о замене агрегата в профиле.
    /// </summary>
    public class AggregateReplacement
    {
        /// <summary>
        /// Тип: "Vr80", "Ta14", "Vk250001", "Vk250002".
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// Отображаемое имя: "ВР-80", ...
        /// </summary>
        public string DisplayName { get; set; } = "";

        public string OldNumber { get; set; } = "";
        public string NewNumber { get; set; } = "";
        public DateTime ReplacedAt { get; set; }
        public string Reason { get; set; } = "";
    }
}