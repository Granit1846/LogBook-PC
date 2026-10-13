using logbook.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using logbook.Services;

namespace logbook.Views;
public sealed class SyncHistoryWindow: Window {
    public SyncHistoryWindow(string folder) {
        Title=SyncText.T("Sync_236ecb49e8");Width=660;Height=560;MinWidth=400;MinHeight=300;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        SetResourceReference(BackgroundProperty,"WindowBackgroundBrush");SetResourceReference(ForegroundProperty,"WindowForegroundBrush");FontSize=14;
        var root=new Grid {Margin=new Thickness(16)};root.RowDefinitions.Add(new RowDefinition());root.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});Content=root;
        var scroll=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto};root.Children.Add(scroll);var content=new StackPanel();scroll.Content=content;
        try {
            var history=new SyncHistory(folder).Read();
            if(history.Count==0)content.Children.Add(new TextBlock {Text=SyncText.T("Sync_9375d003e6")});
            foreach(var entry in history) {
                var card=new StackPanel {Margin=new Thickness(0,8,0,8)};content.Children.Add(card);
                card.Children.Add(new TextBlock {Text=DateTimeOffset.FromUnixTimeMilliseconds(entry.Time).LocalDateTime.ToString("dd.MM.yyyy HH:mm:ss")+" · "+SyncText.Display(entry.Method)});
                card.Children.Add(new TextBlock {Text=SyncText.Display(entry.Direction),FontWeight=FontWeights.Bold});
                card.Children.Add(new TextBlock {Text=SyncText.Display(entry.Result),TextWrapping=TextWrapping.Wrap});
                if(entry.Backup.Length>0) {
                    var button=new Button {Content=SyncText.T("Sync_b0166b7bcd"),HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,6,0,0),Padding=new Thickness(8,4,8,4)};card.Children.Add(button);
                    button.Click+=(_,_)=> {
                        try {
                            if(!File.Exists(entry.Backup))throw new IOException(SyncText.T("Sync_a080ef6c03"));
                            var start=new ProcessStartInfo("explorer.exe") {UseShellExecute=true};start.ArgumentList.Add("/select,"+Path.GetFullPath(entry.Backup));Process.Start(start);
                        } catch(Exception error){MessageBox.Show(this,error.Message,SyncText.T("Sync_72f931bda2"));}
                    };
                }
                content.Children.Add(new Separator());
            }
        } catch(Exception error){content.Children.Add(new TextBlock {Text=SyncText.T("Sync_4c6944a7d0")+error.Message,TextWrapping=TextWrapping.Wrap});}
        var close=new Button {Content=SyncText.T("Sync_4ae50d3073"),HorizontalAlignment=HorizontalAlignment.Right,Padding=new Thickness(12,8,12,8)};close.Click+=(_,_)=>Close();Grid.SetRow(close,1);root.Children.Add(close);
        Loaded+=(_,_)=>logbook.Helpers.DarkTitleBarHelper.Apply(this);
    }
}
