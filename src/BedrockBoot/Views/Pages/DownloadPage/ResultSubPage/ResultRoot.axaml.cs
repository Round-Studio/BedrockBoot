using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
    public ResultRoot()
    {
        InitializeComponent();
    }

    public ResultRoot(SearchResultItemInfo info) : this()
    {
        SearchResultItemInfo = info;
        Update();
    }

    public SearchResultItemInfo SearchResultItemInfo { get; set; }

    public async Task Update()
    {
        ResourceName.Text = SearchResultItemInfo.Name;
        AuthorText.Text = $"By {string.Join(", ", SearchResultItemInfo.Authors)}";
        DescriptionText.Text = SearchResultItemInfo.Description;
        DownloadCountText.Text = SearchResultItemInfo.DownloadCount.ToString();
        UpdataDateText.Text = DateHelper.GetRelativeTime(SearchResultItemInfo.DateUpdated);
        HyperlinkButton.IsVisible = !string.IsNullOrEmpty(SearchResultItemInfo.SourceWebsite);
        HyperlinkButton.NavigateUri = string.IsNullOrEmpty(SearchResultItemInfo.SourceWebsite)
            ? new Uri("")
            : new Uri(SearchResultItemInfo.SourceWebsite);
        if (SearchResultItemInfo.Images != null &&
            SearchResultItemInfo.Images.Count > 0)
        {
            PreviewCard.IsVisible = true;
            SearchResultItemInfo.Images.ForEach(image => PreviewList.Children.Add(new LocalImageRenderWidget(image)
            {
                Width = 290
            }));
        }

        LabelsBox.Children.Clear();

        if (SearchResultItemInfo.Labels.Count > 0)
        {
            LabelsBox.IsVisible = true;
            SearchResultItemInfo.Labels.ForEach(s => LabelsBox.Children.Add(new LabelBox { Text = s }));
        }

        var icon = await ImageLoader.LoadIconAsync(SearchResultItemInfo.IconUri);
        if (icon != null)
        {
            IconBox.Source = icon;
            IconFont.IsVisible = false;
        }

        var description = await new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey)
            .GetModDescriptionAsync(SearchResultItemInfo.Id);
    }

    private void GetResourceBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        GlobalModel.MainWindow.OpenDraw(
            new DrawDownloadCurseForgeResourceContent(
                JsonSerializer.Deserialize<CurseForgeResponse.ModData>(SearchResultItemInfo.JsonData)),
            $"下载资源 {SearchResultItemInfo.Name}");
    }

    private void CopyName_OnClick(object? sender, RoutedEventArgs e)
    {
        CopyService.SetClipboard($"你的好友向你推荐了一个资源【{SearchResultItemInfo.Name}】\n" +
                                 $"地址：{SearchResultItemInfo.SourceWebsite}\n" +
                                 $"前往 [BedrockBoot]，Ctrl+V 即可获取该资源", CopyType.Resource,
            JsonSerializer.Deserialize<CurseForgeResponse.ModData>(SearchResultItemInfo.JsonData).Id);

        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = "剪切板",
            Message = "分享内容已复制，在 BedrockBoot 内按下 Ctrl+V 即可查看该资源"
        });
    }
}