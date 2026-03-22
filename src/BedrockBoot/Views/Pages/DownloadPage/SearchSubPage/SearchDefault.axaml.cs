using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Helpers;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;
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
        
        FetchLatestVersions();
        LoadFeaturedResourcesAsync();
    }

    private async Task LoadFeaturedResourcesAsync()
    {
        // 1. 开始加载，显示 Ring，隐藏旧内容
        ResourceLoadRing.IsVisible = true;
        RecommendationGrid.IsVisible = false; // 假设你的容器叫这个名字

        try
        {
            // 2. 在后台线程执行网络请求
            var client = new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey);
        
            // 使用 Task.Run 确保 API 初始化的耗时操作不在 UI 线程
            var featuredData = await Task.Run(async () => 
            {
                return await client.GetFeaturedModsAsync();
            });

            // 3. 回到 UI 线程更新控件
            // 注意：await 之后会自动回到 UI 上下文，但为了保险或复杂逻辑可以用 Post
            Dispatcher.UIThread.Post(() =>
            {
                if (featuredData?.Data.Popular != null && featuredData.Data.Popular.Count > 0)
                {
                    var popularList = featuredData.Data.Popular;

                    // --- 1. 大按钮 (Index 0) ---
                    var mod0 = popularList[0];
                    BigResourceButton.ResourceName = mod0.Name;
                    BigResourceButton.Description = mod0.Summary;
                    BigResourceButton.Author = $"By {mod0.Authors.FirstOrDefault()?.Name}";
                    BigResourceButton.DownloadCount = mod0.DownloadCount.ToString();
                    BigResourceButton.IconUrl = mod0.Logo?.ThumbnailUrl;
                    BigResourceButton.Labels = mod0.Categories.Select(x => x.Name).ToList();
                    BigResourceButton.UpdateDate = DateHelper.GetRelativeTime(mod0.DateReleased);

                    {
                        var authorNames = mod0.Authors.Select(a => a.Name).ToList();

                        var categories = mod0.Categories.Select(a => a.Name).ToList();

                        var item = new SearchResultItemInfo
                        {
                            Name = mod0.Name,
                            Id = mod0.Id,
                            Description = $"{mod0.Summary}",
                            DateUpdated = mod0.DateReleased,
                            DateCreated = mod0.DateCreated,
                            Authors = authorNames,
                            DownloadCount = (uint)mod0.DownloadCount,
                            IconUri = mod0.Logo.Url,
                            Labels = categories,
                            Images = mod0.Screenshots.Select(a => a.Url).ToList(),
                            SourceWebsite = mod0.Links.WebsiteUrl,
                            JsonData = JsonSerializer.Serialize(mod0)
                        };
                        
                        BigResourceButton.Click +=
                            (s, e) => DownloadRoot.Instance.NavigateTo(new ResultRoot(item));
                    }

                    // --- 2. 小按钮 1 (Index 1) ---
                    if (popularList.Count > 1)
                    {
                        var mod1 = popularList[1];
                        SmallResourceButton1.ResourceName = mod1.Name;
                        SmallResourceButton1.Author = $"By {mod1.Authors.FirstOrDefault()?.Name}";
                        SmallResourceButton1.IconUrl = mod1.Logo?.ThumbnailUrl;
                        // 如果小按钮不需要展示这么多信息，可以省略部分赋值
                        {
                            var authorNames = mod1.Authors.Select(a => a.Name).ToList();

                            var categories = mod1.Categories.Select(a => a.Name).ToList();

                            var item = new SearchResultItemInfo
                            {
                                Name = mod1.Name,
                                Id = mod1.Id,
                                Description = $"{mod1.Summary}",
                                DateUpdated = mod1.DateReleased,
                                DateCreated = mod1.DateCreated,
                                Authors = authorNames,
                                DownloadCount = (uint)mod1.DownloadCount,
                                IconUri = mod1.Logo.Url,
                                Labels = categories,
                                Images = mod1.Screenshots.Select(a => a.Url).ToList(),
                                SourceWebsite = mod1.Links.WebsiteUrl,
                                JsonData = JsonSerializer.Serialize(mod1)
                            };

                            SmallResourceButton1.Click +=
                                (s, e) => DownloadRoot.Instance.NavigateTo(new ResultRoot(item));
                        }
                    }

                    // --- 3. 小按钮 2 (Index 2) ---
                    if (popularList.Count > 2)
                    {
                        var mod2 = popularList[2];
                        SmallResourceButton2.ResourceName = mod2.Name;
                        SmallResourceButton2.Author = $"By {mod2.Authors.FirstOrDefault()?.Name}";
                        SmallResourceButton2.IconUrl = mod2.Logo?.ThumbnailUrl;
                        
                        {
                            var authorNames = mod2.Authors.Select(a => a.Name).ToList();

                            var categories = mod2.Categories.Select(a => a.Name).ToList();

                            var item = new SearchResultItemInfo
                            {
                                Name = mod2.Name,
                                Id = mod2.Id,
                                Description = $"{mod2.Summary}",
                                DateUpdated = mod2.DateReleased,
                                DateCreated = mod2.DateCreated,
                                Authors = authorNames,
                                DownloadCount = (uint)mod2.DownloadCount,
                                IconUri = mod2.Logo.Url,
                                Labels = categories,
                                Images = mod2.Screenshots.Select(a => a.Url).ToList(),
                                SourceWebsite = mod2.Links.WebsiteUrl,
                                JsonData = JsonSerializer.Serialize(mod2)
                            };
                        
                            SmallResourceButton2.Click +=
                                (s, e) => DownloadRoot.Instance.NavigateTo(new ResultRoot(item));
                        }
                    }
                
                    RecommendationGrid.IsVisible = true;
                }
                ResourceLoadRing.IsVisible = false;
            });
        }
        catch (Exception ex)
        {
            // 4. 错误处理
            System.Diagnostics.Debug.WriteLine($"API 请求失败: {ex.Message}");
            Dispatcher.UIThread.Post(() => ResourceLoadRing.IsVisible = false);
        }
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

    private void SearchRes_OnClick(object? sender, RoutedEventArgs e)
    {
        DownloadSearch.SearchFrame.NavigateTo(new SearchDetailed(new SearchInfo
        {
            Type = SearchResourceType.ResourcePack
        }));
    }
}