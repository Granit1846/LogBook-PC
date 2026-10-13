using logbook.Helpers;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using logbook.Services;
using QRCoder;

namespace logbook.Views;
public sealed class LanSyncWindow: Window {
    private LanSyncServer? server;
    public LanSyncWindow(StorageService storage,Action downloaded) {
        Title=SyncText.T("Sync_73773ab330");Width=540;Height=660;MinWidth=380;MinHeight=350;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        SetResourceReference(BackgroundProperty,"WindowBackgroundBrush");SetResourceReference(ForegroundProperty,"WindowForegroundBrush");FontSize=14;
        var root=new Grid {Margin=new Thickness(20)};root.RowDefinitions.Add(new RowDefinition());root.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});Content=root;
        var scroll=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto};root.Children.Add(scroll);
        var content=new StackPanel();scroll.Content=content;
        content.Children.Add(new TextBlock {Text=SyncText.T("Sync_98091a44e7"),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)});
        var addresses=new ComboBox {ItemsSource=LanSyncServer.Addresses(),Margin=new Thickness(0,0,0,12)};content.Children.Add(new TextBlock {Text=SyncText.T("Sync_79e3687abe")});content.Children.Add(addresses);
        var image=new Image {Width=300,Height=300,Margin=new Thickness(0,8,0,8)};content.Children.Add(image);
        var status=new TextBlock {TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8)};content.Children.Add(status);
        content.Children.Add(new TextBlock {Text=SyncText.T("Sync_cc5cc60191"),TextWrapping=TextWrapping.Wrap});
        addresses.SelectionChanged+=(_,_)=> {
            if(server==null||addresses.SelectedItem is not string host)return;
            using var qr=QRCodeGenerator.GenerateQrCode(server.Pairing(host),QRCodeGenerator.ECCLevel.M);
            using var renderer=new PngByteQRCode(qr);using var bytes=new MemoryStream(renderer.GetGraphic(8));
            var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.StreamSource=bytes;bitmap.EndInit();bitmap.Freeze();image.Source=bitmap;
        };
        var close=new Button {Content=SyncText.T("Sync_ea7c3568a6"),Padding=new Thickness(12,8,12,8),HorizontalAlignment=HorizontalAlignment.Right};close.Click+=(_,_)=>Close();Grid.SetRow(close,1);root.Children.Add(close);
        Loaded+=(_,_)=> {
            logbook.Helpers.DarkTitleBarHelper.Apply(this);
            try {
                if(addresses.Items.Count==0){status.Text=SyncText.T("Sync_b14aba19d6");return;}
                server=new LanSyncServer(storage,Dispatcher,downloaded,message=>status.Text=message);addresses.SelectedIndex=0;status.Text=SyncText.T("Sync_9672a70242");
            } catch(Exception error){status.Text=error.Message;}
        };
        Closed+=(_,_)=>server?.Dispose();
    }
}
