using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Helpers;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.Control.Widgets;
using BedrockBoot.Views.DrawContent;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;

public partial class ResultRoot : UserControl
{
    public SearchResultItemInfo SearchResultItemInfo { get; set; }

    public ResultRoot()
    {
        InitializeComponent();
    }

    public ResultRoot(SearchResultItemInfo info) : this()
    {
        SearchResultItemInfo = info;
        Update();
    }

    public async Task Update()
    {
        ResourceName.Text = SearchResultItemInfo.Name;
        AuthorText.Text = $"By {string.Join(", ",SearchResultItemInfo.Authors)}";
        DescriptionText.Text = SearchResultItemInfo.Description;
        DownloadCountText.Text = SearchResultItemInfo.DownloadCount.ToString();
        UpdataDateText.Text = DateHelper.GetRelativeTime(SearchResultItemInfo.DateUpdated);
        OpenSourceWebsite.IsVisible = !string.IsNullOrEmpty(SearchResultItemInfo.SourceWebsite);
        if (SearchResultItemInfo.Images != null &&
            SearchResultItemInfo.Images.Count > 0)
        {
            PreviewCard.IsVisible = true;
            SearchResultItemInfo.Images.ForEach(image => PreviewList.Children.Add(new LocalImageRenderWidget(image)
            {
                Width = 320
            }));
        }
        LabelsBox.Children.Clear();

        if (SearchResultItemInfo.Labels.Count > 0)
        {
            LabelsBox.IsVisible = true;
            SearchResultItemInfo.Labels.ForEach(s => LabelsBox.Children.Add(new LabelBox() { Text = s }));
        }
        
        var icon = await ImageLoader.LoadIconAsync(SearchResultItemInfo.IconUri);
        if (icon != null)
        {
            IconBox.Source = icon;
            IconFont.IsVisible = false;
        }
    }

    private void OpenSourceWebsite_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = SearchResultItemInfo.SourceWebsite,
            UseShellExecute = true
        });
    }

    private void GetResourceBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        GlobalModel.MainWindow.OpenDraw(
            new DrawDownloadCurseForgeResourceContent(
                JsonSerializer.Deserialize<CurseForgeResponse.ModData>(SearchResultItemInfo.JsonData)),
            $"下载资源 {SearchResultItemInfo.Name}");
    }
}