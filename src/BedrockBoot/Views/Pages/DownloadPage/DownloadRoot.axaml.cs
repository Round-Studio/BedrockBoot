using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation;

namespace BedrockBoot.Views.Pages.DownloadPage;

public partial class DownloadRoot : BedrockBootPage
{
    public static NavigationFrame DownloadMainFrame;
    public DownloadRoot()
    {
        InitializeComponent();
        DownloadMainFrame = MainFrame;
        
        MainFrame.NavigateTo(new DownloadSearch());
    }
}