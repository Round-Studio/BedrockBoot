using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Pack.Market;
using BedrockBoot.Views.Pages.DownloadPage.ResultSubPage.PluginMarket;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawDownloadPluginContent : UserControl
{
    public DrawDownloadPluginContent()
    {
        InitializeComponent();
    }

    public DrawDownloadPluginContent(MarketResponse.PluginInfo info) : this()
    {
        MarketNavigation.NavigateTo(new PluginMarketInfo(info));
    }
}