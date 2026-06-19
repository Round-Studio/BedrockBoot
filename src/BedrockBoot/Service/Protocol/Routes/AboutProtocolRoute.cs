using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Views.Pages;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.OtherPage;

namespace BedrockBoot.Service.Protocol.Routes;

public class AboutProtocolRoute : IProtocolRoute
{
    public string RouteName => "about";

    public Task ExecuteAsync(string[] segments, IReadOnlyDictionary<string, string> queryParams)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            MainPage.Instance.SelTag.SelectedIndex = 5;
            MainSettingPage.NavigateTo(new AboutPage());
        });

        return Task.CompletedTask;
    }
}
