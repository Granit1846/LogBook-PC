using System;
using System.Collections.Generic;

namespace logbook.Models
{
    /// <summary>
    /// Данные агрегата (двигатель, редуктор и т.д.) — глобальный каталог.
    /// </summary>
    public class AggregateData
    {
        public string ExchangeId { get; set; } = "";

        /// <summary>
        /// Уникальный номер агрегата (серийник).
        /// </summary>
        public string Number { get; set; } = "";

        /// <summary>
        /// Тип: "Vr80", "Ta14", "Vk250001", "Vk250002".
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// Отображаемое имя: "ВР-80", "Та-14", ...
        /// </summary>
        public string DisplayName { get; set; } = "";

        // === ВР-80 ===
        public List<Vr80Record> Vr80Records { get; set; } = new();
        public Vr80Record Vr80StartRecord { get; set; } = new();

        // === Та-14 ===
        public List<Ta14Record> Ta14Records { get; set; } = new();
        public Ta14Record Ta14StartRecord { get; set; } = new();

        // === ВК-2500-01 ===
        public List<Vk250001Record> Vk250001Records { get; set; } = new();
        public Vk250001Record Vk250001StartRecord { get; set; } = new();
        public string Vk250001CoeffA { get; set; } = "";
        public string Vk250001CoeffH { get; set; } = "";

        // === ВК-2500-02 ===
        public List<Vk250002Record> Vk250002Records { get; set; } = new();
        public Vk250002Record Vk250002StartRecord { get; set; } = new();
        public string Vk250002CoeffA { get; set; } = "";
        public string Vk250002CoeffH { get; set; } = "";

        /// <summary>
        /// История установок (на каких бортах стоял).
        /// </summary>
        public List<AggregatePlacement> PlacementHistory { get; set; } = new();
    }
}