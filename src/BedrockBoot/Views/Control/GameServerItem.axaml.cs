using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;
using BedrockBoot.Base.Entry.Game.Pack.Server;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Server;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Control;

public partial class GameServerItem : UserControl
{
    public Action<ServerItemInfo>? DeleteServer { get; set; }
    private ServerItemInfo ServerItemInfo;
    public GameServerItem()
    {
        InitializeComponent();
    }

    public GameServerItem(ServerItemInfo info) : this()
    {
        ServerItemInfo = info;
        ServerName.Text = info.ServerName;
        ServerDescription.Text = $"{info.ServerAddress}:{info.ServerPort}";
        var checker = new ServerChecker();
        Task.Run(() =>
        {
            try
            {
                var sta = checker.GetServerStatusAsync(info).Result;
                Dispatcher.UIThread.Invoke(() =>
                {
                    ServerMotd.IsVisible = true;
                    PlayerBox.IsVisible = true;
                    ServerMotd.MinecraftText = string.IsNullOrEmpty(sta.MOTD) ? "" : sta.MOTD;
                    DelayBox.Text = $"{sta.Delay} ms";
                    if (sta.Players != null)
                        PlayerBox.Text = $"{sta.Players.Online} / {sta.Players.Max}";
                    else
                    {
                        ServerMotd.IsVisible = false;
                        PlayerBox.IsVisible = false;
                        DelayBox.Text = $"-1 ms";
                        DelayBox.Text = $"无法连接至服务器";
                        DelayBox.Background = Brushes.DarkRed;
                    }
                });
            }
            catch
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    DelayBox.Text = $"无法连接至服务器";
                    DelayBox.Background = Brushes.DarkRed;
                    ServerMotd.IsVisible = false;
                });
            }
        });
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo()
        {
            Title = "删除服务器",
            Content = "您确定要删除吗，这将永远无法恢复。",
            CloseButtonText = "确定",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                DeleteServer?.Invoke(ServerItemInfo);
            }
        });
    }

    private void LaunchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var versionConf = ServerItemInfo.VersionConfig;
        versionConf.Config.IsEditModel = false;
        versionConf.Config.OtherCommand =
            $"minecraft://connect/?serverUrl={ServerItemInfo.ServerAddress}&serverPort={ServerItemInfo.ServerPort} {versionConf.Config.OtherCommand}";
        TaskLaunchGameItem.Launch(versionConf);
    }
}