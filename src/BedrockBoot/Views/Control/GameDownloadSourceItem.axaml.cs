using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Info;

namespace BedrockBoot.Views.Control;

public partial class GameDownloadSourceItem : UserControl
{
    public GameDownloadSourceItem()
    {
        InitializeComponent();
    }
    public GameDownloadSourceItem(GameDownloadUrlInfo info):this()
    {
        SourceHost.Text = info.Host;
        SourceUrl.Text = info.Url;
    }
}