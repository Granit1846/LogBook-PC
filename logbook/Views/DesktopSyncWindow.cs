using logbook.Helpers;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using logbook.Services;
using Newtonsoft.Json.Linq;

namespace logbook.Views;
public sealed class DesktopSyncWindow: Window
{
    private readonly SettingsService settings;
    private readonly StorageService storage;
    private readonly Action downloaded;
    private readonly DesktopAuth auth;
    private readonly TextBox email=new() {MinWidth=240,Margin=new Thickness(0,4,0,8)};
    private readonly PasswordBox password=new() {Margin=new Thickness(0,4,0,8)};
    private readonly TextBlock status=new() {TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,8)};
    private readonly TextBlock sessionLabel=new() {TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)};
    private readonly StackPanel login=new();
    private readonly StackPanel actions=new();
    private readonly Button sync=new() {Content=SyncText.T("Sync_5822fb7d95"),Margin=new Thickness(0,8,0,8),Padding=new Thickness(12,8,12,8)};
    private readonly Button logout=new() {Content=SyncText.T("Sync_8578367fc2"),Margin=new Thickness(0,8,0,8)};
    private readonly RadioButton personalSpace=new() {Content=SyncText.T("Sync_personal_space"),GroupName="CloudSpace",IsChecked=true,Margin=new Thickness(0,4,16,4)};
    private readonly RadioButton sharedSpace=new() {Content=SyncText.T("Sync_shared_space"),GroupName="CloudSpace",Margin=new Thickness(0,4,0,4)};
    private readonly TextBlock sharedWarning=new() {Text=SyncText.T("Sync_shared_warning"),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,2,0,6)};
    private readonly StackPanel badgesPanel = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 8) };
    private readonly Border tgBadge = CreateBadge("✅ Telegram", System.Windows.Media.Color.FromRgb(42, 171, 238));
    private readonly Border maxBadge = CreateBadge("✅ MAX", System.Windows.Media.Color.FromRgb(0, 119, 255));
    private readonly StackPanel linkPanel = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 8) };
    private Button linkMaxBtn = null!;
    private Button linkTgBtn = null!;
    private bool busy;
    private Button? wifi;
    private static Border CreateBadge(string text, System.Windows.Media.Color color) {
        return new Border {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(32, color.R, color.G, color.B)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(color),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 0, 8, 0),
            Child = new TextBlock { Text = text, FontWeight = FontWeights.SemiBold }
        };
    }
    public DesktopSyncWindow(SettingsService settings,StorageService storage,Action downloaded,string? authFolder=null,bool personalOnly=false)
    {
        this.settings=settings;this.storage=storage;this.downloaded=downloaded;
        auth=new DesktopAuth(authFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"LogbookApp"));
        Title=SyncText.T("Sync_c08622028f");Width=590;Height=570;MinWidth=420;MinHeight=340;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        SetResourceReference(BackgroundProperty,"WindowBackgroundBrush");SetResourceReference(ForegroundProperty,"WindowForegroundBrush");
        SetResourceReference(FontFamilyProperty,"AppFontFamily");FontSize=14;
        var root=new Grid {Margin=new Thickness(20)};root.RowDefinitions.Add(new RowDefinition {Height=new GridLength(1,GridUnitType.Star)});root.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});Content=root;
        var scroll=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto};root.Children.Add(scroll);
        var content=new StackPanel();scroll.Content=content;
        content.Children.Add(new TextBlock {Text=SyncText.T("Sync_9c3b0b5a94"),FontSize=20,FontWeight=FontWeights.Bold,Margin=new Thickness(0,0,0,12)});
        content.Children.Add(new TextBlock {Text=SyncText.T("Sync_b583874a6c"),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,16)});
        wifi=Button(SyncText.T("Sync_6559f0e947"),()=> {new LanSyncWindow(storage,downloaded) {Owner=this}.ShowDialog();return Task.CompletedTask;});
        content.Children.Add(wifi);
        content.Children.Add(Button(SyncText.T("Sync_236ecb49e8"),()=> {new SyncHistoryWindow(settings.Settings.DataFolder) {Owner=this}.ShowDialog();return Task.CompletedTask;}));
        content.Children.Add(sessionLabel);content.Children.Add(login);
        var maxLogin = Button("🔵 Войти через MAX", async () => await MessengerAuth("max"));
        maxLogin.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 119, 255));
        maxLogin.Foreground = System.Windows.Media.Brushes.White;
        maxLogin.FontWeight = FontWeights.SemiBold;
        maxLogin.Padding = new Thickness(14, 8, 14, 8);
        maxLogin.Margin = new Thickness(0, 4, 0, 4);

        var tgLogin = Button("🔷 Войти через Telegram", async () => await MessengerAuth("telegram"));
        tgLogin.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 171, 238));
        tgLogin.Foreground = System.Windows.Media.Brushes.White;
        tgLogin.FontWeight = FontWeights.SemiBold;
        tgLogin.Padding = new Thickness(14, 8, 14, 8);
        tgLogin.Margin = new Thickness(0, 0, 0, 10);

        login.Children.Add(maxLogin);
        login.Children.Add(tgLogin);
        login.Children.Add(new TextBlock { Text = "── или через Email и пароль ──", Foreground = System.Windows.Media.Brushes.Gray, Margin = new Thickness(0, 2, 0, 8), HorizontalAlignment = HorizontalAlignment.Center });
        login.Children.Add(new TextBlock {Text="Email"});login.Children.Add(email);login.Children.Add(new TextBlock {Text=SyncText.T("Sync_14f7c63cc1")});login.Children.Add(password);
        var buttons=new StackPanel {Orientation=Orientation.Horizontal};login.Children.Add(buttons);
        var enter=Button(SyncText.T("Sync_939e95a11d"),async()=>await SignIn(false));var register=Button(SyncText.T("Sync_7224765e44"),async()=>await SignIn(true));buttons.Children.Add(enter);buttons.Children.Add(register);
        content.Children.Add(actions);
        var spaces=new WrapPanel();spaces.Children.Add(personalSpace);spaces.Children.Add(sharedSpace);actions.Children.Add(spaces);
        actions.Children.Add(sharedWarning);actions.Children.Add(sync);actions.Children.Add(logout);
        badgesPanel.Children.Add(tgBadge);
        badgesPanel.Children.Add(maxBadge);
        actions.Children.Add(badgesPanel);
        linkMaxBtn = Button("🔗 Связать с MAX", () => { LinkMessenger("max"); return Task.CompletedTask; });
        linkTgBtn = Button("🔗 Связать с Telegram", () => { LinkMessenger("telegram"); return Task.CompletedTask; });
        linkPanel.Children.Add(linkMaxBtn);
        linkPanel.Children.Add(linkTgBtn);
        actions.Children.Add(linkPanel);
        personalSpace.Checked+=(_,_)=>Refresh();sharedSpace.Checked+=(_,_)=>Refresh();
        sync.Click+=async(_,_)=>await Operation(async()=> {
            var history=new SyncHistory(settings.Settings.DataFolder);
            try {
            using var client=Client();var session=auth.Read()??throw new InvalidOperationException(SyncText.T("Sync_4eb58e1e56"));
            if(sharedSpace.IsChecked==true) {
                var publishing=storage.Snapshot();var profiles=ExchangeCodec.SharedProfilePackets(publishing);storage.Save(publishing);
                var published=await client.PublishSharedProfiles(profiles);var catalog=await client.SharedProfileCatalog();
                var picker=new SharedProfilePickerWindow(catalog) {Owner=this};picker.ShowDialog();
                if(picker.Selected==null) {status.Text=SyncText.T("Sync_9c327e9ce5");return;}
                var packets=await client.PullSharedProfiles(picker.Selected);var current=storage.Snapshot();
                var local=ExchangeCodec.Encode(current).ToString(Newtonsoft.Json.Formatting.None);storage.Save(current);
                var merged=ExchangeCodec.MergeSharedProfiles(ExchangeCodec.Parse(local),packets).ToString(Newtonsoft.Json.Formatting.None);
                storage.Download(merged,local,0,"","",true);downloaded();
                var completed=storage.Snapshot();completed.Exchange.SelectCheckpoint(session.Endpoint,"shared");completed.Exchange.LastSuccess=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();completed.Exchange.SaveCheckpoint();storage.Save(completed);
                status.Text=string.Format(SyncText.T("Sync_shared_done"),published,packets.Count);
                history.Add(new SyncHistoryEntry(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),SyncText.T("Sync_shared_space"),SyncText.T("Sync_shared_received"),status.Text,storage.LastSyncBackup));return;
            }
            var account=sharedSpace.IsChecked==true?"shared":session.User;
            var engine=new DesktopSyncEngine(storage,client,session.Endpoint,account);var result=await engine.Sync();
            while(result.Conflict!=null) {
                var choice=new DesktopConflictWindow(result.Conflict) {Owner=this};choice.ShowDialog();
                if(choice.UseLocal==null) {status.Text=SyncText.T("Sync_9c327e9ce5");history.Add(new SyncHistoryEntry(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),"Интернет","Отменено",status.Text));return;}
                result=await engine.Resolve(result.Conflict,choice.UseLocal.Value);
            }
            if(result.Downloaded)downloaded();status.Text=result.Message;
            history.Add(new SyncHistoryEntry(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),"Интернет",result.Downloaded?"Получено":result.Direction,result.Message,result.Downloaded?storage.LastSyncBackup:""));
            } catch(Exception error) {history.Add(new SyncHistoryEntry(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),"Интернет","Ошибка",error is System.Net.Http.HttpRequestException ? SyncText.T("Sync_e48c5d1ecf") : error.Message));throw;}
        });
        logout.Click+=(_,_)=> {auth.Clear();status.Text=SyncText.T("Sync_8e4bf12752");Refresh();};
        content.Children.Add(status);
        var close=Button(SyncText.T("Sync_4ae50d3073"),()=> {Close();return Task.CompletedTask;});close.HorizontalAlignment=HorizontalAlignment.Right;Grid.SetRow(close,1);root.Children.Add(close);
        Closing+=(_,e)=> {if(busy){e.Cancel=true;status.Text=SyncText.T("Sync_c572544936");}};
        Loaded+=async(_,_)=> {
            logbook.Helpers.DarkTitleBarHelper.Apply(this);
            Refresh();
            if (auth.Read() != null) {
                try {
                    using var client = Client();
                    await client.CheckLinkedStatus();
                    Refresh();
                } catch {}
            }
        };
    }
    private static Button Button(string title,Func<Task> action) {
        var button=new Button {Content=title,Padding=new Thickness(12,7,12,7),Margin=new Thickness(0,4,8,4)};
        button.Click+=async(_,_)=>await action();return button;
    }
    private DesktopSupabase Client()=>new(settings.Settings.SupabaseUrl,settings.Settings.SupabaseKey,auth,sharedSpace.IsChecked==true);
    private async Task SignIn(bool register) {
        var mail=email.Text;var secret=password.Password;password.Clear();
        await Operation(async()=> {
            if(string.IsNullOrWhiteSpace(mail)||secret.Length==0)throw new InvalidOperationException(SyncText.T("Sync_3c9c951703"));
            using var client=Client();status.Text=await client.SignIn(mail,secret,register)?SyncText.T("Sync_0127d2d21d"):SyncText.T("Sync_a8592f662c");
        });
    }
    private async Task MessengerAuth(string target) {
        var code = "auth_" + Guid.NewGuid().ToString("N").Substring(0, 12);
        var botUrl = target == "max"
            ? $"https://max.ru/se14711712_bot?start={code}"
            : $"https://t.me/my_logbook_app_bot?start={code}";

        await Operation(async () => {
            using var client = Client();
            await client.CreateAuthSession(code);
            status.Text = target == "max"
                ? "Ожидание подтверждения в боте MAX (@se14711712_bot)... Откройте мессенджер и нажмите «Запустить»."
                : "Ожидание подтверждения в боте Telegram (@my_logbook_app_bot)... Откройте мессенджер и нажмите «Запустить».";

            try {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = botUrl,
                    UseShellExecute = true
                });
            } catch {}

            for (int i = 0; i < 80; i++) {
                await Task.Delay(1500);
                var confirmed = await client.CheckAuthSession(code);
                if (confirmed != null && confirmed.Status == "confirmed" && !string.IsNullOrEmpty(confirmed.Email)) {
                    await client.SignIn(confirmed.Email, confirmed.Password ?? "", false);
                    _ = client.DeleteAuthSession(code);
                    var name = !string.IsNullOrEmpty(confirmed.Username)
                        ? $"{confirmed.FirstName} (@{confirmed.Username})"
                        : confirmed.FirstName ?? (target == "max" ? "MAX" : "Telegram");
                    status.Text = $"Вход через {(target == "max" ? "MAX" : "Telegram")} выполнен: {name}";
                    Refresh();
                    return;
                }
            }
            status.Text = "Время ожидания входа истекло. Повторите попытку.";
        });
    }
    private void LinkMessenger(string target) {
        var session = auth.Read();
        if (session == null) return;
        var m = System.Text.RegularExpressions.Regex.Match(session.Email, @"^tg_(\d+)@telegram\.logbook$");
        var pilotId = m.Success ? m.Groups[1].Value : "";
        var token = Guid.NewGuid().ToString("N").Substring(0, 8);
        var param = !string.IsNullOrEmpty(pilotId) ? $"link_{pilotId}_{token}" : $"auth_{token}";
        var url = target == "max"
            ? $"https://max.ru/se14711712_bot?start={param}"
            : $"https://t.me/my_logbook_app_bot?start={param}";
        try {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName = url,
                UseShellExecute = true
            });
        } catch {}
    }
    private async Task Operation(Func<Task> action) {
        if(busy)return;busy=true;if(wifi!=null)wifi.IsEnabled=false;login.IsEnabled=false;actions.IsEnabled=false;status.Text=SyncText.T("Sync_9674b15f97");
        try {await action();}
        catch(Exception error) {status.Text=error is OperationCanceledException ? SyncText.T("Sync_2818a93db8") : error.Message;}
        finally {busy=false;if(wifi!=null)wifi.IsEnabled=true;login.IsEnabled=true;actions.IsEnabled=true;Refresh();}
    }
    private void Refresh() {
        var session=auth.Read();login.Visibility=session==null?Visibility.Visible:Visibility.Collapsed;actions.Visibility=session==null?Visibility.Collapsed:Visibility.Visible;
        sharedWarning.Visibility=sharedSpace.IsChecked==true?Visibility.Visible:Visibility.Collapsed;
        var account=sharedSpace.IsChecked==true?"shared":session?.User;
        var last=session==null?0:storage.Snapshot().Exchange.LastSuccessFor(session.Endpoint,account!);
        sessionLabel.Text=session==null?SyncText.T("Sync_515b79cbb5"):SyncText.T("Sync_752d2f234f")+session.Email+(last>0?SyncText.T("Sync_eff80d43ef")+DateTimeOffset.FromUnixTimeMilliseconds(last).LocalDateTime.ToString("dd.MM.yyyy HH:mm"):"");
        if (session != null) {
            bool hasTg = session.HasTelegram || session.Email.StartsWith("tg_");
            bool hasMax = session.HasMax;
            tgBadge.Visibility = hasTg ? Visibility.Visible : Visibility.Collapsed;
            maxBadge.Visibility = hasMax ? Visibility.Visible : Visibility.Collapsed;
            badgesPanel.Visibility = (hasTg || hasMax) ? Visibility.Visible : Visibility.Collapsed;
            if (hasTg && hasMax) {
                linkPanel.Visibility = Visibility.Collapsed;
            } else {
                linkPanel.Visibility = Visibility.Visible;
                linkMaxBtn.Visibility = hasMax ? Visibility.Collapsed : Visibility.Visible;
                linkTgBtn.Visibility = hasTg ? Visibility.Collapsed : Visibility.Visible;
            }
        }
    }
}
public sealed class SharedProfilePickerWindow: Window
{
    public IReadOnlyList<string>? Selected {get;private set;}
    public SharedProfilePickerWindow(IReadOnlyList<SharedProfileInfo> profiles) {
        Title=SyncText.T("Sync_shared_choose");Width=600;Height=560;MinWidth=430;MinHeight=320;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        SetResourceReference(BackgroundProperty,"WindowBackgroundBrush");SetResourceReference(ForegroundProperty,"WindowForegroundBrush");FontSize=14;
        var root=new Grid {Margin=new Thickness(20)};root.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition());root.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});Content=root;
        root.Children.Add(new TextBlock {Text=SyncText.T("Sync_shared_select_help"),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)});
        var list=new StackPanel();var scroll=new ScrollViewer {Content=list,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetRow(scroll,1);root.Children.Add(scroll);
        var choices=new List<(CheckBox Box,SharedProfileInfo Profile)>();
        foreach(var profile in profiles) {
            var title=profile.Name+(string.IsNullOrWhiteSpace(profile.BoardNumber)?"":" · "+string.Format(SyncText.T("Sync_shared_board"),profile.BoardNumber));
            var box=new CheckBox {Content=title,Tag=profile,Margin=new Thickness(4,8,4,2),FontWeight=FontWeights.SemiBold};list.Children.Add(box);
            list.Children.Add(new TextBlock {Text=DateTimeOffset.FromUnixTimeMilliseconds(profile.UpdatedAt).LocalDateTime.ToString("dd.MM.yyyy HH:mm"),Margin=new Thickness(28,0,4,8)});choices.Add((box,profile));
        }
        if(profiles.Count==0)list.Children.Add(new TextBlock {Text=SyncText.T("Sync_shared_empty"),Margin=new Thickness(0,16,0,16)});
        var buttons=new WrapPanel {HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,12,0,0)};Grid.SetRow(buttons,2);root.Children.Add(buttons);
        var cancel=new Button {Content=SyncText.T("Sync_0ec753be8d"),Padding=new Thickness(12,8,12,8),Margin=new Thickness(6,0,0,0)};
        var download=new Button {Content=SyncText.T("Sync_shared_download"),Padding=new Thickness(12,8,12,8),Margin=new Thickness(6,0,0,0),IsEnabled=false};
        void Refresh()=>download.IsEnabled=choices.Any(c=>c.Box.IsChecked==true);foreach(var choice in choices)choice.Box.Checked+=(_,_)=>Refresh();foreach(var choice in choices)choice.Box.Unchecked+=(_,_)=>Refresh();
        cancel.Click+=(_,_)=>Close();download.Click+=(_,_)=>{Selected=choices.Where(c=>c.Box.IsChecked==true).Select(c=>c.Profile.ProfileId).ToList();DialogResult=true;};buttons.Children.Add(cancel);buttons.Children.Add(download);
        Loaded+=(_,_)=>logbook.Helpers.DarkTitleBarHelper.Apply(this);
    }
}
public sealed class DesktopConflictWindow: Window
{
    public bool? UseLocal {get;private set;}
    public DesktopConflictWindow(DesktopConflict conflict) {
        Title=SyncText.T("Sync_e7ad254ef4");Width=720;Height=570;MinWidth=470;MinHeight=320;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        SetResourceReference(BackgroundProperty,"WindowBackgroundBrush");SetResourceReference(ForegroundProperty,"WindowForegroundBrush");FontSize=14;
        var root=new Grid {Margin=new Thickness(20)};root.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition());root.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});Content=root;
        root.Children.Add(new TextBlock {Text=SyncText.T("Sync_4373777816")+conflict.Revision,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)});
        var text=new TextBox {Text=Summary(conflict),IsReadOnly=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetRow(text,1);root.Children.Add(text);
        var buttons=new WrapPanel {HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,12,0,0)};Grid.SetRow(buttons,2);root.Children.Add(buttons);
        void Add(string name,bool? result) {var b=new Button {Content=name,Padding=new Thickness(12,8,12,8),Margin=new Thickness(6,4,0,0)};b.Click+=(_,_)=>{UseLocal=result;Close();};buttons.Children.Add(b);}
        Add(SyncText.T("Sync_8d70566571"),true);Add(SyncText.T("Sync_646a0eecc4"),false);Add(SyncText.T("Sync_0ec753be8d"),null);
        Loaded+=(_,_)=>logbook.Helpers.DarkTitleBarHelper.Apply(this);
    }
    public static string Summary(DesktopConflict conflict) {
        var local=(JObject)ExchangeCodec.Parse(conflict.Local)["tables"]!;var remote=(JObject)ExchangeCodec.Parse(conflict.Remote)["tables"]!;var text=new StringBuilder(SyncText.T("Sync_a298fc2408"));
        foreach(var name in new[]{"profiles","days","launches","chast1_records","vr80_records","ta14_records","vk250001_records","vk250002_records"})text.AppendLine($"{name}: {((JArray)local[name]!).Count} / {((JArray)remote[name]!).Count}");
        text.AppendLine(SyncText.T("Sync_b4bc7e1b0d"));int total=0;
        foreach(var table in ExchangeCodec.Tables) {
            var a=local[table]!.OfType<JObject>().ToDictionary(r=>(string)r["id"]!);var b=remote[table]!.OfType<JObject>().ToDictionary(r=>(string)r["id"]!);
            foreach(var id in a.Keys.Union(b.Keys)) {
                a.TryGetValue(id,out var left);b.TryGetValue(id,out var right);if(JToken.DeepEquals(left,right))continue;
                if(total++>=30)continue;var row=left??right!;text.AppendLine($"{table}, {row["date"]??row["name"]??id[..8]}");
                if(left==null||right==null)text.AppendLine(left==null?SyncText.T("Sync_1c5cd51ca0"):SyncText.T("Sync_3e0d361136"));
                else foreach(var f in left.Properties().Where(p=>!JToken.DeepEquals(p.Value,right[p.Name])).Take(5)) {
                    string Short(JToken? v) {var s=v?.ToString()??"—";return s.Length>120?s[..120]+"…":s;}
                    text.AppendLine($"  {f.Name}: {Short(f.Value)} → {Short(right[f.Name])}");
                }
            }
        }
        if(total>30)text.AppendLine(SyncText.T("Sync_429687917e")+(total-30));return text.ToString();
    }
}
