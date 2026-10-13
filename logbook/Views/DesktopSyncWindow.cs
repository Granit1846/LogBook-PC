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
    private readonly bool forcePersonalOnly;
    private bool busy;
    private Button? wifi;
    public DesktopSyncWindow(SettingsService settings,StorageService storage,Action downloaded,string? authFolder=null,bool personalOnly=false)
    {
        this.settings=settings;this.storage=storage;this.downloaded=downloaded;
        auth=new DesktopAuth(authFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"LogbookApp"));
        forcePersonalOnly=personalOnly;
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
        login.Children.Add(new TextBlock {Text="Email"});login.Children.Add(email);login.Children.Add(new TextBlock {Text=SyncText.T("Sync_14f7c63cc1")});login.Children.Add(password);
        var buttons=new StackPanel {Orientation=Orientation.Horizontal};login.Children.Add(buttons);
        var enter=Button(SyncText.T("Sync_939e95a11d"),async()=>await SignIn(false));var register=Button(SyncText.T("Sync_7224765e44"),async()=>await SignIn(true));buttons.Children.Add(enter);buttons.Children.Add(register);
        content.Children.Add(actions);
        personalSpace.SetResourceReference(ForegroundProperty, "WindowForegroundBrush");
        sharedSpace.SetResourceReference(ForegroundProperty, "WindowForegroundBrush");
        var spaces=new WrapPanel();spaces.Children.Add(personalSpace);spaces.Children.Add(sharedSpace);actions.Children.Add(spaces);
        actions.Children.Add(sharedWarning);actions.Children.Add(sync);actions.Children.Add(logout);
        personalSpace.Checked+=(_,_)=>Refresh();sharedSpace.Checked+=(_,_)=>Refresh();
        void UpdateSyncButtonLabel() => sync.Content = sharedSpace.IsChecked == true ? SyncText.T("Sync_SharedButtonAction") : SyncText.T("Sync_5822fb7d95");
        personalSpace.Checked+=(_,_)=>UpdateSyncButtonLabel();
        sharedSpace.Checked+=(_,_)=>UpdateSyncButtonLabel();
        sync.Click+=async(_,_)=>await Operation(async()=> {
            var history=new SyncHistory(settings.Settings.DataFolder);
            try {
            var session=auth.Read()??throw new InvalidOperationException(SyncText.T("Sync_4eb58e1e56"));
            if(sharedSpace.IsChecked==true) {
                // Общая синхронизация: ТОЛЬКО СКАЧИВАНИЕ с выбором профилей
                using var sharedClient=new DesktopSupabase(settings.Settings.SupabaseUrl,settings.Settings.SupabaseKey,auth,true);
                var catalog=await sharedClient.SharedProfileCatalog();
                var picker=new SharedProfilePickerWindow(catalog) {Owner=this};picker.ShowDialog();
                if(picker.Selected==null) {status.Text=SyncText.T("Sync_9c327e9ce5");return;}
                var packets=await sharedClient.PullSharedProfiles(picker.Selected);var current=storage.Snapshot();
                var local=ExchangeCodec.Encode(current).ToString(Newtonsoft.Json.Formatting.None);storage.Save(current);
                var merged=ExchangeCodec.MergeSharedProfiles(ExchangeCodec.Parse(local),packets).ToString(Newtonsoft.Json.Formatting.None);
                storage.Download(merged,local,0,"","",true);downloaded();
                var completed=storage.Snapshot();completed.Exchange.SelectCheckpoint(session.Endpoint,"shared");completed.Exchange.LastSuccess=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();completed.Exchange.SaveCheckpoint();storage.Save(completed);
                status.Text=SyncText.T("Sync_shared_downloaded_only",packets.Count);
                history.Add(new SyncHistoryEntry(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),SyncText.T("Sync_shared_space"),SyncText.T("Sync_shared_received"),status.Text,storage.LastSyncBackup));return;
            }
            // Личная синхронизация: отправка в личное и общее, скачивание из личного
            using var personalClient=new DesktopSupabase(settings.Settings.SupabaseUrl,settings.Settings.SupabaseKey,auth,false);
            var engine=new DesktopSyncEngine(storage,personalClient,session.Endpoint,session.User);var result=await engine.Sync();
            while(result.Conflict!=null) {
                var choice=new DesktopConflictWindow(result.Conflict) {Owner=this};choice.ShowDialog();
                if(choice.UseLocal==null) {status.Text=SyncText.T("Sync_9c327e9ce5");history.Add(new SyncHistoryEntry(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),SyncText.T("Sync_60a6800444"),SyncText.T("Sync_5ebee19fe9"),status.Text));return;}
                result=await engine.Resolve(result.Conflict,choice.UseLocal.Value);
            }
            if(result.Downloaded)downloaded();
            // Также публикуем профили в общее пространство
            try {
                using var sharedClient=new DesktopSupabase(settings.Settings.SupabaseUrl,settings.Settings.SupabaseKey,auth,true);
                var publishing=storage.Snapshot();var packets=ExchangeCodec.SharedProfilePackets(publishing);storage.Save(publishing);
                await sharedClient.PublishSharedProfiles(packets);
            } catch { }
            status.Text=result.Message;
            history.Add(new SyncHistoryEntry(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),SyncText.T("Sync_60a6800444"),result.Downloaded?SyncText.T("Sync_b571b20ab1"):result.Direction,result.Message,result.Downloaded?storage.LastSyncBackup:""));
            } catch(Exception error) {history.Add(new SyncHistoryEntry(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),SyncText.T("Sync_60a6800444"),SyncText.T("Sync_72aecd9ad8"),LocalizeError(error)));throw;}
        });
        logout.Click+=(_,_)=> {auth.Clear();status.Text=SyncText.T("Sync_8e4bf12752");Refresh();};
        content.Children.Add(status);
        var close=Button(SyncText.T("Sync_4ae50d3073"),()=> {Close();return Task.CompletedTask;});close.HorizontalAlignment=HorizontalAlignment.Right;Grid.SetRow(close,1);root.Children.Add(close);
        Closing+=(_,e)=> {if(busy){e.Cancel=true;status.Text=SyncText.T("Sync_c572544936");}};
        Loaded+=(_,_)=>logbook.Helpers.DarkTitleBarHelper.Apply(this);Refresh();
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
    private static string LocalizeError(Exception error) {
        if (error is OperationCanceledException) return SyncText.T("Sync_2818a93db8");
        if (error is System.Net.Http.HttpRequestException) return SyncText.T("Sync_e48c5d1ecf");
        if (error is FormatException) return logbook.Helpers.LocalizationManager.CurrentLanguage == "en" ? "Data format error." : "Ошибка формата данных.";
        return error.Message;
    }
    private async Task Operation(Func<Task> action) {
        if(busy)return;busy=true;if(wifi!=null)wifi.IsEnabled=false;login.IsEnabled=false;actions.IsEnabled=false;status.Text=SyncText.T("Sync_9674b15f97");
        try {await action();}
        catch(Exception error) {status.Text=LocalizeError(error);}
        finally {busy=false;if(wifi!=null)wifi.IsEnabled=true;login.IsEnabled=true;actions.IsEnabled=true;Refresh();}
    }
    private void Refresh() {
        var session=auth.Read();login.Visibility=session==null?Visibility.Visible:Visibility.Collapsed;actions.Visibility=session==null?Visibility.Collapsed:Visibility.Visible;
                if (forcePersonalOnly) {
            personalSpace.IsChecked = true;
            personalSpace.Visibility = Visibility.Collapsed;
            sharedSpace.Visibility = Visibility.Collapsed;
            sharedWarning.Visibility = Visibility.Collapsed;
        } else {
            sharedWarning.Visibility=sharedSpace.IsChecked==true?Visibility.Visible:Visibility.Collapsed;
        }
        var account=sharedSpace.IsChecked==true?"shared":session?.User;
        var last=session==null?0:storage.Snapshot().Exchange.LastSuccessFor(session.Endpoint,account!);
        sessionLabel.Text=session==null?SyncText.T("Sync_515b79cbb5"):SyncText.T("Sync_752d2f234f")+session.Email+(last>0?SyncText.T("Sync_eff80d43ef")+DateTimeOffset.FromUnixTimeMilliseconds(last).LocalDateTime.ToString("dd.MM.yyyy HH:mm"):"");
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
            var title=profile.Name+(string.IsNullOrWhiteSpace(profile.BoardNumber)?"":" · "+SyncText.T("Sync_shared_board",profile.BoardNumber));
            var box=new CheckBox {Content=title,Tag=profile,Margin=new Thickness(4,8,4,2),FontWeight=FontWeights.SemiBold};box.SetResourceReference(ForegroundProperty, "WindowForegroundBrush");list.Children.Add(box);
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
