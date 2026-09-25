/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

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
using BedrockBoot.Models.Pack.Game.Instance;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Models.Pack.Plugin.Market;
using BedrockBoot.Models.Pack.Search;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.Control.Widgets;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Entity;

namespace BedrockBoot.Views.Pages.DownloadPage.SearchSubPage;

public partial class SearchDefault : UserControl
{
    private int _completedTasks;
    private bool _resourceLoadSuccess;

    private bool _versionLoadSuccess;
    private ImageLoader _imageLoader = new ImageLoader();

    public SearchDefault()
    {
        InitializeComponent();
        DownloadSearch.SearchDetailed = null;

        LoadSearchHistory();
        _ = FetchLatestVersions();
        _ = LoadFeaturedResourcesAsync();
        _ = LoadPluginAsync();
        _ = LoadLeviLaminaModsAsync();
        _ = LoadDllModsAsync();
    }

    private static I18nManager i18n => I18nManager.Instance;

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _imageLoader.Dispose();
    }

    private void LoadSearchHistory()
    {
        NoneBox.IsVisible = false;
        HistoryList.IsVisible = false;
        HistoryListScrollViewer.IsVisible = false;
        CleanBtn.IsVisible = false;
        var searchHis = new ConfigEntity<List<SearchInfo>>(PathsList.HistoryPath, false);
        if (searchHis?.Data == null) searchHis.Data = new List<SearchInfo>();
        searchHis.Save();
        if (searchHis.Data.Count <= 0)
        {
            NoneBox.IsVisible = true;
            return;
        }

        searchHis.Data.Reverse();
        searchHis.Data.ForEach(his => HistoryList.Items.Add(new SearchHistoryItem(his)
        {
            SearchAction = info => { DownloadSearch.SearchFrame.NavigateTo(new SearchDetailed(info)); }
        }));
        HistoryList.IsVisible = true;
        HistoryListScrollViewer.IsVisible = true;
        CleanBtn.IsVisible = true;
    }

    private void CheckNetworkStatus()
    {
        _completedTasks++;
        if (_completedTasks >= 2)
            Dispatcher.UIThread.Invoke(() =>
            {
                // 如果全部失败，显示底部的网络错误通知卡片
                NetworkErrorNotice.IsVisible = !_versionLoadSuccess && !_resourceLoadSuccess;
            });
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

                Dispatcher.UIThread.Post(() =>
                {
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
            Dispatcher.UIThread.Post(() =>
            {
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
            var versions = await (new MinecraftSearch()).GetRecommendAsync();
            var release = versions[0];
            var preview = versions[1];

            if (release == null && preview == null) throw new Exception("No versions found");

            Dispatcher.UIThread.Invoke(() =>
            {
                _versionLoadSuccess = true;
                if (release != null)
                {
                    ReleaseBtn.Version = release.Name;
                    ReleaseBtn.Description = $"{release.Id}, {release.DateUpdated}, Release";
                }

                if (preview != null)
                {
                    PreviewBtn.Version = preview.Name;
                    PreviewBtn.Description = $"{preview.Id}, {preview.DateUpdated}, Preview";
                }

                RecommendationPanel.IsVisible = true;
                LoadRing.IsVisible = false;
            });
        }
        catch (Exception ex)
        {
            _versionLoadSuccess = false;
            Dispatcher.UIThread.Invoke(() =>
            {
                // 加载失败，直接隐藏整个版本板块的 BorderCard
                VersionCard.IsVisible = false;
            });
        }
        finally
        {
            CheckNetworkStatus();
        }
    }

    private async Task LoadLeviLaminaModsAsync()
    {
        try
        {
            var client = await (new LeviLaminaModSearch()).GetRecommendAsync();
            var plugin1 = client[0];
            var plugin2 = client[1];

            LLModView1.PluginName = plugin1.Name;
            LLModView1.Description = plugin1.Description;
            LLModView2.PluginName = plugin2.Name;
            LLModView2.Description = plugin2.Description;

            LLModView1.Click += (s, e) =>
                DownloadRoot.Instance.NavigateTo(new ResultRoot(plugin1));
            LLModView2.Click += (s, e) =>
                DownloadRoot.Instance.NavigateTo(new ResultRoot(plugin2));

            LLModLoadRing.IsVisible = false;
            LeviLaminaItem.IsVisible = true;

            var tasks = new[]
            {
                _imageLoader.LoadIconAsync(plugin1.IconUri),
                _imageLoader.LoadIconAsync(plugin2.IconUri)
            };

            await Task.WhenAll(tasks);
            LLModView1.Icon = await tasks[0];
            LLModView2.Icon = await tasks[1];
        }
        catch
        {
            LeviLaminaModCard.IsVisible = false;
        }
    }

    private async Task LoadDllModsAsync()
    {
        try
        {
            var client = await (new DllModsSearch()).GetRecommendAsync();
            var plugin1 = client[0];
            var plugin2 = client[1];

            DllView1.PluginName = plugin1.Name;
            DllView1.Description = plugin1.Description;
            DllView2.PluginName = plugin2.Name;
            DllView2.Description = plugin2.Description;

            DllView1.Click += (s, e) =>
                DownloadRoot.Instance.NavigateTo(new ResultRoot(plugin1));
            DllView2.Click += (s, e) =>
                DownloadRoot.Instance.NavigateTo(new ResultRoot(plugin2));

            DllRing.IsVisible = false;
            DllItem.IsVisible = true;

            var tasks = new[]
            {
                _imageLoader.LoadIconAsync(plugin1.IconUri),
                _imageLoader.LoadIconAsync(plugin2.IconUri)
            };

            await Task.WhenAll(tasks);
            DllView1.Icon = await tasks[0];
            DllView2.Icon = await tasks[1];
        }
        catch
        {
            DllModCard.IsVisible = false;
        }
    }

    private async Task LoadPluginAsync()
    {
        try
        {
            var client = await (new PluginPackSearch()).GetRecommendAsync();
            var plugin1 = client[0];
            var plugin2 = client[1];

            PluginView1.PluginName = plugin1.Name;
            PluginView1.Description = plugin1.Description;
            PluginView2.PluginName = plugin2.Name;
            PluginView2.Description = plugin2.Description;

            PluginView1.Click += (s, e) =>
                DownloadRoot.Instance.NavigateTo(new ResultRoot(plugin1));
            PluginView2.Click += (s, e) =>
                DownloadRoot.Instance.NavigateTo(new ResultRoot(plugin2));

            PluginLoadRing.IsVisible = false;
            PluginItem.IsVisible = true;

            var tasks = new[]
            {
                _imageLoader.LoadIconAsync(plugin1.IconUri),
                _imageLoader.LoadIconAsync(plugin2.IconUri)
            };

            await Task.WhenAll(tasks);
            PluginView1.Icon = await tasks[0];
            PluginView2.Icon = await tasks[1];
        }
        catch
        {
            PluginCard.IsVisible = false;
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
        var item = new SearchResultItemInfo
        {
            Name = mod.Name, Id = mod.Id.ToString(), Description = mod.Summary,
            DateUpdated = mod.DateReleased, Authors = mod.Authors.Select(a => a.Name).ToList(),
            DownloadCount = (uint)mod.DownloadCount, IconUri = mod.Logo?.Url,
            Labels = mod.Categories.Select(c => c.Name).ToList(),
            SourceWebsite = mod.Links?.WebsiteUrl, JsonData = JsonSerializer.Serialize(mod),
            ResourceType = SearchResourceType.ResourcePack
        };
        DownloadRoot.Instance.NavigateTo(new ResultRoot(item));
    }

    // --- 基础导航事件 ---
    private void GameListBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        NavigateSearch(SearchResourceType.Minecraft);
    }

    private void SearchRes_OnClick(object? sender, RoutedEventArgs e)
    {
        NavigateSearch(SearchResourceType.ResourcePack);
    }

    private void NavigateSearch(SearchResourceType type)
    {
        DownloadSearch.SearchFrame.NavigateTo(new SearchDetailed(new SearchInfo { Type = type }));
    }

    private void ReleaseBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenDownloadDraw(MinecraftGameTypeVersion.Release);
    }

    private void PreviewBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenDownloadDraw(MinecraftGameTypeVersion.Preview);
    }

    private void OpenDownloadDraw(MinecraftGameTypeVersion type)
    {
        var version = VersionHelper.GetVersions().Find(x => x.Type == type);
        if (version != null)
        {
            var installer = new InstanceInstaller(version);
            installer.Install();
            /*GlobalModel.MainWindow.OpenDraw(new DrawDownloadGameContent(version),
                $"{i18n["Download.Action.DownloadGame"]} {version.Key}");*/
        }
    }

    private void CleanBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Content = i18n["Download.Search.History.Clear.Confirm.Content"],
            Title = i18n["Download.Search.History.Clear.Confirm.Title"],
            CloseButtonText = i18n["Shared.Action.Confirm"],
            PrimaryButtonText = i18n["Shared.Action.Cancel"],
            CloseAction = () =>
            {
                var searchHis = new ConfigEntity<List<SearchInfo>>(PathsList.HistoryPath);
                searchHis.Data.Clear();
                searchHis.Save();

                LoadSearchHistory();
            }
        });
    }

    private void SearchPlugin_OnClick(object? sender, RoutedEventArgs e)
    {
        NavigateSearch(SearchResourceType.PluginPack);
    }

    private void SearchLeviLaminaMod_OnClick(object? sender, RoutedEventArgs e)
    {
        NavigateSearch(SearchResourceType.LeviLaminaMods);
    }

    private void SearchDllMod_OnClick(object? sender, RoutedEventArgs e)
    {
        NavigateSearch(SearchResourceType.DllMods);
    }
}