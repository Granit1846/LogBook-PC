using System.Collections.Generic;

namespace logbook.Models
{
    /// <summary>
    /// Общая структура данных приложения.
    /// </summary>
    public class AppData
    {
        public logbook.Services.DesktopExchangeState Exchange { get; set; } = new();

        public Dictionary<string, ProfileData> Profiles { get; set; } = new();
        public Dictionary<string, AggregateData> Aggregates { get; set; } = new();
    }
}