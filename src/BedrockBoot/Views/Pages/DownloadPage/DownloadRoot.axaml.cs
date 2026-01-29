using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.Views.Pages.DownloadPage.SearchSubPage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation;

namespace BedrockBoot.Views.Pages.DownloadPage;

public partial class DownloadRoot : BedrockBootPage
{
    public static DownloadRoot Instance { get; private set; }
    public DownloadRoot()
    {
        InitializeComponent();
        Instance = this;
        
        MainFrame.NavigateTo(new DownloadSearch());
    }

    public void NavigateTo(object page)
    {
        BackBtn.IsVisible = false;
        if (page.GetType() != typeof(DownloadSearch))
        {
            BackBtn.IsVisible = true;
        }
        
        MainFrame.NavigateTo(page);
    }

    private void BackBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        NavigateTo(new DownloadSearch());
    }
}