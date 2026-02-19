using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.DrawContent;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.DownloadPage.SearchSubPage;

public partial class SearchDefault : UserControl
{
    private static I18nManager i18n => I18nManager.Instance;

    public SearchDefault()
    {
        InitializeComponent();

        // 重置详细搜索状态
        DownloadSearch.SearchDetailed = null;

        // 异步获取最新推荐版本
        FetchLatestVersions();
    }

    private void FetchLatestVersions()
    {
        Task.Run(() =>
        {
            try
            {
                var versions = VersionHelper.GetVersions();
                var release = versions.Find(x => x.Type == MinecraftGameTypeVersion.Release);
                var preview = versions.Find(x => x.Type == MinecraftGameTypeVersion.Preview);

                Dispatcher.UIThread.Invoke(() =>
                {
                    if (release != null)
                    {
                        ReleaseVersion.Text = release.ID;
                        ReleaseDescription.Text = $"{release.Date}, {release.BuildType}";
                    }

                    if (preview != null)
                    {
                        PreviewVersion.Text = preview.ID;
                        PreviewDescription.Text = $"{preview.Date}, {preview.BuildType}";
                    }

                    RecommendationPanel.IsVisible = true;
                    LoadRing.IsVisible = false;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to fetch default versions: {ex.Message}");
                Dispatcher.UIThread.Invoke(() =>
                {
                    RecommendationPanel.IsVisible = false;
                    LoadRing.IsVisible = false;
                });
            }
        });
    }

    /// <summary>
    /// 跳转到游戏详细列表
    /// </summary>
    private void GameListBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DownloadSearch.SearchFrame.NavigateTo(new SearchDetailed(new SearchInfo
        {
            Type = SearchResourceType.Minecraft
        }));
    }

    private void ReleaseBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenDownloadDraw(MinecraftGameTypeVersion.Release);
    }

    private void PreviewBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenDownloadDraw(MinecraftGameTypeVersion.Preview);
    }

    /// <summary>
    /// 统一打开下载侧边栏
    /// </summary>
    private void OpenDownloadDraw(MinecraftGameTypeVersion type)
    {
        var version = VersionHelper.GetVersions().Find(x => x.Type == type);
        if (version == null) return;

        var title = $"{i18n["Download.Action.DownloadGame"]} {version.ID}";
        GlobalModel.MainWindow.OpenDraw(new DrawDownloadGameContent(version), title);
    }
}