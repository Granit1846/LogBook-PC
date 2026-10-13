using System.Collections.Generic;

namespace logbook.Models
{
    public class ProfileData
    {
        public string ExchangeId { get; set; } = "";

        public string ProfileName { get; set; }

        public Dictionary<string, DayData> Days { get; set; }
            = new Dictionary<string, DayData>();

        // === Часть 1 ===
        public List<Chast1Record> Chast1Records { get; set; }
            = new List<Chast1Record>();

        public Chast1Record Chast1StartRecord { get; set; }
            = new Chast1Record();

        // === ВР-80 ===
        public List<Vr80Record> Vr80Records { get; set; }
            = new List<Vr80Record>();

        public Vr80Record Vr80StartRecord { get; set; }
            = new Vr80Record();

        // === Та-14 ===
        public List<Ta14Record> Ta14Records { get; set; }
            = new List<Ta14Record>();

        public Ta14Record Ta14StartRecord { get; set; }
            = new Ta14Record();

        // === ВК-2500-01 ===
        public List<Vk250001Record> Vk250001Records { get; set; }
            = new List<Vk250001Record>();

        public Vk250001Record Vk250001StartRecord { get; set; }
            = new Vk250001Record();

        public string Vk250001CoeffA { get; set; } = "";
        public string Vk250001CoeffH { get; set; } = "";

        // === ВК-2500-02 ===
        public List<Vk250002Record> Vk250002Records { get; set; }
            = new List<Vk250002Record>();

        public Vk250002Record Vk250002StartRecord { get; set; }
            = new Vk250002Record();

        public string Vk250002CoeffA { get; set; } = "";
        public string Vk250002CoeffH { get; set; } = "";

        /// <summary>
        /// Была ли выполнена хотя бы одна синхронизация с сервером.
        /// </summary>
        public bool HasBeenSynced { get; set; } = false;

        // === Агрегаты ===
        /// <summary>
        /// Привязки агрегатов: тип → номер.
        /// "Vr80" → "12345", "Ta14" → "67890", ...
        /// </summary>
        public Dictionary<string, string> AggregateAssignments { get; set; } = new();

        /// <summary>
        /// История замен агрегатов.
        /// </summary>
        public List<AggregateReplacement> AggregateHistory { get; set; } = new();

        /// <summary>
        /// Включена ли синхронизация для профиля.
        /// false — если пользователь отказался заполнить номера агрегатов.
        /// </summary>
        public bool SyncEnabled { get; set; } = true;

        // === Профиль ===
        public string IconName { get; set; } = "1.ico";

        // === Ресурсы ===
        public List<ResourceRecord> ResourceRecords { get; set; } = new List<ResourceRecord>();
        public List<OilRecord> OilRecords { get; set; } = new List<OilRecord>();

        /// <summary>
        /// Время последнего изменения ресурсов (UTC). Для синхронизации.
        /// </summary>
        public DateTime ResourcesUpdatedAt { get; set; } = DateTime.MinValue;
        // === Журнал ТО ===
        public List<MaintenanceWork> MaintenanceWorks { get; set; } = new List<MaintenanceWork>();
        public List<DefectTask> DefectTasks { get; set; } = new List<DefectTask>();
    }
}