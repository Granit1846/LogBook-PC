using System;
using System.Collections.Generic;

namespace logbook.Models
{
    /// <summary>
    /// Ресурсы борта (вкладка «Ресурсы») для синхронизации.
    /// Привязаны к номеру борта (AggregateAssignments["Chast1"]).
    /// </summary>
    public class ResourcesData
    {
        public string BoardNumber { get; set; } = "";

        public List<ResourceRecord> ResourceRecords { get; set; } = new();

        public List<OilRecord> OilRecords { get; set; } = new();

        /// <summary>
        /// Время последнего изменения (UTC). Используется для разрешения конфликтов.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}