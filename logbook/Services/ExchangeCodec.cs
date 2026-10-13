using logbook.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using logbook.Models;

namespace logbook.Services;

public sealed record SharedProfilePacket(string ProfileId,string Name,string BoardNumber,JObject Payload);

public sealed class DesktopSyncCheckpoint {
    public long BaseRevision { get; set; }
    public string BaseJson { get; set; } = "";
    public string PendingId { get; set; } = "";
    public string PendingJson { get; set; } = "";
    public long PendingRevision { get; set; }
    public long LastSuccess { get; set; }
}

public sealed class DesktopExchangeState
{
    public Dictionary<string,DesktopSyncCheckpoint> Checkpoints { get; set; } = new();
    private static string CheckpointKey(string endpoint,string account) => new JArray(endpoint,account).ToString(Formatting.None);
    public void SaveCheckpoint() {
        if (Endpoint.Length == 0) return;
        Checkpoints[CheckpointKey(Endpoint,Account)] = new DesktopSyncCheckpoint {
            BaseRevision=BaseRevision,BaseJson=BaseJson,PendingId=PendingId,PendingJson=PendingJson,
            PendingRevision=PendingRevision,LastSuccess=LastSuccess
        };
    }
    public void SelectCheckpoint(string endpoint,string account) {
        if (Endpoint == endpoint && Account == account) return;
        SaveCheckpoint();
        Checkpoints.TryGetValue(CheckpointKey(endpoint,account),out var checkpoint);
        Endpoint=endpoint; Account=account;
        BaseRevision=checkpoint?.BaseRevision ?? 0; BaseJson=checkpoint?.BaseJson ?? "";
        PendingId=checkpoint?.PendingId ?? ""; PendingJson=checkpoint?.PendingJson ?? "";
        PendingRevision=checkpoint?.PendingRevision ?? 0; LastSuccess=checkpoint?.LastSuccess ?? 0;
    }
    public long LastSuccessFor(string endpoint,string account) => Endpoint==endpoint && Account==account
        ? LastSuccess : Checkpoints.GetValueOrDefault(CheckpointKey(endpoint,account))?.LastSuccess ?? 0;
    public string LanPeer { get; set; } = "";
    public long LanRevision { get; set; }
    public string LanPacket { get; set; } = "";
    public Dictionary<string,LanReceipt> LanReceipts { get; set; } = new();
    public string Endpoint { get; set; } = "";
    public string Account { get; set; } = "";
    public long BaseRevision { get; set; }
    public string BaseJson { get; set; } = "";
    public string PendingId { get; set; } = "";
    public string PendingJson { get; set; } = "";
    public long PendingRevision { get; set; }
    public string Packet { get; set; } = "";
    public string NativeBaseline { get; set; } = "";
    public long LastSuccess { get; set; }
    public Dictionary<string,string> Identities { get; set; } = new();
}

