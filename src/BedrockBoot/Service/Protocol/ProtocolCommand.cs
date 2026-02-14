using System.Linq;
using Avalonia.Threading;
using BedrockBoot.Views.Pages;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.OtherPage;

namespace BedrockBoot.Service.Protocol;

public class ProtocolCommand
{
    public static void OnCommand(string[] command)
    {
        if (command.ToList().Contains("about"))
            Dispatcher.UIThread.Invoke(() =>
            {
                MainPage.Instance.SelTag.SelectedIndex = 5;
                MainSettingPage.NavigateTo(new AboutPage());
            });
    }
}