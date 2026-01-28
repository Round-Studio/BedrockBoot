using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.DrawContent;
using BedrockLauncher.Core;
using BedrockLauncher.Core.VersionJsons;

namespace BedrockBoot.Views.Pages.DownloadPage.SearchSubPage;

public partial class SearchDefault : UserControl
{
    public SearchDefault()
    {
        InitializeComponent();

        DownloadSearch.SearchDetailed = null;

        Task.Run(() =>
        {
            var rele = VersionHelper.GetVersions().Find(x => x.Type == MinecraftGameTypeVersion.Release);
            var prev = VersionHelper.GetVersions().Find(x => x.Type == MinecraftGameTypeVersion.Preview);

            Dispatcher.UIThread.Invoke(() =>
            {
                RecommendationPanel.IsVisible = true;
                LoadRing.IsVisible = false;

                ReleaseVersion.Text = rele.ID;
                PreviewVersion.Text = prev.ID;

                ReleaseDescription.Text = $"{rele.Date}，{rele.BuildType}";
                PreviewDescription.Text = $"{prev.Date}，{prev.BuildType}";
            });
        });
    }
    

    private void GameListBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DownloadSearch.SearchFrame.NavigateTo(new SearchDetailed(new SearchInfo()
        {
            Type = SearchResourceType.Minecraft
        }));
    }

    private void ReleaseBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var i = VersionHelper.GetVersions().Find(x => x.Type == MinecraftGameTypeVersion.Release);
        GlobalModel.MainWindow.OpenDraw(new DrawDownloadGameContent(i),$"下载游戏 {i.ID}");
    }

    private void PreviewBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var i = VersionHelper.GetVersions().Find(x => x.Type == MinecraftGameTypeVersion.Preview);
        GlobalModel.MainWindow.OpenDraw(new DrawDownloadGameContent(i),$"下载游戏 {i.ID}");
    }
}