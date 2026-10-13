using System;

namespace logbook.Models
{
    /// <summary>
    /// Запись о том, на каком борту стоял агрегат.
    /// </summary>
    public class AggregatePlacement
    {
        public string ProfileName { get; set; } = "";
        public DateTime InstalledAt { get; set; }
        public DateTime? RemovedAt { get; set; }

        /// <summary>
        /// Номер этого же агрегата, но изначально стоявшего на борту
        /// (если борт уже использовался с другим агрегатом).
        /// </summary>
        public string PreviousNumber { get; set; } = "";
    }
}