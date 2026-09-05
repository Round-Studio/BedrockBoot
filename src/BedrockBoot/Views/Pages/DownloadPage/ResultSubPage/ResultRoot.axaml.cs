using System;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Helpers;
using BedrockBoot.Interface.Download;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Service;
using BedrockBoot.Service.Download;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;

public partial class ResultRoot : UserControl
{
    private ImageLoader imageLoader = new ImageLoader();

    public ResultRoot()
    {
        InitializeComponent();
    }

    public ResultRoot(SearchResultItemInfo info) : this()
    {
        SearchResultItemInfo = info;
        // 触发异步更新，不阻塞 UI 线程
        _ = UpdateAsync();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        imageLoader.Dispose();
    }

    private static I18nManager i18n => I18nManager.Instance;

    public SearchResultItemInfo SearchResultItemInfo { get; set; } = null!;
    private IDownloadResult _downloadService;

    /// <summary>
    ///     异步加载资源详情
    /// </summary>
    public async Task UpdateAsync()
    {
        _downloadService = SearchResultItemInfo.ResourceType switch
        {
            SearchResourceType.ResourcePack => new CurseForgeDownloadResult(SearchResultItemInfo),
            SearchResourceType.PluginPack => new PluginDownloadResult(SearchResultItemInfo)
        };

        ResourceName.Text = SearchResultItemInfo.Name;
        AuthorText.Text = $"{i18n["Download.Result.Author.Prefix"]} {string.Join(", ", SearchResultItemInfo.Authors)}";
        ResourceName2.Text = SearchResultItemInfo.Name;
        AuthorText2.Text = $"{string.Join(", ", SearchResultItemInfo.Authors)}";
        DescriptionText.Text = SearchResultItemInfo.Description;
        UpdataDateText.Text = DateHelper.GetRelativeTime(SearchResultItemInfo.DateUpdated);

        var hasWebsite = !string.IsNullOrEmpty(SearchResultItemInfo.SourceWebsite);
        HyperlinkButton.IsVisible = hasWebsite;
        HyperlinkButton2.IsVisible = hasWebsite;
        if (hasWebsite && Uri.TryCreate(SearchResultItemInfo.SourceWebsite, UriKind.Absolute, out var uri))
        {
            HyperlinkButton2.NavigateUri = uri;
            HyperlinkButton.NavigateUri = uri;
        }

        LabelsBox.Children.Clear();
        if (SearchResultItemInfo.Labels is { Count: > 0 })
        {
            LabelsBox.IsVisible = true;
            foreach (var s in SearchResultItemInfo.Labels) LabelsBox.Children.Add(new LabelBox { Text = s });
        }

        _ = Task.Run(async () =>
        {
            var count = await _downloadService.GetDownloadCount();
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                DownloadCountText.Text = count.ToString("N0");
                DownloadCountPanel.IsVisible = true;
            });
        });

        var icon = await imageLoader.LoadImageBrushAsync(SearchResultItemInfo.IconUri);

        if (icon != null)
        {
            IconBox.Source = icon;
            ResourceIcon.Source = icon;
            ResourceIconIcon.IsVisible = false;
            IconFont.IsVisible = false;
        }
    }

    /// <summary>
    ///     打开下载抽屉
    /// </summary>
    private void GetResourceBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        /*var modData = JsonSerializer.Deserialize<CurseForgeResponse.ModData>(SearchResultItemInfo.JsonData);
        if (modData == null) return;

        GlobalModel.MainWindow.OpenDraw(
            new DrawDownloadCurseForgeResourceContent(modData),
            $"{i18n["Download.Action.GetResource"]}: {SearchResultItemInfo.Name}");*/

        PageSelect.SelectedItem = ItemFiles;
    }

    /// <summary>
    ///     复制分享链接
    /// </summary>
    private void CopyName_OnClick(object? sender, RoutedEventArgs e)
    {
        var modData = JsonSerializer.Deserialize<CurseForgeResponse.ModData>(SearchResultItemInfo.JsonData);
        if (modData == null) return;

        var shareContent = string.Format(i18n["Download.Result.Share.Format"],
            SearchResultItemInfo.Name, SearchResultItemInfo.SourceWebsite);

        CopyService.SetClipboard(shareContent, CopyType.Resource, modData.Id);

        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = "剪切板",
            Message = i18n["Download.Result.Share.Success"]
        });
    }

    private void ScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (MainScrollViewer != null)
        {
            var value = MainScrollViewer.Offset.Y;
            if (value >= 120)
                SmallBox.Margin = new Thickness(30, 25, 30, 0);
            else
                SmallBox.Margin = new Thickness(30, -72, 30, 0);
        }
    }

    private void GoTopBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainScrollViewer.Offset = new Vector(MainScrollViewer.Offset.X, 0);
    }

    private void PageSelect_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (PageSelect == null) return;
        if (PageSelect.SelectedItem == null) return;
        var tag = (PageSelect.SelectedItem as ListBoxItem)!.Tag!.ToString();
        UserControl page = tag switch
        {
            "Description" => new ResultDescription(_downloadService)
            {
                NotFountDescription = () =>
                {
                    ItemDescription.IsVisible = false;
                    PageSelect.SelectedItem = ItemFiles;
                }
            },
            "Files" => new ResultFiles(_downloadService)
        };

        NavigationFrame.NavigateTo(page);
    }
}