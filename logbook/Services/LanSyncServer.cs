using logbook.Helpers;
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace logbook.Services;
public sealed class LanReceipt {
    public string Hash {get;set;}="";
    public long Expected {get;set;}
    public long Revision {get;set;}
}
public sealed class LanSyncServer: IDisposable {
    private const int Limit=64*1024*1024;
    private readonly StorageService store;
    private readonly Dispatcher dispatcher;
    private readonly Action downloaded;
    private readonly Action<string> status;
    private readonly TcpListener listener;
    private readonly CancellationTokenSource stop=new(TimeSpan.FromMinutes(10));
    private readonly X509Certificate2 certificate;
    private readonly string token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    public string Peer {get;}
    public string Fingerprint {get;}
    public int Port {get;}
    public long Expires {get;}=DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeMilliseconds();
    public static string[] Addresses() {
        var addresses=new System.Collections.Generic.List<string>();
        foreach(var nic in NetworkInterface.GetAllNetworkInterfaces()) {
            if(nic.OperationalStatus!=OperationalStatus.Up||nic.NetworkInterfaceType==NetworkInterfaceType.Loopback)continue;
            foreach(var a in nic.GetIPProperties().UnicastAddresses) {
                var b=a.Address.GetAddressBytes();
                if(a.Address.AddressFamily==AddressFamily.InterNetwork && (b[0]==10 || b[0]==192&&b[1]==168 || b[0]==172&&b[1]>=16&&b[1]<=31))addresses.Add(a.Address.ToString());
            }
        }
        return addresses.ToArray();
    }
    public LanSyncServer(StorageService store,Dispatcher dispatcher,Action downloaded,Action<string> status,IPAddress? bindAddress=null) {
        this.store=store;this.dispatcher=dispatcher;this.downloaded=downloaded;this.status=status;
        var data=store.Snapshot();if(data.Exchange.LanPeer.Length==0)data.Exchange.LanPeer=Guid.NewGuid().ToString();store.Save(data);Peer=data.Exchange.LanPeer;
        using var rsa=RSA.Create(2048);
        var request=new CertificateRequest("CN=LogBook local sync",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
        using var generated=request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5),DateTimeOffset.UtcNow.AddDays(1));
        certificate=X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pfx),null,X509KeyStorageFlags.DefaultKeySet);
        Fingerprint=Convert.ToHexString(SHA256.HashData(certificate.RawData)).ToLowerInvariant();
        listener=new TcpListener(bindAddress ?? IPAddress.Any,0);listener.Start(4);Port=((IPEndPoint)listener.LocalEndpoint).Port;_ = Run();
    }
    public string Pairing(string host)=>new JObject { ["format"]="LogBookLan",["version"]=1,["host"]=host,["port"]=Port,["peer"]=Peer,["pin"]=Fingerprint,["token"]=token,["expires"]=Expires }.ToString(Formatting.None);
    private JObject Current() {
        var data=store.Snapshot();var packet=ExchangeCodec.Encode(data).ToString(Formatting.None);var state=data.Exchange;
        if(!ExchangeCodec.Same(packet,state.LanPacket)){state.LanRevision++;state.LanPacket=packet;}
        store.Save(data);
        return new JObject {["revision"]=state.LanRevision,["payload"]=ExchangeCodec.Parse(packet)};
    }
    private JObject Handle(JObject body) {
        stop.Token.ThrowIfCancellationRequested();
        var supplied=(string?)body["token"]??"";
        if(supplied.Length!=token.Length||!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied),Encoding.UTF8.GetBytes(token)))throw new InvalidOperationException(SyncText.T("Sync_45bb92dff9"));
        if(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()>Expires)throw new InvalidOperationException(SyncText.T("Sync_ad845dddb0"));
        var op=(string?)body["op"];
        if(op=="complete") {
            var kind=(string?)body["kind"];
            var direction=kind switch {"received"=>"Отправлено","matched"=>"Без изменений","conflict"=>"Выбор версии","cancelled"=>"Отменено",_=>throw new InvalidDataException(SyncText.T("Sync_29c99585f5"))};
            var message=kind switch {"received"=>SyncText.T("Sync_b0faf1ec73"),"matched"=>SyncText.T("Sync_9946eaf724"),"conflict"=>SyncText.T("Sync_8858daa795"),"cancelled"=>SyncText.T("Sync_e97ed54edb"),_=>""};
            store.History.Add(new SyncHistoryEntry(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),"Wi-Fi",direction,message));
            return new JObject {["ok"]=true};
        }
        if(op=="pull")return Current();
        if(op!="commit")throw new InvalidDataException(SyncText.T("Sync_12c063ad9c"));
        var id=(string?)body["change"]??"";if(!Guid.TryParse(id,out _))throw new InvalidDataException(SyncText.T("Sync_f8961ae3fa"));
        var expected=(long?)body["expected"]??-1;
        var payload=body["payload"] as JObject ?? throw new InvalidDataException(SyncText.T("Sync_d0dd940c4c"));ExchangeCodec.Validate(payload);
        var packet=payload.ToString(Formatting.None);var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(packet)));
        var previous=store.Snapshot().Exchange.LanReceipts;
        if(previous.TryGetValue(id,out var receipt)) {
            if(receipt.Hash!=hash||receipt.Expected!=expected)throw new InvalidDataException(SyncText.T("Sync_daea37184f"));
            return new JObject {["accepted"]=true,["revision"]=receipt.Revision};
        }
        var current=Current();var revision=(long)current["revision"]!;
        if(expected!=revision)return new JObject {["accepted"]=false,["revision"]=revision};
        var next=checked(revision+1);
        // The receipt and replacement are saved in the same atomic work_data.json write.
        store.Download(packet,current["payload"]!.ToString(Formatting.None),next,"","",preserveSyncState:true,configure:s=> {
            s.LanRevision=next;s.LanPacket=packet;s.LanReceipts[id]=new LanReceipt {Hash=hash,Expected=expected,Revision=next};
        });
        store.History.Add(new SyncHistoryEntry(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),"Wi-Fi","Получено",SyncText.T("Sync_dd0a661b52"),store.LastSyncBackup));
        downloaded();status(SyncText.T("Sync_06d584a626"));
        return new JObject {["accepted"]=true,["revision"]=next};
    }
    private async Task Run() {
        try {
            while(!stop.IsCancellationRequested) {
                using var client=await listener.AcceptTcpClientAsync(stop.Token);
                using var timeout=CancellationTokenSource.CreateLinkedTokenSource(stop.Token);timeout.CancelAfter(TimeSpan.FromSeconds(45));
                using var tls=new SslStream(client.GetStream(),false);
                try {
                    await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions {ServerCertificate=certificate,EnabledSslProtocols=SslProtocols.Tls12|SslProtocols.Tls13},timeout.Token);
                    var header=new byte[4];await tls.ReadExactlyAsync(header,timeout.Token);int length=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header);
                    if(length<=0||length>Limit)throw new InvalidDataException(SyncText.T("Sync_fd0c2a5560"));
                    var bytes=new byte[length];await tls.ReadExactlyAsync(bytes,timeout.Token);
                    var body=ExchangeCodec.Parse(Encoding.UTF8.GetString(bytes));
                    JObject reply;
                    try {reply=await dispatcher.InvokeAsync(()=>Handle(body));}
                    catch(Exception e){reply=new JObject {["error"]=e.Message};}
                    var response=Encoding.UTF8.GetBytes(reply.ToString(Formatting.None));
                    System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header,response.Length);
                    await tls.WriteAsync(header,timeout.Token);await tls.WriteAsync(response,timeout.Token);await tls.FlushAsync(timeout.Token);
                } catch(Exception e) when(!stop.IsCancellationRequested) {
                    await dispatcher.InvokeAsync(()=>status(SyncText.T("Sync_6a4a81b5eb")+e.Message));
                }
            }
        } catch(OperationCanceledException) {} catch(SocketException) when(stop.IsCancellationRequested) {}
        finally {listener.Stop();certificate.Dispose();await dispatcher.InvokeAsync(()=>status(SyncText.T("Sync_e651213c51")));}
    }
    public void Dispose(){stop.Cancel();listener.Stop();}
}
