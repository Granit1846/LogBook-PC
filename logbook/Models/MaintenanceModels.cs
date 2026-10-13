using System;

namespace logbook.Models
{
    public class MaintenanceWork
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Kind { get; set; } = "";
        public string Date { get; set; } = ""; // ISO YYYY-MM-DD
        public int CounterMinutes { get; set; }
        public string Description { get; set; } = "";
        public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public class DefectTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Description { get; set; } = "";
        public string WorkId { get; set; } = "";
        public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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