using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.Views.Pages.MultiplayerPage;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainMultiplayerPage : BedrockBootPage
{
    public MainMultiplayerPage()
    {
        InitializeComponent();
        
        MainFrame.NavigateTo(new MultiplayerDependenceDownload());
    }
}