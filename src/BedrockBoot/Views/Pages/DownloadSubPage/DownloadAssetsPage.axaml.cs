using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Views.Control.Items;

namespace BedrockBoot.Views.Pages.DownloadSubPage;

public partial class DownloadAssetsPage : UserControl
{
    public string Key => TextBox.Text!;
    
    // 添加分页相关字段
    private int _currentPage = 1;
    private int _totalPages = 0;
    private int _currentIndex = 0;
    private int _pageSize = 20;
    
    // 添加搜索状态
    private bool _isSearching = false;
    private CurseForgeApiClient _apiClient;

    public DownloadAssetsPage()
    {
        InitializeComponent();
        _apiClient = new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey);
        
        // 绑定回车键搜索
        TextBox.KeyDown += (sender, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                Search();
            }
        };
        
        Search();

        // 设置上翻页逻辑
        ResultPage.UpAction = () =>
        {
            if (_currentPage > 1 && !_isSearching)
            {
                GoToPage(_currentPage - 1);
            }
        };

        // 设置下翻页逻辑
        ResultPage.DownAction = () =>
        {
            if (_currentPage < _totalPages && !_isSearching)
            {
                GoToPage(_currentPage + 1);
            }
        };
    }

    /// <summary>
    /// 跳转到指定页码
    /// </summary>
    private void GoToPage(int pageNumber)
    {
        if (_isSearching) return;
        
        _currentPage = Math.Clamp(pageNumber, 1, _totalPages);
        _currentIndex = (_currentPage - 1) * _pageSize;
        
        SearchWithPagination();
    }

    /// <summary>
    /// 搜索（重置到第一页）
    /// </summary>
    public void Search()
    {
        // 重置分页状态
        _currentPage = 1;
        _currentIndex = 0;
        
        SearchWithPagination();
    }

    /// <summary>
    /// 带分页的搜索
    /// </summary>
    private void SearchWithPagination()
    {
        if (_isSearching) return;
        
        var key = Key;
        
        _isSearching = true;
        ResultPage.CleanPage();
        NoneBox.IsVisible = false;
        LoadingRing.IsVisible = true;
        
        Task.Run(async () =>
        {
            try
            {
                var items = await _apiClient.SearchModsAsync(key, pageSize: _pageSize, index: _currentIndex);
                
                Dispatcher.UIThread.Invoke(() =>
                {
                    if (items?.Data?.Count > 0)
                    {
                        // 更新总页数
                        _totalPages = (int)Math.Ceiling((double)items.Pagination.TotalCount / items.Pagination.PageSize);
                        
                        ResultPage.Update(
                            new DownloadAssetsResultPage(items.Data),
                            _totalPages,
                            _currentPage);
                    }
                    else
                    {
                        NoneBox.IsVisible = true;
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    NoneBox.IsVisible = true;
                    Console.WriteLine($@"搜索失败: {ex}");
                });
            }
            finally
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    LoadingRing.IsVisible = false;
                    _isSearching = false;
                });
            }
        });
    }

    /// <summary>
    /// 点击搜索按钮
    /// </summary>
    private void SearchBtn_OnClick(object? sender, RoutedEventArgs e) => Search();
}