using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Views.Pages.DownloadPage.SearchSubPage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation;

namespace BedrockBoot.Views.Pages.DownloadPage;

public partial class DownloadSearch : UserControl
{
    public static NavigationFrame SearchFrame;
    public static SearchDetailed SearchDetailed;
    public static DownloadSearch DownloadSearchView;

    public DownloadSearch()
    {
        InitializeComponent();

        DownloadSearchView = this;
        SearchFrame = NavigationFrame;
        NavigationFrame.NavigateTo(new SearchDefault());

        KeyBox.KeyDown += (sender, e) =>
        {
            if (e.Key == Key.Enter) SearchBtn_OnClick(null, null);
        };
    }

    public string SearchKey => KeyBox.Text;

    private void SearchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SearchDetailed == null) NavigationFrame.NavigateTo(new SearchDetailed());

        SearchDetailed.OnSearch(new SearchInfo
        {
            Key = KeyBox.Text,
            Type = SearchResourceType.Unknow
        });
    }
}