using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.Server;
using BedrockBoot.Models.Pack.Game.Server;

namespace BedrockBoot.Views.Control;

public partial class GameServerItem : UserControl
{
    public GameServerItem()
    {
        InitializeComponent();
    }

    public GameServerItem(ServerItemInfo info) : this()
    {
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
                    ServerMotd.MinecraftText = sta.MOTD;
                    DelayBox.Text = $"{sta.Delay} ms";
                    PlayerBox.Text = $"{sta.Players.Online} / {sta.Players.Max}";
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
}