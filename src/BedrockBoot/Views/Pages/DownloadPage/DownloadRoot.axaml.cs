using Avalonia.Interactivity;
using BedrockBoot.Base.Entry;

namespace BedrockBoot.Views.Pages.DownloadPage;

public partial class DownloadRoot : BedrockBootPage
{
    public DownloadRoot()
    {
        InitializeComponent();
        Instance = this;

        MainFrame.NavigateTo(new DownloadSearch());
    }

    public static DownloadRoot Instance { get; private set; }

    public void NavigateTo(object page)
    {
        BackBtn.IsVisible = false;
        if (page.GetType() != typeof(DownloadSearch)) BackBtn.IsVisible = true;

        MainFrame.NavigateTo(page);
    }

    private void BackBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        NavigateTo(new DownloadSearch());
    }
}