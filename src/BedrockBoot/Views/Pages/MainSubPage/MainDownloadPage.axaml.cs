using Avalonia.Controls;
using BedrockBoot.Base.Entry;
using BedrockBoot.Views.Pages.DownloadSubPage;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainDownloadPage : BedrockBootPage
{
    public MainDownloadPage()
    {
        InitializeComponent();
        NavFrame.NavigateTo(new DownloadGamePage());
    }
}