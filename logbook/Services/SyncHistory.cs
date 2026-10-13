using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace logbook.Services;
public record SyncHistoryEntry(long Time,string Method,string Direction,string Result,string Backup="");
public sealed class SyncHistory {
    private readonly string path;
    private static readonly object gate=new();
    public SyncHistory(string folder){path=Path.Combine(folder,"sync-history.json");}
    public List<SyncHistoryEntry> Read(){lock(gate){return File.Exists(path)?JsonConvert.DeserializeObject<List<SyncHistoryEntry>>(File.ReadAllText(path))??new():new();}}
    public void Add(SyncHistoryEntry entry){lock(gate){var rows=Read();rows.Insert(0,entry);AtomicFile.Write(path,Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(rows.Take(200),Formatting.Indented)));}}
}
