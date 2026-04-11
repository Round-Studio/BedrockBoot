using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Helpers;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Views.Control.Widgets;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.DownloadPage.SearchSubPage;

public partial class SearchDefault : UserControl
{
    private static I18nManager i18n => I18nManager.Instance;

    private bool _versionLoadSuccess = false;
    private bool _resourceLoadSuccess = false;
    private int _completedTasks = 0;

    public SearchDefault()
    {
        InitializeComponent();
        DownloadSearch.SearchDetailed = null;
        
        _ = FetchLatestVersions();
        _ = LoadFeaturedResourcesAsync();
    }

    private void CheckNetworkStatus()
    {
        _completedTasks++;
        if (_completedTasks >= 2)
        {
            Dispatcher.UIThread.Invoke(() => {
                // 如果全部失败，显示底部的网络错误通知卡片
                NetworkErrorNotice.IsVisible = !_versionLoadSuccess && !_resourceLoadSuccess;
            });
        }
    }

    // --- 热门资源加载 ---
    private async Task LoadFeaturedResourcesAsync()
    {
        try
        {
            var client = new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey);
            var featuredData = await Task.Run(async () => await client.GetFeaturedModsAsync());

            if (featuredData?.Data?.Popular != null && featuredData.Data.Popular.Count > 0)
            {
                _resourceLoadSuccess = true;
                var popularList = featuredData.Data.Popular;

                Dispatcher.UIThread.Post(() => {
                    UpdateBigButton(popularList[0]);
                    if (popularList.Count > 1) UpdateSmallButton(SmallResourceBtn1, popularList[1]);
                    if (popularList.Count > 2) UpdateSmallButton(SmallResourceBtn2, popularList[2]);

                    RecommendationGrid.IsVisible = true;
                    ResourceLoadRing.IsVisible = false;
                });
                return;
            }
            throw new Exception("Data empty");
        }
        catch (Exception ex)
        {
            _resourceLoadSuccess = false;
            Dispatcher.UIThread.Post(() => {
                // 加载失败，直接隐藏整个资源板块的 BorderCard
                ResourceCard.IsVisible = false; 
            });
        }
        finally
        {
            CheckNetworkStatus();
        }
    }

    // --- 游戏版本加载 ---
    private async Task FetchLatestVersions()
    {
        try
        {
            var versions = await Task.Run(() => VersionHelper.GetVersions());
            var release = versions.Find(x => x.Type == BedrockLauncher.Core.MinecraftGameTypeVersion.Release);
            var preview = versions.Find(x => x.Type == BedrockLauncher.Core.MinecraftGameTypeVersion.Preview);

            if (release == null && preview == null) throw new Exception("No versions found");

            Dispatcher.UIThread.Invoke(() => {
                _versionLoadSuccess = true;
                if (release != null)
                {
                    ReleaseBtn.Version = release.ID;
                    ReleaseBtn.Description = $"{release.Date}, {release.BuildType}";
                }
                if (preview != null)
                {
                    PreviewBtn.Version = preview.ID;
                    PreviewBtn.Description = $"{preview.Date}, {preview.BuildType}";
                }
                RecommendationPanel.IsVisible = true;
                LoadRing.IsVisible = false;
            });
        }
        catch (Exception ex)
        {
            _versionLoadSuccess = false;
            Dispatcher.UIThread.Invoke(() => {
                // 加载失败，直接隐藏整个版本板块的 BorderCard
                VersionCard.IsVisible = false;
            });
        }
        finally
        {
            CheckNetworkStatus();
        }
    }

    // --- 数据填充辅助方法 ---
    private void UpdateBigButton(CurseForgeResponse.ModData mod)
    {
        BigResourceBtn.ResourceName = mod.Name;
        BigResourceBtn.Description = mod.Summary;
        BigResourceBtn.Author = $"By {mod.Authors.FirstOrDefault()?.Name}";
        BigResourceBtn.DownloadCount = mod.DownloadCount.ToString();
        BigResourceBtn.IconUrl = mod.Logo?.ThumbnailUrl;
        BigResourceBtn.UpdateDate = DateHelper.GetRelativeTime(mod.DateReleased);
        BigResourceBtn.Labels = mod.Categories.Select(x => x.Name).ToList();
        BigResourceBtn.Click += (s, e) => NavigateToResult(mod);
    }

    private void UpdateSmallButton(SmallResourceButton srb, CurseForgeResponse.ModData mod)
    {
        srb.ResourceName = mod.Name;
        srb.Author = $"By {mod.Authors.FirstOrDefault()?.Name}";
        srb.IconUrl = mod.Logo?.ThumbnailUrl;
        srb.Click += (s, e) => NavigateToResult(mod);
    }

    private void NavigateToResult(CurseForgeResponse.ModData mod)
    {
        var item = new SearchResultItemInfo {
            Name = mod.Name, Id = mod.Id, Description = mod.Summary,
            DateUpdated = mod.DateReleased, Authors = mod.Authors.Select(a => a.Name).ToList(),
            DownloadCount = (uint)mod.DownloadCount, IconUri = mod.Logo?.Url,
            Labels = mod.Categories.Select(c => c.Name).ToList(),
            SourceWebsite = mod.Links?.WebsiteUrl, JsonData = JsonSerializer.Serialize(mod)
        };
        DownloadRoot.Instance.NavigateTo(new ResultRoot(item));
    }

    // --- 基础导航事件 ---
    private void GameListBtn_OnClick(object? sender, RoutedEventArgs e) => NavigateSearch(SearchResourceType.Minecraft);
    private void SearchRes_OnClick(object? sender, RoutedEventArgs e) => NavigateSearch(SearchResourceType.ResourcePack);
    private void NavigateSearch(SearchResourceType type) => DownloadSearch.SearchFrame.NavigateTo(new SearchDetailed(new SearchInfo { Type = type }));
    private void ReleaseBtn_OnClick(object? sender, RoutedEventArgs e) => OpenDownloadDraw(MinecraftGameTypeVersion.Release);
    private void PreviewBtn_OnClick(object? sender, RoutedEventArgs e) => OpenDownloadDraw(MinecraftGameTypeVersion.Preview);

    private void OpenDownloadDraw(MinecraftGameTypeVersion type)
    {
        var version = VersionHelper.GetVersions().Find(x => x.Type == type);
        if (version != null) GlobalModel.MainWindow.OpenDraw(new DrawDownloadGameContent(version), $"{i18n["Download.Action.DownloadGame"]} {version.ID}");
    }
}