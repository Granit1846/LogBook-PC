using System;
using Newtonsoft.Json;

namespace logbook.Models
{
    public class MaintenanceWork
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("kind")]
        public string Kind { get; set; } = "";

        [JsonProperty("date")]
        public string Date { get; set; } = ""; // ISO YYYY-MM-DD

        [JsonProperty("counterMinutes")]
        public int CounterMinutes { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; } = "";

        [JsonProperty("createdAt")]
        public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public class DefectTask
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("description")]
        public string Description { get; set; } = "";

        [JsonProperty("workId")]
        public string WorkId { get; set; } = "";

        [JsonProperty("createdAt")]
        public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        [JsonProperty("completedAt")]
        public long CompletedAt { get; set; } = 0L;
    }

    public enum DueState
    {
        NeedsBaseline,
        Later,
        Due,
        Overdue
    }

    public class WorkDueItem
    {
        public string Kind { get; set; } = "";
        public string Title { get; set; } = "";
        public MaintenanceWork? Last { get; set; }
        public string? Lower { get; set; }
        public string? Nominal { get; set; }
        public string? Upper { get; set; }
        public DueState State { get; set; }
        public string StatusText { get; set; } = "";
        public string StatusColorHex { get; set; } = "#888888";
    }
}