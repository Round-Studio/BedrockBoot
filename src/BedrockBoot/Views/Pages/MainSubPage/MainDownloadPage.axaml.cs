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

        IsEdit = true;
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            var tag = ((ListBoxItem)PageSel.SelectedItem).Tag.ToString();
            switch (tag)
            {
                case "Game":
                    NavFrame.NavigateTo(new DownloadGamePage());
                    break;
                case "Assets":
                    NavFrame.NavigateTo(new DownloadAssetsPage());
                    break;
            }
        }
    }
}