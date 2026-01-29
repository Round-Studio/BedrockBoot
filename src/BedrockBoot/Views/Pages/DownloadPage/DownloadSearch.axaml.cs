using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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
    
    // 保存搜索状态
    private static string _lastSearchKey = string.Empty;
    private static SearchResourceType _lastSearchType = SearchResourceType.Unknow;
    
    public string SearchKey => KeyBox.Text;
    
    public DownloadSearch()
    {
        InitializeComponent();

        DownloadSearchView = this;
        SearchFrame = NavigationFrame;
        
        // 恢复上次的搜索关键词
        if (!string.IsNullOrEmpty(_lastSearchKey))
        {
            KeyBox.Text = _lastSearchKey;
        }
        
        // 延迟导航，确保UI已加载完成
        Dispatcher.UIThread.Post(() =>
        {
            NavigationFrame.NavigateTo(new SearchDefault());
            
            // 如果有保存的搜索记录，自动导航到详细搜索页面
            if (!string.IsNullOrEmpty(_lastSearchKey))
            {
                if (SearchDetailed == null)
                {
                    NavigationFrame.NavigateTo(new SearchDetailed());
                }
                
                SearchDetailed.OnSearch(new SearchInfo()
                {
                    Key = _lastSearchKey,
                    Type = _lastSearchType
                });
            }
        });
        
        KeyBox.KeyDown += (sender, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                SearchBtn_OnClick(null, null);
            }
        };
    }

    private void SearchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SearchDetailed == null)
        {
            NavigationFrame.NavigateTo(new SearchDetailed());
        }
        
        var searchKey = KeyBox.Text;
        var searchType = SearchResourceType.Unknow; // 您可能需要从UI获取实际类型
        
        // 保存搜索状态
        _lastSearchKey = searchKey;
        _lastSearchType = searchType;

        SearchDetailed.OnSearch(new SearchInfo()
        {
            Key = searchKey,
            Type = searchType
        });
    }
    
    // 如果需要从外部设置搜索类型，可以添加这个方法
    public void SetSearchType(SearchResourceType type)
    {
        _lastSearchType = type;
    }
    
    // 如果需要清空搜索历史，可以添加这个方法
    public static void ClearSearchHistory()
    {
        _lastSearchKey = string.Empty;
        _lastSearchType = SearchResourceType.Unknow;
    }
}