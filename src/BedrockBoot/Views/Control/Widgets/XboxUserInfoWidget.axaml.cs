using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Core.Models.Xbox;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Views.Control.Widgets;

public partial class XboxUserInfoWidget : UserControl
{
    public XboxUserInfoWidget()
    {
        InitializeComponent();

        Task.Run(async () =>
        {
            if (GlobalModel.XboxUserInfo != null)
                Dispatcher.UIThread.Invoke(() =>
                {
                    BoxContent.IsVisible = true;
                    PlayerName.Text = GlobalModel.XboxUserInfo.Gamertag;
                });
            else
                try
                {
                    var checker = new XboxLoginStatusChecker();
                    var status = await checker.GetDetailedXboxStatus();
                    GlobalModel.XboxUserInfo = status.XboxUserInfo;

                    Dispatcher.UIThread.Invoke(() =>
                    {
                        BoxContent.IsVisible = true;
                        PlayerName.Text = GlobalModel.XboxUserInfo.Gamertag;
                    });
                }
                catch
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        BoxContent.IsVisible = true;
                        PlayerName.Text = "无法获取";
                    });
                }

            Dispatcher.UIThread.Invoke(() => LoadRing.IsVisible = false);
        });
    }
}