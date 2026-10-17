using logbook.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using logbook.Models;

namespace logbook.Services;
public record DesktopSession(string Endpoint,string User,string Email,string Access,string Refresh,long Expires,bool HasTelegram=false,bool HasMax=false,string? LinkedEmail=null);
public record DesktopRemote(long Revision,string? Payload);
public record DesktopCommit(bool Accepted,long Revision);
public record DesktopConflict(string Local,string Remote,long Revision);
public record DesktopSyncResult(string Message,DesktopConflict? Conflict=null,bool Downloaded=false,string Direction="Без изменений");
public record SharedProfileInfo(string ProfileId,string Name,string BoardNumber,long Revision,long UpdatedAt);
public record AuthSessionResult(string Status, string? Email, string? Password, string? FirstName, string? Username);
public interface IDesktopRemote {
    Task<DesktopRemote> Pull();
    Task<DesktopCommit> Commit(string change,long expected,string payload);
}
public sealed class DesktopAuth
{
    private readonly string path;
    public DesktopAuth(string folder) {path=Path.Combine(folder,"sync-auth.dat");}
    public DesktopSession? Read() {
        if(!File.Exists(path))return null;
        try {return JsonConvert.DeserializeObject<DesktopSession>(Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser)));}
        catch(CryptographicException) {Clear();return null;}
    }
    public void Save(DesktopSession session) {AtomicFile.Write(path,ProtectedData.Protect(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(session)),null,DataProtectionScope.CurrentUser));}
    public void Clear() {if(File.Exists(path))File.Delete(path);}
}
public static class AtomicFile
{
    public static void Write(string path,byte[] bytes) {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);var temp=path+"."+Guid.NewGuid()+".tmp";
        try {using(var file=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {file.Write(bytes);file.Flush(true);}
            if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
        } finally {if(File.Exists(temp))File.Delete(temp);}
    }
}
public sealed class DesktopSupabase: IDesktopRemote,IDisposable
{
    private readonly string endpoint,key;private readonly DesktopAuth auth;private readonly bool sharedSpace;
    private readonly HttpClient http=new(new HttpClientHandler {AllowAutoRedirect=false}) {Timeout=TimeSpan.FromSeconds(45)};
    public DesktopSupabase(string endpoint,string key,DesktopAuth auth,bool sharedSpace=false) {
        this.endpoint=endpoint.TrimEnd('/');this.key=key.Trim();this.auth=auth;this.sharedSpace=sharedSpace;
        if(!Uri.TryCreate(this.endpoint,UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.UserInfo.Length>0||uri.Query.Length>0||uri.Fragment.Length>0||uri.AbsolutePath!="/")throw new InvalidDataException(SyncText.T("Sync_98ea53641a"));
        bool safe=this.key.StartsWith("sb_publishable_",StringComparison.Ordinal);
        if(!safe)try {var b=this.key.Split('.')[1].Replace('-','+').Replace('_','/');b=b.PadRight((b.Length+3)/4*4,'=');safe=(string?)JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(b)))["role"]=="anon";}catch(FormatException){}catch(IndexOutOfRangeException){}catch(JsonException){}
        if(!safe)throw new InvalidDataException(SyncText.T("Sync_ce08f48962"));
    }
    private async Task<JObject> Request(string path,JObject body,string? token=null) {
        using var request=new HttpRequestMessage(HttpMethod.Post,endpoint+path);request.Headers.Add("apikey",key);
        if(token!=null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
        request.Content=new StringContent(body.ToString(Formatting.None),Encoding.UTF8,"application/json");
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,timeout.Token);
        if(!response.IsSuccessStatusCode)throw new HttpRequestException(response.StatusCode switch {
            HttpStatusCode.Unauthorized=>SyncText.T("Sync_ee43eced29"),
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity=>SyncText.T("Sync_0d75ab090e"),
            HttpStatusCode.Forbidden=>SyncText.T("Sync_322abe01b3"),
            _=>SyncText.T("Sync_bc957d5e09", (int)response.StatusCode)},null,response.StatusCode);
        using var stream=await response.Content.ReadAsStreamAsync(timeout.Token);
        using var buffer=new MemoryStream();var block=new byte[8192];int count;
        while((count=await stream.ReadAsync(block,timeout.Token))>0) {
            if(buffer.Length+count>64*1024*1024)throw new InvalidDataException(SyncText.T("Sync_fd0c2a5560"));buffer.Write(block,0,count);
        }
        return ExchangeCodec.Parse(Encoding.UTF8.GetString(buffer.ToArray()));
    }
    private DesktopSession Store(JObject value) {
        var user = (JObject)value["user"]!;
        var meta = user["user_metadata"] as JObject;
        var email = (string)user["email"]!;
        var hasTg = email.StartsWith("tg_") || meta?["telegram_id"] != null || (bool?)meta?["tg_linked"] == true;
        var hasMax = meta?["max_id"] != null || (bool?)meta?["max_linked"] == true;
        var linkedEmail = (string?)meta?["linked_email"];
        var session = new DesktopSession(endpoint, (string)user["id"]!, email, (string)value["access_token"]!, (string)value["refresh_token"]!, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (long)value["expires_in"]! * 1000, hasTg, hasMax, linkedEmail);
        auth.Save(session);return session;
    }
    public async Task<(bool HasTelegram, bool HasMax, string? LinkedEmail)> CheckLinkedStatus() {
        var current = auth.Read();
        if (current == null) return (false, false, null);
        try {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint + "/auth/v1/user");
            request.Headers.Add("apikey", key);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await Token());
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var response = await http.SendAsync(request, timeout.Token);
            if (response.IsSuccessStatusCode) {
                var text = await response.Content.ReadAsStringAsync();
                var obj = JObject.Parse(text);
                var meta = obj["user_metadata"] as JObject;
                var email = (string?)obj["email"] ?? current.Email;
                var hasTg = email.StartsWith("tg_") || meta?["telegram_id"] != null || (bool?)meta?["tg_linked"] == true;
                var hasMax = meta?["max_id"] != null || (bool?)meta?["max_linked"] == true;
                var linkedEmail = (string?)meta?["linked_email"] ?? current.LinkedEmail;
                var updated = current with { HasTelegram = hasTg, HasMax = hasMax, LinkedEmail = linkedEmail };
                auth.Save(updated);
                return (hasTg, hasMax, linkedEmail);
            }
        } catch { }
        return (current.HasTelegram || current.Email.StartsWith("tg_"), current.HasMax, current.LinkedEmail);
    }
    private static string BotPassword(long telegramId) {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var digest = md5.ComputeHash(Encoding.UTF8.GetBytes($"salt_logbook_2026_{telegramId}"));
        return "TgLogBook_" + string.Concat(digest.Select(b => b.ToString("x2")));
    }
    public async Task LinkEmail(string newEmail, string newPassword) {
        var session = auth.Read() ?? throw new InvalidOperationException("Пользователь не авторизован");
        var cleanEmail = newEmail.Trim().ToLowerInvariant();

        using var request = new HttpRequestMessage(HttpMethod.Put, endpoint + "/auth/v1/user");
        request.Headers.Add("apikey", key);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await Token());
        var body = new JObject {
            ["data"] = new JObject { ["linked_email"] = cleanEmail }
        };
        request.Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var req2 = new HttpRequestMessage(HttpMethod.Post, endpoint + "/rest/v1/telegram_auth_sessions");
        req2.Headers.Add("apikey", key);
        req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        req2.Headers.Add("Prefer", "resolution=merge-duplicates");
        var m = System.Text.RegularExpressions.Regex.Match(session.Email, @"^tg_(\d+)@");
        long pilotId = m.Success ? long.Parse(m.Groups[1].Value) : 0;
        var sessionObj = new JObject {
            ["code"] = "email_" + cleanEmail,
            ["telegram_id"] = pilotId,
            ["first_name"] = cleanEmail,
            ["username"] = "",
            ["auth_email"] = session.Email,
            ["auth_password"] = newPassword,
            ["status"] = "confirmed"
        };
        req2.Content = new StringContent(sessionObj.ToString(Formatting.None), Encoding.UTF8, "application/json");
        using var res2 = await http.SendAsync(req2);
        res2.EnsureSuccessStatusCode();

        auth.Save(session with { LinkedEmail = cleanEmail });
    }
    public async Task<bool> SignIn(string email,string password,bool register) {
        var cleanEmail = email.Trim().ToLowerInvariant();
        if (!register) {
            // First check if this email is an alias for a bot account (Telegram / MAX)
            try {
                using var req = new HttpRequestMessage(HttpMethod.Get, endpoint + $"/rest/v1/telegram_auth_sessions?code=eq.email_{cleanEmail}&select=telegram_id,auth_email,auth_password");
                req.Headers.Add("apikey", key);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                using var res = await http.SendAsync(req);
                if (res.IsSuccessStatusCode) {
                    var arr = JArray.Parse(await res.Content.ReadAsStringAsync());
                    if (arr.Count > 0) {
                        var row = (JObject)arr[0];
                        var pass = (string?)row["auth_password"];
                        var realMail = (string?)row["auth_email"];
                        var tgId = (long?)row["telegram_id"] ?? 0;
                        if (!string.IsNullOrEmpty(realMail) && pass == password) {
                            var botPw = tgId > 0 ? BotPassword(tgId) : password;
                            JObject? resp2 = null;
                            try {
                                resp2 = await Request("/auth/v1/token?grant_type=password", new JObject { ["email"] = realMail, ["password"] = botPw });
                            } catch {
                                resp2 = await Request("/auth/v1/token?grant_type=password", new JObject { ["email"] = realMail, ["password"] = password });
                            }
                            if (resp2?["access_token"] != null && resp2["access_token"]!.Type != JTokenType.Null) {
                                Store(resp2);
                                return true;
                            }
                        } else if (!string.IsNullOrEmpty(realMail) && pass != password) {
                            throw new InvalidOperationException("Неверный пароль");
                        }
                    }
                }
            } catch (InvalidOperationException) { throw; }
            catch { }

            var response = await Request("/auth/v1/token?grant_type=password", new JObject { ["email"] = cleanEmail, ["password"] = password });
            if (response["access_token"] != null && response["access_token"]!.Type != JTokenType.Null) {
                Store(response);
                return true;
            }
            return false;
        }
        var regResp = await Request("/auth/v1/signup", new JObject { ["email"] = cleanEmail, ["password"] = password });
        if (regResp["access_token"] == null || regResp["access_token"]!.Type == JTokenType.Null) return false;
        Store(regResp);
        return true;
    }
    public async Task CreateAuthSession(string code) {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint + "/rest/v1/telegram_auth_sessions");
        request.Headers.Add("apikey", key);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Headers.Add("Prefer", "return=minimal");
        request.Content = new StringContent(new JObject { ["code"] = code, ["status"] = "pending" }.ToString(Formatting.None), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
    public async Task<AuthSessionResult?> CheckAuthSession(string code) {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint + $"/rest/v1/telegram_auth_sessions?code=eq.{code}&select=status,auth_email,auth_password,first_name,username");
        request.Headers.Add("apikey", key);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        using var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        var text = await response.Content.ReadAsStringAsync();
        var array = JArray.Parse(text);
        if (array.Count == 0) return null;
        var obj = (JObject)array[0];
        return new AuthSessionResult(
            (string?)obj["status"] ?? "pending",
            (string?)obj["auth_email"],
            (string?)obj["auth_password"],
            (string?)obj["first_name"],
            (string?)obj["username"]
        );
    }
    public async Task DeleteAuthSession(string code) {
        using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint + $"/rest/v1/telegram_auth_sessions?code=eq.{code}");
        request.Headers.Add("apikey", key);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        using var response = await http.SendAsync(request);
    }
    private async Task<string> Token(bool refresh=false) {
        var session=auth.Read()??throw new InvalidOperationException(SyncText.T("Sync_4eb58e1e56"));
        if(session.Endpoint!=endpoint)throw new InvalidOperationException(SyncText.T("Sync_2a2a9b53b3"));
        if(!refresh&&session.Expires>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()+60000)return session.Access;
        return Store(await Request("/auth/v1/token?grant_type=refresh_token",new JObject {["refresh_token"]=session.Refresh})).Access;
    }
    private async Task<JObject> Rpc(string name,JObject args) {
        try {return await Request("/rest/v1/rpc/"+name,args,await Token());}
        catch(HttpRequestException e) when(e.StatusCode==HttpStatusCode.Unauthorized) {return await Request("/rest/v1/rpc/"+name,args,await Token(true));}
    }
    public async Task<DesktopRemote> Pull() {var r=await Rpc(sharedSpace?"logbook_shared_sync_pull_v1":"logbook_sync_pull_v1",new JObject());return new DesktopRemote((long)r["revision"]!,r["payload"]?.Type==JTokenType.Null?null:r["payload"]?.ToString(Formatting.None));}
    public async Task<DesktopCommit> Commit(string change,long expected,string payload) {var r=await Rpc(sharedSpace?"logbook_shared_sync_commit_v1":"logbook_sync_commit_v1",new JObject {["p_change_id"]=change,["p_expected_revision"]=expected,["p_payload"]=ExchangeCodec.Parse(payload)});return new DesktopCommit((bool)r["accepted"]!,(long)r["revision"]!);}
    public async Task<int> PublishSharedProfiles(IEnumerable<SharedProfilePacket> profiles) {
        var count=0;foreach(var profile in profiles) {await Rpc("logbook_shared_profile_publish_v2",new JObject {
            ["p_profile_id"]=profile.ProfileId,["p_name"]=profile.Name,["p_board_number"]=profile.BoardNumber,["p_payload"]=profile.Payload
        });count++;}return count;
    }
    public async Task<List<SharedProfileInfo>> SharedProfileCatalog() {
        var result=await Rpc("logbook_shared_profile_catalog_v2",new JObject());return result["profiles"]!.OfType<JObject>()
            .Select(p=>new SharedProfileInfo((string)p["profileId"]!,(string)p["name"]!,(string)p["boardNumber"]!,
                (long)p["revision"]!,(long)p["updatedAt"]!)).ToList();
    }
    public async Task<List<JObject>> PullSharedProfiles(IEnumerable<string> profileIds) {
        var result=new List<JObject>();foreach(var id in profileIds) {
            var response=await Rpc("logbook_shared_profile_pull_v2",new JObject {["p_profile_ids"]=new JArray(id)});
            var row=response["profiles"]!.OfType<JObject>().SingleOrDefault() ?? throw new InvalidDataException(SyncText.T("Sync_shared_missing"));
            result.Add((JObject)row["payload"]!);
        }return result;
    }
    public void Dispose()=>http.Dispose();
}
public interface IDesktopStore {
    AppData Snapshot();
    void Save(AppData data);
    void Download(string packet,string expectedLocal,long revision,string endpoint,string account);
}
public sealed class DesktopSyncEngine
{
    private readonly IDesktopStore store;private readonly IDesktopRemote remote;private readonly string endpoint,account;
    public DesktopSyncEngine(IDesktopStore store,IDesktopRemote remote,string endpoint,string account) {this.store=store;this.remote=remote;this.endpoint=endpoint;this.account=account;}
    private string Local() {var data=store.Snapshot();var payload=ExchangeCodec.Encode(data).ToString(Formatting.None);data.Exchange.SaveCheckpoint();store.Save(data);return payload;}
    private void Ack(long revision,string payload) {
        if(revision<=0)throw new InvalidDataException(SyncText.T("Sync_ddf52bbdea"));
        var data=store.Snapshot();var s=data.Exchange;s.BaseRevision=revision;s.BaseJson=payload;s.PendingId="";s.PendingJson="";s.PendingRevision=0;s.LastSuccess=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();data.Exchange.SaveCheckpoint();store.Save(data);
    }
    public async Task<DesktopSyncResult> Sync() {
        var data=store.Snapshot();var s=data.Exchange;
        if(s.Endpoint!=endpoint||s.Account!=account) {s.SelectCheckpoint(endpoint,account);data.Exchange.SaveCheckpoint();store.Save(data);}
        if(s.PendingId.Length>0) {
            var reply=await remote.Commit(s.PendingId,s.PendingRevision,s.PendingJson);
            if(reply.Accepted)Ack(reply.Revision,s.PendingJson);else ClearPending();
        }
        var cloud=await remote.Pull();if(cloud.Revision<0||((cloud.Revision==0)!=(cloud.Payload==null)))throw new InvalidDataException(SyncText.T("Sync_3ecf8d7332"));
        var local=Local();s=store.Snapshot().Exchange;
        if(cloud.Revision==0) {if(s.BaseRevision>0)throw new InvalidOperationException(SyncText.T("Sync_1421293073"));return await Upload(local,0);}
        ExchangeCodec.Validate(ExchangeCodec.Parse(cloud.Payload!));
        if(ExchangeCodec.Same(local,cloud.Payload!)) {Ack(cloud.Revision,cloud.Payload!);return new DesktopSyncResult(SyncText.T("Sync_ed8e867c4b"));}
        if(ExchangeCodec.Same(local,s.BaseJson)||s.BaseRevision==0&&!ExchangeCodec.Parse(local)["tables"]!.Children<JProperty>().Any(p=>((JArray)p.Value).Count>0)) {
            store.Download(cloud.Payload!,local,cloud.Revision,endpoint,account);return new DesktopSyncResult(SyncText.T("Sync_7bb0f62d66"),Downloaded:true);
        }
        if(cloud.Revision==s.BaseRevision&&ExchangeCodec.Same(cloud.Payload!,s.BaseJson))return await Upload(local,cloud.Revision);
        return new DesktopSyncResult(SyncText.T("Sync_5cca551617"),new DesktopConflict(local,cloud.Payload!,cloud.Revision));
    }
    private void ClearPending() {var data=store.Snapshot();data.Exchange.PendingId="";data.Exchange.PendingJson="";data.Exchange.PendingRevision=0;data.Exchange.SaveCheckpoint();store.Save(data);}
    private async Task<DesktopSyncResult> Upload(string payload,long expected) {
        var data=store.Snapshot();var id=Guid.NewGuid().ToString();data.Exchange.PendingId=id;data.Exchange.PendingJson=payload;data.Exchange.PendingRevision=expected;data.Exchange.SaveCheckpoint();store.Save(data);
        var reply=await remote.Commit(id,expected,payload);
        if(reply.Accepted) {Ack(reply.Revision,payload);return new DesktopSyncResult(ExchangeCodec.Same(Local(),payload)?SyncText.T("Sync_dec2f83363"):SyncText.T("Sync_f4e0a06f87"),Direction:"Отправлено");}
        ClearPending();var fresh=await remote.Pull();return new DesktopSyncResult(SyncText.T("Sync_0c2d195259"),fresh.Payload!=null?new DesktopConflict(Local(),fresh.Payload,fresh.Revision):null);
    }
    public async Task<DesktopSyncResult> Resolve(DesktopConflict conflict,bool useLocal) {
        var local=Local();if(!ExchangeCodec.Same(local,conflict.Local))throw new InvalidOperationException(SyncText.T("Sync_93155fad6e"));
        var fresh=await remote.Pull();if(fresh.Revision!=conflict.Revision||!ExchangeCodec.Same(fresh.Payload??"",conflict.Remote))return new DesktopSyncResult(SyncText.T("Sync_5571c8ae40"),fresh.Payload!=null?new DesktopConflict(local,fresh.Payload,fresh.Revision):null);
        if(useLocal)return await Upload(local,fresh.Revision);
        store.Download(conflict.Remote,local,conflict.Revision,endpoint,account);return new DesktopSyncResult(SyncText.T("Sync_96523a886e"),Downloaded:true);
    }
}
