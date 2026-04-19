using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.Server;
using BedrockBoot.Models.Pack.Game.Server;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Control.Items;

public partial class GameServerItem : UserControl
{
    private readonly ServerItemInfo ServerItemInfo;

    public GameServerItem()
    {
        InitializeComponent();
    }

    public GameServerItem(ServerItemInfo info) : this()
    {
        ServerItemInfo = info;
        ServerName.Text = info.ServerName;
        ServerDescription.Text = $"{info.ServerAddress}:{info.ServerPort}";

        _ = UpdateServerStatusAsync();
    }

    private static I18nManager i18n => I18nManager.Instance;

    public Action<ServerItemInfo>? DeleteServer { get; set; }

    private async Task UpdateServerStatusAsync()
    {
        var checker = new ServerChecker();
        try
        {
            // 使用 await 替代 .Result 避免阻塞
            var sta = await checker.GetServerStatusAsync(ServerItemInfo);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (sta.Players != null)
                {
                    ServerMotd.IsVisible = true;
                    PlayerBox.IsVisible = true;
                    ServerMotd.MinecraftText = string.IsNullOrEmpty(sta.MOTD) ? "" : sta.MOTD;
                    DelayBox.Text = $"{sta.Delay} ms";
                    PlayerBox.Text = $"{sta.Players.Online} / {sta.Players.Max}";

                    // 根据延迟设置颜色
                    DelayBox.Background = sta.Delay < 100 ? Brushes.Green :
                        sta.Delay < 250 ? Brushes.Orange : Brushes.Red;
                }
                else
                {
                    SetOfflineStatus();
                }
            });
        }
        catch
        {
            await Dispatcher.UIThread.InvokeAsync(SetOfflineStatus);
        }
    }

    private void SetOfflineStatus()
    {
        ServerMotd.IsVisible = false;
        PlayerBox.IsVisible = false;
        DelayBox.Text = i18n["Instance.Server.Status.Offline"];
        DelayBox.Background = Brushes.DarkRed;
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = i18n["Instance.Server.Delete.Title"],
            Content = $"{i18n["Instance.Server.Delete.Content"]}\n{i18n["Common.Action.Irreversible"]}",
            CloseButtonText = i18n["MainWindow.Common.Confirm"],
            PrimaryButtonText = i18n["MainWindow.Common.Cancel"],
            CloseAction = () => { DeleteServer?.Invoke(ServerItemInfo); }
        });
    }

    private void LaunchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var versionConf = ServerItemInfo.VersionConfig;
        if (versionConf == null) return;

        versionConf.Config.IsEditModel = false;
        // 构造快速加入指令
        versionConf.Config.OtherCommand =
            $"minecraft://connect/?serverUrl={ServerItemInfo.ServerAddress}&serverPort={ServerItemInfo.ServerPort} {versionConf.Config.OtherCommand}";

        TaskLaunchGameItem.Launch(versionConf);
    }
}