/// <summary>Maps desktop models while retaining every Android field not represented by the desktop UI.</summary>
public static class ExchangeCodec
{
    private static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
    private static readonly string[] Types={"Chast1","Vr80","Ta14","Vk250001","Vk250002"};
    private static readonly JObject Schema=ReadSchema();
    public static IEnumerable<string> Tables => Schema.Properties().Select(p=>p.Name);
    private static JObject ReadSchema()
    {
        using var stream=typeof(ExchangeCodec).Assembly.GetManifestResourceStream("logbook.ExchangeSchema.json") ?? throw new InvalidOperationException(SyncText.T("Sync_4fe41a869f"));
        using var reader=new StreamReader(stream);return JObject.Parse(reader.ReadToEnd());
    }
    private sealed class NativeResolver: DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member,MemberSerialization serialization)
        {
            var p=base.CreateProperty(member,serialization);
            if(!p.Writable || p.PropertyName=="Exchange" || p.PropertyName=="HasBeenSynced" || p.PropertyName=="ResourcesUpdatedAt" || p.PropertyName!.Contains("IsYellow") || p.PropertyName.Contains("IsRed") || p.PropertyName.Contains("IsWarning") || p.PropertyName.Contains("IsCritical") || p.PropertyName.Contains("IsNegative")) p.Ignored=true;
            return p;
        }
    }
    public static JObject Native(AppData data)=>JObject.FromObject(data,JsonSerializer.Create(new JsonSerializerSettings { ContractResolver=new NativeResolver(),DateParseHandling=DateParseHandling.None }));
    public static bool Same(string a,string b)=>!string.IsNullOrWhiteSpace(a)&&!string.IsNullOrWhiteSpace(b)&&JToken.DeepEquals(Parse(a),Parse(b));
    public static JObject Parse(string text)
    {
        using var r=new JsonTextReader(new StringReader(text)) {DateParseHandling=DateParseHandling.None,FloatParseHandling=FloatParseHandling.Decimal,MaxDepth=128};
        return JObject.Load(r,new JsonLoadSettings {DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
    }
    public static string LegacyId(string kind,string key)
    {
        var bytes=MD5.HashData(Encoding.UTF8.GetBytes("LogBook/legacy/v1/"+kind+"/"+key));bytes[6]=(byte)((bytes[6]&15)|48);bytes[8]=(byte)((bytes[8]&63)|128);
        var hex=Convert.ToHexString(bytes).ToLowerInvariant();return $"{hex[..8]}-{hex[8..12]}-{hex[12..16]}-{hex[16..20]}-{hex[20..]}";
    }
    private static string Text(JObject o,string key)=>o[key]?.Type==JTokenType.Null ? "" : o[key]?.ToString().Trim() ?? "";
    private static string Id(JObject o,string fallback="")
    {
        var id=Text(o,"ExchangeId");if(id.Length==0) {id=fallback.Length>0 ? fallback : Guid.NewGuid().ToString();o["ExchangeId"]=id;}return id;
    }
    private static decimal Number(string value)
    {
        if(string.IsNullOrWhiteSpace(value)||value is "—" or "–" or "-") return 0;
        return decimal.Parse(value.Replace(',','.'),NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,Inv);
    }
    private static long Integer(string value)
    {
        var d=Number(value);if(decimal.Truncate(d)!=d) throw new InvalidDataException(SyncText.T("Sync_a67811df08")+value);return checked((long)d);
    }
    private static long Minutes(string value)
    {
        if(string.IsNullOrWhiteSpace(value)||value is "—" or "–" or "-") return 0;
        var m=Regex.Match(value,@"^(-?)(\d+):(\d{1,2})$");if(!m.Success||int.Parse(m.Groups[3].Value)>59) throw new InvalidDataException(SyncText.T("Sync_b1fb76cfb1")+value);
        var n=checked(long.Parse(m.Groups[2].Value)*60+int.Parse(m.Groups[3].Value));return m.Groups[1].Value=="-" ? -n : n;
    }
    private static string Time(long value)=>(value<0?"-":"")+(Math.Abs(value)/60).ToString(Inv)+":"+(Math.Abs(value)%60).ToString("00",Inv);
    private static DateTime Date(string value)
    {
        var parts=value.Split('.');if(parts.Length==3 && parts[2].Length==2) value=parts[0]+"."+parts[1]+".20"+parts[2];
        if(!DateTime.TryParseExact(value,new[]{"dd.MM.yyyy","yyyy-MM-dd"},Inv,DateTimeStyles.None,out var d)) throw new InvalidDataException(SyncText.T("Sync_9256336538")+value);return d;
    }
    private static string Iso(string value)=>Date(value).ToString("yyyy-MM-dd",Inv);
    private static string Local(string value)=>string.IsNullOrEmpty(value) ? value
        : MonthlyTotals.TryMonth(value,out var month) ? MonthlyTotals.Label(month)
        : Date(value).ToString("dd.MM.yyyy",Inv);
    public static bool HasSnkValue(string? val)
    {
        if (string.IsNullOrWhiteSpace(val) || val is "—" or "–" or "-") return false;
        val = val.Trim();
        if (val.Contains(':'))
        {
            var parts = val.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
                return (h * 60 + m) > 0;
            return false;
        }
        val = val.Replace(',', '.');
        if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
            return d > 0.00001;
        return false;
    }
    public static string CleanSnk(string? val, string label = "")
    {
        if (!HasSnkValue(val)) return "";
        var trimmed = val!.Trim();
        if (label.Equals("I", StringComparison.OrdinalIgnoreCase))
        {
            var cleaned = trimmed.Replace(',', '.');
            if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
            {
                long rounded = (long)Math.Round(d);
                return rounded > 0 ? rounded.ToString(Inv) : "";
            }
        }
        return trimmed;
    }
    private static string Table(string type,bool start)=>type.ToLowerInvariant()+ (start?"_start":"_records");
    private static string Field(string type,string field)=>type.StartsWith("Vk") ? field switch {"colSNE"=>"A","pprA"=>"A_PPR","pprH"=>"H_PPR",_=>char.ToUpperInvariant(field[0])+field[1..]} : char.ToUpperInvariant(field[0])+field[1..];
    private static bool IsTime(string type,string field)=>type switch {
        "Chast1"=>new[]{"groundHours","flightDuration","col5","colSNE"}.Contains(field),
        "Vr80"=>new[]{"groundHours","col6","col7","col24","colSNE","col26","col27"}.Contains(field),
        "Ta14"=>new[]{"colVremya","colSNE","col7","colSNE2"}.Contains(field),_=>field is "colSNE" or "pprA"};
    private static JObject Template(string table)=>(JObject)new JObject(((JObject)Schema[table]!).Properties().Select(p=>new JProperty(p.Name,p.Value["default"]!.DeepClone())));
    private static JObject Row(string table,string id,JObject old)
    {
        var row=old["tables"]?[table]?.OfType<JObject>().SingleOrDefault(x=>Text(x,"id")==id);
        var result=row!=null ? (JObject)row.DeepClone() : Template(table);
        foreach(var field in ((JObject)Schema[table]!).Properties())
            result[field.Name] ??= field.Value["default"]!.DeepClone();
        return result;
    }
    private static JObject Form(string type,JObject source,bool start,JObject profile,JObject wire)
    {
        foreach(var p in wire.Properties().ToList()) {
            var field=p.Name;if(new[]{"id","profileId","date","sortOrder","isTotal","totalMonth","isHistorical"}.Contains(field)) continue;
            if(field is "coeffA" or "coeffH") {wire[field]=Number(Text(profile,type+char.ToUpperInvariant(field[0])+field[1..])).ToString("0.0############################",Inv);continue;}
            var raw=Text(source,Field(type,field));var storage=Schema[Table(type,start)]![field]!["type"]!.ToString();
            if(field=="needsManualRecalc") wire[field]=(bool?)source["NeedsManualRecalc"]==true ? 1 : 0;
            else if(storage=="TEXT") wire[field]=raw;
            else if(IsTime(type,field)) wire[field]=Minutes(raw);
            else if(storage=="REAL") wire[field]=Number(raw).ToString("0.0############################",Inv);
            else wire[field]=Integer(raw=="0:00" ? "0" : raw);
        }
        var date=Text(source,"Date");if(start) wire["date"]=date.Length==0 ? "" : Iso(date);
        else {
            var isTotal=MonthlyTotals.TryMonth(date,out var monthDate);
            if(!isTotal && (bool?)source["IsTotal"]==true) {
                var totalDate=Date(date);monthDate=new DateTime(totalDate.Year,totalDate.Month,1);isTotal=true;
            }
            DateTime d;
            if(isTotal) {
                d=monthDate.AddMonths(1).AddDays(-1);
            } else d=Date(date);
            wire["date"]=isTotal ? MonthlyTotals.Label(monthDate) : d.ToString("yyyy-MM-dd",Inv);
            wire["sortOrder"]=(long)(d-new DateTime(1970,1,1)).TotalDays*2+(isTotal?1:0);
            wire["isTotal"]=isTotal?1:0;wire["totalMonth"]=isTotal ? d.ToString("yyyy-MM",Inv) : "";
        }
        return wire;
    }
    private static JObject Unform(string type,JObject wire,bool start,JObject previous,JObject profile)
    {
        var native=(JObject)previous.DeepClone();
        foreach(var p in wire.Properties()) {
            var field=p.Name;if(new[]{"id","profileId","sortOrder","totalMonth","isHistorical"}.Contains(field)) continue;
            if(field is "coeffA" or "coeffH") {profile[type+char.ToUpperInvariant(field[0])+field[1..]]=p.Value.ToString();continue;}
            if(field=="date") native["Date"]=Local(p.Value.ToString());
            else if(field is "isTotal" or "needsManualRecalc") native[Field(type,field)]=p.Value.Value<int>()!=0;
            else {
                var key=Field(type,field);
                var storage=Schema[Table(type,start)]![field]!["type"]!.ToString();
                var numeric=storage is "INTEGER" or "REAL";
                // Android displays numeric zero as an empty formular cell. Retain an
                // explicit desktop zero only when the original desktop cell was zero.
                var prior=Text(previous,key);
                var explicitZero=numeric && prior.Length>0 && prior is not "—" and not "–" and not "-"
                    && (IsTime(type,field) ? Minutes(prior)==0 : Number(prior=="0:00" ? "0" : prior)==0);
                native[key]=numeric && Number(p.Value.ToString())==0 && !explicitZero ? ""
                    : IsTime(type,field) ? Time(p.Value.Value<long>()) : p.Value.ToString();
            }
        }
        native["ExchangeId"]=Text(wire,"id");return native;
    }
    private static JArray Items(JObject profile,string key)=>profile[key] as JArray ?? new JArray();
    private static JObject Resource(JObject native,bool oil,int order)
    {
        var wire=Text(native,"ExchangeExtras").Length>0 ? Parse(Text(native,"ExchangeExtras")) : new JObject();
        var fields=oil ? new[]{"name","byHours","byYears","coefficient","term"} : new[]{"name","sne","extendedTo1","yearsResource","issueDate","extendedTo2","remaining1","remaining2","installedAtSne1","installedAtSne2","coefficient"};
        var empty=new JArray();
        foreach(var field in fields) {
            var key=field switch {"sne"=>"SNE","installedAtSne1"=>"InstalledAtSNE1","installedAtSne2"=>"InstalledAtSNE2",_=>char.ToUpperInvariant(field[0])+field[1..]};
            var raw=Text(native,key);
            var time=oil ? field is "byHours" or "coefficient" : new[]{"sne","extendedTo1","remaining1","installedAtSne1","installedAtSne2"}.Contains(field);
            if(time && (raw.Length==0 || raw is "—" or "–" or "-")) empty.Add(field);
            wire[field]=time ? new JValue(Minutes(raw)) : new JValue(raw);
        }
        wire["emptyFields"]=empty;
        wire["groupId"] ??= "";wire["pinned"] ??= false;wire["order"] ??= order;return wire;
    }
    private static JObject Unresource(JObject wire,bool oil)
    {
        var native=new JObject { ["ExchangeId"]=Guid.NewGuid().ToString(),["ExchangeExtras"]=wire.ToString(Formatting.None) };
        foreach(var p in wire.Properties()) {
            var field=p.Name;var key=field switch {"sne"=>"SNE","installedAtSne1"=>"InstalledAtSNE1","installedAtSne2"=>"InstalledAtSNE2",_=>char.ToUpperInvariant(field[0])+field[1..]};
            var time=oil ? field is "byHours" or "coefficient" : new[]{"sne","extendedTo1","remaining1","installedAtSne1","installedAtSne2"}.Contains(field);
            if(field is "groupId" or "pinned" or "order" or "emptyFields")continue;
            var missing=time && p.Value.Value<long>()==0 && (wire["emptyFields"] is not JArray marked || marked.Values<string>().Contains(field));
            native[key]=time ? (missing ? "" : Time(p.Value.Value<long>())) : p.Value.ToString();
        }
        return native;
    }
    public static JObject Encode(AppData data)
    {
        var native=Native(data);var state=data.Exchange;var old=state.Packet.Length>0 ? Parse(state.Packet) : new JObject();
        if(Same(native.ToString(Formatting.None),state.NativeBaseline)) { Validate(old); return old; }
        string MetaId(string key) {if(!state.Identities.TryGetValue(key,out var value))state.Identities[key]=value=Guid.NewGuid().ToString();return value;}
        var tables=new JObject(Tables.Select(t=>new JProperty(t,new JArray())));
        void Add(string table,JObject row)=>((JArray)tables[table]!).Add(row);
        var profiles=(JObject)native["Profiles"]!;
        foreach(var entry in profiles.Properties()) {
            var source=(JObject)entry.Value;var assignments=source["AggregateAssignments"] as JObject ?? new JObject();var board=Text(assignments,"Chast1");
            var sourceKey=board.Length>0 ? "board:"+board : "profile:"+entry.Name;
            var pid=Id(source,LegacyId("profile",sourceKey));var profile=Row("profiles",pid,old);profile["id"]=pid;
            profile["name"]=Text(source,"ProfileName").Length>0 ? Text(source,"ProfileName") : entry.Name;profile["boardNumber"]=board;
            profile["aggregateAssignmentsJson"]=assignments.ToString(Formatting.None);
            profile["resourcesJson"]=new JArray(Items(source,"ResourceRecords").OfType<JObject>().Select((r,i)=>Resource(r,false,i))).ToString(Formatting.None);
            profile["oilsJson"]=new JArray(Items(source,"OilRecords").OfType<JObject>().Select((r,i)=>Resource(r,true,i))).ToString(Formatting.None);
            var mwList = (source["MaintenanceWorks"] as JArray ?? new JArray()).OfType<JObject>().Select(w => new JObject {
                ["id"] = Text(w,"id").Length > 0 ? Text(w,"id") : (Text(w,"Id").Length > 0 ? Text(w,"Id") : Guid.NewGuid().ToString()),
                ["kind"] = Text(w,"kind").Length > 0 ? Text(w,"kind") : Text(w,"Kind"),
                ["date"] = Text(w,"date").Length > 0 ? Text(w,"date") : Text(w,"Date"),
                ["counterMinutes"] = w["counterMinutes"]?.Value<int>() ?? w["CounterMinutes"]?.Value<int>() ?? 0,
                ["description"] = Text(w,"description").Length > 0 ? Text(w,"description") : Text(w,"Description"),
                ["createdAt"] = w["createdAt"]?.Value<long>() ?? w["CreatedAt"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            profile["maintenanceWorksJson"] = new JArray(mwList).ToString(Formatting.None);

            var dtList = (source["DefectTasks"] as JArray ?? new JArray()).OfType<JObject>().Select(t => new JObject {
                ["id"] = Text(t,"id").Length > 0 ? Text(t,"id") : (Text(t,"Id").Length > 0 ? Text(t,"Id") : Guid.NewGuid().ToString()),
                ["description"] = Text(t,"description").Length > 0 ? Text(t,"description") : Text(t,"Description"),
                ["workId"] = Text(t,"workId").Length > 0 ? Text(t,"workId") : Text(t,"WorkId"),
                ["createdAt"] = t["createdAt"]?.Value<long>() ?? t["CreatedAt"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["completedAt"] = t["completedAt"]?.Value<long>() ?? t["CompletedAt"]?.Value<long>() ?? 0L
            });
            profile["defectTasksJson"] = new JArray(dtList).ToString(Formatting.None);
            var history=new JArray(Items(source,"AggregateHistory").OfType<JObject>().Select(h=> new JObject { ["type"]=Text(h,"Type"),["oldNumber"]=Text(h,"OldNumber"),["newNumber"]=Text(h,"NewNumber"),["reason"]=Text(h,"Reason"),["date"]=DateTimeOffset.TryParse(Text(h,"ReplacedAt"),out var stamp) && stamp.Year>=1900 ? stamp.ToUnixTimeMilliseconds() : 0,["installationDate"]="" }));
            var previousHistory=JArray.Parse(Text(profile,"aggregateHistoryJson"));
            foreach(var h in history.OfType<JObject>()) {
                var prior=previousHistory.OfType<JObject>().FirstOrDefault(p=>Text(p,"type")==Text(h,"type") && Text(p,"oldNumber")==Text(h,"oldNumber") && Text(p,"newNumber")==Text(h,"newNumber") && Text(p,"date")==Text(h,"date"));
                if(prior!=null)h["installationDate"]=prior["installationDate"]?.DeepClone() ?? "";
            }
            if(!JToken.DeepEquals(source["AggregateHistory"],state.NativeBaseline.Length>0 ? Parse(state.NativeBaseline)["Profiles"]?[entry.Name]?["AggregateHistory"] : null)) profile["aggregateHistoryJson"]=history.ToString(Formatting.None);
            foreach(var type in Types) {
                var start=source[type+"StartRecord"] as JObject;if(start!=null) {
                    var sid=Id(start);var row=Form(type,start,true,source,Row(Table(type,true),sid,old));row["id"]=sid;row["profileId"]=pid;Add(Table(type,true),row);
                }
                foreach(var record in Items(source,type+"Records").OfType<JObject>()) {
                    var rid=Id(record);var row=Form(type,record,false,source,Row(Table(type,false),rid,old));if(type!="Chast1" && old["tables"]?[Table(type,false)]?.OfType<JObject>().Any(r=>Text(r,"id")==rid)!=true)row["isHistorical"]=1;row["id"]=rid;row["profileId"]=pid;Add(Table(type,false),row);
                }
            }
            var sne=((JArray)tables["chast1_records"]!).OfType<JObject>().Where(r=>Text(r,"profileId")==pid && r["isTotal"]!.Value<int>()==0).OrderBy(r=>r["sortOrder"]!.Value<long>()).LastOrDefault()?["colSNE"] ?? ((JArray)tables["chast1_start"]!).OfType<JObject>().SingleOrDefault(r=>Text(r,"profileId")==pid)?["colSNE"] ?? 0;
            profile["fuselageSne"]=sne.DeepClone();Add("profiles",profile);
            foreach(var d in (source["Days"] as JObject ?? new JObject()).Properties()) {
                var day=(JObject)d.Value;var did=Id(day);var date=Iso(d.Name);var row=Row("days",did,old);
                row["id"]=did;row["profileId"]=pid;row["date"]=date;row["crew"]=Text(day,"Crew");
                row["isClosed"]=((JArray)tables["chast1_records"]!).OfType<JObject>().Any(r=>Text(r,"profileId")==pid && Text(r,"date")==date)?1:0;Add("days",row);
                var index=0;foreach(var launch in Items(day,"Launches").OfType<JObject>()) {
                    var lid=Id(launch);var lr=Row("launches",lid,old);lr["id"]=lid;lr["dayId"]=did;lr["order"]=index++;
                    foreach(var f in new[]{"vsu","zemlya","vozduh","tp","samolet"}) lr[f]=Integer(Text(launch,f switch {"tp"=>"TP","samolet"=>"SamoletLandings",_=>char.ToUpperInvariant(f[0])+f[1..]}));Add("launches",lr);
                }
                var snid=MetaId("snk:"+did);var sn=Row("snk_values",snid,old);sn["id"]=snid;sn["dayId"]=did;
                foreach(var label in new[]{"A","B","E","G","H","I"}) {
                    var value=Items(day,"SnkValues").OfType<JObject>().SingleOrDefault(v=>Text(v,"Label")==label) ?? new JObject();
                    foreach(var side in new[]{"Left","Right"})sn[label.ToLowerInvariant()+side]=Number(Text(value,side+"Value")).ToString("0.0############################",Inv);
                }Add("snk_values",sn);
            }
            var receiptId=MetaId("receipt:"+pid);var receipt=Row("desktop_imports",receiptId,old);
            receipt["id"]=receiptId;receipt["profileId"]=pid;receipt["sourceKey"]=sourceKey;receipt["sharedId"]=pid;
            var original=source.ToString(Formatting.None);receipt["originalJson"]=original;receipt["sourceHash"]=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(original))).ToLowerInvariant();Add("desktop_imports",receipt);
        }
        foreach(var entry in ((JObject)native["Aggregates"]!).Properties()) {
            var aggregate=(JObject)entry.Value;var type=Text(aggregate,"Type");if(!Types.Contains(type)||type=="Chast1")throw new InvalidDataException(SyncText.T("Sync_141dd8a756")+type);
            var canonical=type.StartsWith("Vk")?"Vk2500":type;var number=Text(aggregate,"Number");var aid=Id(aggregate,LegacyId("aggregate",canonical+":"+number));
            var row=Row("aggregate_archives",aid,old);row["id"]=aid;row["type"]=canonical;row["number"]=number;
            var baseline=state.NativeBaseline.Length>0 ? Parse(state.NativeBaseline)["Aggregates"]?[entry.Name] : null;
            if(row["formularJson"]!.ToString().Length==0 || !JToken.DeepEquals(aggregate,baseline)) {
                var start=Form(type,aggregate[type+"StartRecord"] as JObject ?? new JObject(),true,aggregate,Template(Table(type,true)));
                var records=new JArray(Items(aggregate,type+"Records").OfType<JObject>().Select(r=>Form(type,r,false,aggregate,Template(Table(type,false)))));
                var archive=Text(row,"formularJson").Length>0 ? Parse(Text(row,"formularJson")) : new JObject();
                archive["start"]=start;archive["records"]=records;archive["legacySource"]=aggregate.DeepClone();archive["sharedId"]=aid;
                row["formularJson"]=archive.ToString(Formatting.None);
            }Add("aggregate_archives",row);
        }
        var packet=new JObject {["format"]="LogBookExchange",["version"]=1,["tables"]=tables};
        foreach(var table in Tables) tables[table]=new JArray(((JArray)tables[table]!).OfType<JObject>().OrderBy(r=>Text(r,"id"),StringComparer.Ordinal));
        // Commit generated IDs back into the detached desktop snapshot before persisting pending state.
        var updated=native.ToObject<AppData>()!;data.Profiles=updated.Profiles;data.Aggregates=updated.Aggregates;
        Validate(packet);return packet;
    }

    public static List<SharedProfilePacket> SharedProfilePackets(AppData data)
    {
        var packet=Encode(data);var tables=(JObject)packet["tables"]!;var result=new List<SharedProfilePacket>();
        foreach(var profile in tables["profiles"]!.OfType<JObject>()) {
            var pid=Text(profile,"id");var dayIds=tables["days"]!.OfType<JObject>()
                .Where(r=>Text(r,"profileId")==pid).Select(r=>Text(r,"id")).ToHashSet();
            var numbers=new HashSet<string>(StringComparer.Ordinal);
            foreach(var value in Parse(Text(profile,"aggregateAssignmentsJson")).Children<JProperty>().Select(p=>p.Value.ToString()))
                if(value.Length>0)numbers.Add(value);
            foreach(var history in JArray.Parse(Text(profile,"aggregateHistoryJson")).OfType<JObject>())
                foreach(var key in new[]{"oldNumber","newNumber"})if(Text(history,key).Length>0)numbers.Add(Text(history,key));
            var selected=new JObject();
            foreach(var table in Tables) selected[table]=new JArray(tables[table]!.OfType<JObject>().Where(row=> table switch {
                "profiles"=>Text(row,"id")==pid,
                "launches" or "snk_values"=>dayIds.Contains(Text(row,"dayId")),
                "aggregate_archives"=>numbers.Contains(Text(row,"number")),
                _=>row["profileId"]!=null&&Text(row,"profileId")==pid
            }).Select(r=>r.DeepClone()));
            var one=new JObject {{"format","LogBookExchange"},{"version",1},{"tables",selected}};Validate(one);
            result.Add(new SharedProfilePacket(pid,Text(profile,"name"),Text(profile,"boardNumber"),one));
        }
        return result;
    }

    public static JObject MergeSharedProfiles(JObject current,IEnumerable<JObject> incomingPackets)
    {
        Validate(current);var target=(JObject)current["tables"]!;
        foreach(var raw in incomingPackets) {
            Validate(raw);var source=(JObject)raw["tables"]!;var profiles=source["profiles"] as JArray;
            if(profiles==null||profiles.Count!=1)throw new InvalidDataException(SyncText.T("Sync_shared_one_profile"));
            var incoming=(JObject)profiles[0]!;var pid=Text(incoming,"id");
            var oldDayIds=target["days"]!.OfType<JObject>().Where(r=>Text(r,"profileId")==pid).Select(r=>Text(r,"id")).ToHashSet();
            foreach(var table in Tables.Where(t=>t!="aggregate_archives")) target[table]=new JArray(target[table]!.OfType<JObject>().Where(row=>!(table switch {
                "profiles"=>Text(row,"id")==pid,
                "launches" or "snk_values"=>oldDayIds.Contains(Text(row,"dayId")),
                _=>row["profileId"]!=null&&Text(row,"profileId")==pid
            })).Select(r=>r.DeepClone()));
            var profile=(JObject)incoming.DeepClone();var name=Text(profile,"name");
            if(target["profiles"]!.OfType<JObject>().Any(p=>Text(p,"id")!=pid&&Text(p,"name")==name))profile["name"]=$"{name} ({pid[..8]})";
            foreach(var table in Tables.Where(t=>t!="aggregate_archives")) {
                var destination=(JArray)target[table]!;
                if(table=="profiles")destination.Add(profile);else foreach(var row in source[table]!)destination.Add(row.DeepClone());
            }
            foreach(var archive in source["aggregate_archives"]!.OfType<JObject>()) {
                target["aggregate_archives"]=new JArray(target["aggregate_archives"]!.OfType<JObject>().Where(existing=>
                    Text(existing,"id")!=Text(archive,"id") && (Text(existing,"type")!=Text(archive,"type")||Text(existing,"number")!=Text(archive,"number")))
                    .Select(r=>r.DeepClone()).Append(archive.DeepClone()));
            }
        }
        foreach(var table in Tables)target[table]=new JArray(target[table]!.OfType<JObject>().OrderBy(r=>Text(r,"id"),StringComparer.Ordinal));
        Validate(current);return current;
    }
    public static void Validate(JObject packet)
    {
        if(Text(packet,"format")!="LogBookExchange" || (int?)packet["version"]!=1 || packet["tables"] is not JObject tables || !tables.Properties().Select(p=>p.Name).ToHashSet().SetEquals(Tables)) throw new InvalidDataException(SyncText.T("Sync_40ce8953fa"));
        foreach(var profile in tables["profiles"]!.OfType<JObject>()) {
            profile["maintenanceWorksJson"] ??= "[]";
            profile["defectTasksJson"] ??= "[]";
        }
        if(Encoding.UTF8.GetByteCount(packet.ToString(Formatting.None))>64*1024*1024)throw new InvalidDataException(SyncText.T("Sync_fd0c2a5560"));
        foreach(var table in Tables) {
            if(tables[table] is not JArray rows)throw new InvalidDataException(SyncText.T("Sync_db2ef0cc2f")+table);
            var columns=(JObject)Schema[table]!;var ids=new HashSet<string>();
            foreach(var token in rows) {
                if(token is not JObject row || !row.Properties().Select(p=>p.Name).ToHashSet().SetEquals(columns.Properties().Select(p=>p.Name)))throw new InvalidDataException(SyncText.T("Sync_eb15f0056e")+table);
                var id=Text(row,"id");if(!Guid.TryParseExact(id,"D",out var uuid)||uuid.ToString()!=id || !ids.Add(id))throw new InvalidDataException(SyncText.T("Sync_8d6f15617c"));
                foreach(var field in row.Properties()) {
                    if(field.Name is "id" or "profileId" or "dayId") {if(!Guid.TryParseExact(field.Value.ToString(),"D",out _))throw new InvalidDataException(SyncText.T("Sync_102b915c5e"));continue;}
                    var type=columns[field.Name]!["type"]!.ToString();
                    if(type=="INTEGER") {if(field.Value.Type!=JTokenType.Integer)throw new InvalidDataException(SyncText.T("Sync_ee92f6be54"));var n=field.Value.Value<long>();if(columns[field.Name]!["default"]!.Type==JTokenType.Integer && field.Name is not "sortOrder" and not "createdAt" and not "savedAt" and not "importedAt" and not "resourcesUpdatedAt" && (n<int.MinValue||n>int.MaxValue))throw new InvalidDataException(SyncText.T("Sync_2513b7838f"));}
                    else if(type=="REAL") {if(field.Value.Type!=JTokenType.String)throw new InvalidDataException(SyncText.T("Sync_66fe7b0d0d"));_ = Number(field.Value.ToString());}
                    else if(field.Value.Type!=JTokenType.String)throw new InvalidDataException(SyncText.T("Sync_6f7f71e403"));
                }
                if(table=="profiles") {
                    foreach(var field in new[]{"aggregateAssignmentsJson","aggregateInstalledAtJson","resourcesJson","oilsJson","aggregateHistoryJson","aggregateGroupsJson","oilGroupsJson","maintenanceWorksJson","defectTasksJson"}) {
                        using var reader=new JsonTextReader(new StringReader(Text(row,field))) {DateParseHandling=DateParseHandling.None,MaxDepth=128};
                        var value=JToken.ReadFrom(reader);
                        if(field is "aggregateAssignmentsJson" or "aggregateInstalledAtJson" ? value is not JObject : value is not JArray)throw new InvalidDataException(SyncText.T("Sync_6e4e319830")+field);
                    }
                }
                foreach(var parent in new[]{("profileId","profiles"),("dayId","days")}) if(row[parent.Item1]!=null && !tables[parent.Item2]!.OfType<JObject>().Any(r=>Text(r,"id")==Text(row,parent.Item1))) throw new InvalidDataException(SyncText.T("Sync_5b9518f041"));
            }
            var unique=table=="days"?new[]{"profileId","date"}:table.EndsWith("_start")||table=="desktop_imports"?new[]{"profileId"}:table=="snk_values"?new[]{"dayId"}:table=="aggregate_archives"?new[]{"type","number"}:Array.Empty<string>();
            if(unique.Length>0 && rows.OfType<JObject>().Select(r=>string.Join("\0",unique.Select(k=>Text(r,k)))).Distinct().Count()!=rows.Count)throw new InvalidDataException(SyncText.T("Sync_ea10563791")+table);
        }
    }
    public static AppData Decode(JObject packet,DesktopExchangeState state)
    {
        Validate(packet);var tables=(JObject)packet["tables"]!;var profiles=new JObject();var aggregates=new JObject();
        foreach(var profile in tables["profiles"]!.OfType<JObject>()) {
            var pid=Text(profile,"id");var receipt=tables["desktop_imports"]!.OfType<JObject>().SingleOrDefault(r=>Text(r,"profileId")==pid);
            var native=receipt!=null ? Parse(Text(receipt,"originalJson")) : JObject.FromObject(new ProfileData());
            native["ExchangeId"]=pid;native["ProfileName"]=Text(profile,"name");native["AggregateAssignments"]=Parse(Text(profile,"aggregateAssignmentsJson"));
            var name=Text(profile,"name");if(name.Length==0)name=Text(profile,"boardNumber");if(name.Length==0)name=pid;
            if(profiles[name]!=null)name+=" ("+pid[..8]+")";if(profiles[name]!=null)throw new InvalidDataException(SyncText.T("Sync_b89b65f857"));
            var days=new JObject();foreach(var day in tables["days"]!.OfType<JObject>().Where(d=>Text(d,"profileId")==pid)) {
                var date=Local(Text(day,"date"));var did=Text(day,"id");var d=native["Days"]?[date] as JObject ?? new JObject();d=(JObject)d.DeepClone();
                d["ExchangeId"]=did;d["Date"]=date;d["Profile"]=name;d["Crew"]=Text(day,"crew");
                d["Launches"]=new JArray(tables["launches"]!.OfType<JObject>().Where(l=>Text(l,"dayId")==did).OrderBy(l=>(int)l["order"]!).Select(l=>new JObject {
                    ["ExchangeId"]=Text(l,"id"),["Vsu"]=l["vsu"]!.DeepClone(),["Zemlya"]=l["zemlya"]!.DeepClone(),["Vozduh"]=l["vozduh"]!.DeepClone(),["TP"]=l["tp"]!.DeepClone(),["SamoletLandings"]=l["samolet"]!.DeepClone()}));
                var sn=tables["snk_values"]!.OfType<JObject>().SingleOrDefault(s=>Text(s,"dayId")==did);
                if(sn!=null) {
                    state.Identities["snk:"+did]=Text(sn,"id");var values=d["SnkValues"] as JArray ?? new JArray();
                    foreach(var label in new[]{"A","B","E","G","H","I"}) {
                        var v=values.OfType<JObject>().SingleOrDefault(s=>Text(s,"Label")==label);if(v==null){v=new JObject {["Label"]=label};values.Add(v);}
                        v["LeftValue"]=CleanSnk(Text(sn,label.ToLowerInvariant()+"Left"),label);v["RightValue"]=CleanSnk(Text(sn,label.ToLowerInvariant()+"Right"),label);
                    }d["SnkValues"]=values;
                }
                if(d["SnkValues"] is JArray snkList) {
                    foreach(var v in snkList.OfType<JObject>()) {
                        var lbl=Text(v,"Label");
                        v["LeftValue"]=CleanSnk(Text(v,"LeftValue"),lbl);
                        v["RightValue"]=CleanSnk(Text(v,"RightValue"),lbl);
                    }
                }days[date]=d;
            }native["Days"]=days;
            foreach(var type in Types) {
                var start=tables[Table(type,true)]!.OfType<JObject>().SingleOrDefault(r=>Text(r,"profileId")==pid);
                native[type+"StartRecord"]=start==null ? new JObject() : Unform(type,start,true,native[type+"StartRecord"] as JObject ?? new JObject(),native);
                var previous=Items(native,type+"Records");native[type+"Records"]=new JArray(tables[Table(type,false)]!.OfType<JObject>().Where(r=>Text(r,"profileId")==pid).OrderBy(r=>(long)r["sortOrder"]!).Select(r=> {
                    var prior=previous.OfType<JObject>().FirstOrDefault(p=>Text(p,"ExchangeId")==Text(r,"id")) ?? previous.OfType<JObject>().FirstOrDefault(p=>Local(Text(p,"Date"))==Local(Text(r,"date"))) ?? new JObject();
                    return Unform(type,r,false,prior,native);
                }));
            }
            native["ResourceRecords"]=new JArray(JArray.Parse(Text(profile,"resourcesJson")).OfType<JObject>().OrderBy(r=>(int?)r["order"]??0).Select(r=>Unresource(r,false)));
            native["OilRecords"]=new JArray(JArray.Parse(Text(profile,"oilsJson")).OfType<JObject>().OrderBy(r=>(int?)r["order"]??0).Select(r=>Unresource(r,true)));
            native["MaintenanceWorks"]=JArray.Parse(Text(profile,"maintenanceWorksJson"));
            native["DefectTasks"]=JArray.Parse(Text(profile,"defectTasksJson"));
            native["AggregateHistory"]=new JArray(JArray.Parse(Text(profile,"aggregateHistoryJson")).OfType<JObject>().Select(h=>new JObject { ["Type"]=Text(h,"type"),["OldNumber"]=Text(h,"oldNumber"),["NewNumber"]=Text(h,"newNumber"),["Reason"]=Text(h,"reason"),["ReplacedAt"]=(long?)h["date"]>0 ? DateTimeOffset.FromUnixTimeMilliseconds((long)h["date"]!).LocalDateTime.ToString("O") : DateTime.MinValue.ToString("O")}));
            if(receipt!=null)state.Identities["receipt:"+pid]=Text(receipt,"id");profiles[name]=native;
        }
        foreach(var row in tables["aggregate_archives"]!.OfType<JObject>()) {
            var archived=Parse(Text(row,"formularJson"));var native=archived["legacySource"] as JObject ?? JObject.FromObject(new AggregateData());native=(JObject)native.DeepClone();
            var type=Text(native,"Type");if(!Types.Contains(type)||type=="Chast1")type=Text(row,"type")=="Vk2500"?"Vk250001":Text(row,"type");
            native["Type"]=type;native["Number"]=Text(row,"number");native["ExchangeId"]=Text(row,"id");
            native[type+"StartRecord"]=Unform(type,(JObject)archived["start"]!,true,native[type+"StartRecord"] as JObject ?? new JObject(),native);
            var previousArchive=Items(native,type+"Records");
            native[type+"Records"]=new JArray(((JArray)archived["records"]!).OfType<JObject>().Select(r=>Unform(type,r,false,previousArchive.OfType<JObject>().FirstOrDefault(p=>Local(Text(p,"Date"))==Local(Text(r,"date"))) ?? new JObject(),native)));
            if(aggregates[Text(row,"number")]!=null)throw new InvalidDataException(SyncText.T("Sync_d6f710ef72"));aggregates[Text(row,"number")]=native;
        }
        var data=new JObject {["Profiles"]=profiles,["Aggregates"]=aggregates}.ToObject<AppData>()!;data.Exchange=state;
        state.Packet=packet.ToString(Formatting.None);state.NativeBaseline=Native(data).ToString(Formatting.None);return data;
    }
}
