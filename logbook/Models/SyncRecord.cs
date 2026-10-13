using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace logbook.Models
{
    /// <summary>
    /// Универсальная запись для синхронизации.
    /// Одна строка в таблице aggregates или boards.
    /// </summary>
    [Table("sync_files")]
    public class SyncRecord : BaseModel
    {
        [PrimaryKey("kind", false)]
        [Column("kind")]
        public string Kind { get; set; } = "";

        [Column("type")]
        public string Type { get; set; } = "";

        [PrimaryKey("number", false)]
        [Column("number")]
        public string Number { get; set; } = "";

        [Column("data")]
        public string Data { get; set; } = "";

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}