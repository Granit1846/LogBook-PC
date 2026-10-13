using System.Collections.Generic;

namespace logbook.Models
{
    /// <summary>
    /// Данные борта (Часть 1) для синхронизации.
    /// Агрегаты от него не зависят — у них свои файлы.
    /// </summary>
    public class BoardData
    {
        public string Number { get; set; } = "";
        public List<Chast1Record> Chast1Records { get; set; } = new();
        public Chast1Record Chast1StartRecord { get; set; } = new();

        /// <summary>
        /// Привязки агрегатов (Часть 1 → ВР-80, Та-14, ВК-01, ВК-02).
        /// </summary>
        public Dictionary<string, string> AggregateAssignments { get; set; } = new();
    }
}