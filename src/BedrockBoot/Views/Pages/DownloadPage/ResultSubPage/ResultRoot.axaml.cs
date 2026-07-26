using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum;
using BedrockBoot.Helpers;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Service;
using BedrockBoot.Views.Control.Widgets;
using BedrockBoot.Views.DrawContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;

public partial class ResultRoot : UserControl
{
	private ImageLoader imageLoader = ImageLoader.Shared;
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

    private static I18nManager i18n => I18nManager.Instance;

    public SearchResultItemInfo SearchResultItemInfo { get; set; } = null!;

    /// <summary>
    ///     异步加载资源详情
    /// </summary>
    public async Task UpdateAsync()
    {
        // 1. 基础文字信息
        ResourceName.Text = SearchResultItemInfo.Name;
        AuthorText.Text = $"{i18n["Download.Result.Author.Prefix"]} {string.Join(", ", SearchResultItemInfo.Authors)}";
        ResourceName2.Text = SearchResultItemInfo.Name;
        AuthorText2.Text = $"{string.Join(", ", SearchResultItemInfo.Authors)}";
        DescriptionText.Text = SearchResultItemInfo.Description;
        DownloadCountText.Text = SearchResultItemInfo.DownloadCount.ToString("N0"); // 格式化数字
        UpdataDateText.Text = DateHelper.GetRelativeTime(SearchResultItemInfo.DateUpdated);

        // 2. 外部链接
        var hasWebsite = !string.IsNullOrEmpty(SearchResultItemInfo.SourceWebsite);
        HyperlinkButton.IsVisible = hasWebsite;
        HyperlinkButton2.IsVisible = hasWebsite;
        if (hasWebsite && Uri.TryCreate(SearchResultItemInfo.SourceWebsite, UriKind.Absolute, out var uri))
        {
            HyperlinkButton2.NavigateUri = uri;
            HyperlinkButton.NavigateUri = uri;
        }

        // 3. 预览图列表渲染
        PreviewList.Children.Clear();
        if (SearchResultItemInfo.Images is { Count: > 0 })
        {
            PreviewCard.IsVisible = true;
            foreach (var image in SearchResultItemInfo.Images)
                PreviewList.Children.Add(new LocalImageRenderWidget(image) { Width = 290 });
        }

        // 4. 标签渲染
        LabelsBox.Children.Clear();
        if (SearchResultItemInfo.Labels is { Count: > 0 })
        {
            LabelsBox.IsVisible = true;
            foreach (var s in SearchResultItemInfo.Labels) LabelsBox.Children.Add(new LabelBox { Text = s });
        }

        // 5. 异步图标加载
        var icon = await imageLoader.LoadImageBrushAsync(SearchResultItemInfo.IconUri);
	

		if (icon != null)
        {
			IconBox.Source = icon;
            ResourceIcon.Source = icon;
            ResourceIconIcon.IsVisible = false;
            IconFont.IsVisible = false;
        }

        // 6. 获取 CurseForge 详细 HTML 描述
        await LoadDetailedDescription();
    }

    private async Task LoadDetailedDescription()
    {
        try
        {
            var apiClient = new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey);
            var descriptionHtml = await apiClient.GetModDescriptionAsync(SearchResultItemInfo.Id);

            if (!string.IsNullOrEmpty(descriptionHtml))
            {
                DescriptionCard.IsVisible = true;
                DescriptionContent.Children.Clear();

                // 转换 HTML 到 Avalonia 控件
                var controls = HtmlToControlConverter.ConvertHtmlToControls(descriptionHtml);
                foreach (var control in controls) DescriptionContent.Children.Add(control);
            }
        }
        catch (Exception ex)
        {
            DescriptionCard.IsVisible = true;
            DescriptionContent.Children.Clear();
            DescriptionContent.Children.Add(new TextBlock
            {
                Text = $"{i18n["Download.Result.Error.LoadDescription"]}: {ex.Message}",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.Red,
                Margin = new Thickness(0, 10)
            });
        }
    }

    /// <summary>
    ///     打开下载抽屉
    /// </summary>
    private void GetResourceBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var modData = JsonSerializer.Deserialize<CurseForgeResponse.ModData>(SearchResultItemInfo.JsonData);
        if (modData == null) return;

        GlobalModel.MainWindow.OpenDraw(
            new DrawDownloadCurseForgeResourceContent(modData),
            $"{i18n["Download.Action.GetResource"]}: {SearchResultItemInfo.Name}");
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
            Title = i18n["Common.Clipboard.Title"],
            Message = i18n["Download.Result.Share.Success"]
        });
    }

    private void ScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (MainScrollViewer != null)
        {
            var value = MainScrollViewer.Offset.Y;
            if (value >= 80)
                SmallBox.Margin = new Thickness(30, 25, 30, 0);
            else
                SmallBox.Margin = new Thickness(30, -72, 30, 0);
        }
    }

    private void GoTopBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainScrollViewer.Offset = new Vector(MainScrollViewer.Offset.X, 0);
    }
}