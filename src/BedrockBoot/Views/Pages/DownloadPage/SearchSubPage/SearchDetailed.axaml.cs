using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.Control.Items;
using BedrockLauncher.Core;
using BedrockLauncher.Core.VersionJsons;
using Octokit;

namespace BedrockBoot.Views.Pages.DownloadPage.SearchSubPage;

public partial class SearchDetailed : ISetting
{
    public static SearchInfo SearchInfo { get; set; }

    public SearchDetailed()
    {
        InitializeComponent();

        DownloadSearch.SearchDetailed = this;
    }

    public SearchDetailed(SearchInfo info) : this()
    {
        OnSearch(info);
    }

    public void OnSearch(SearchInfo info)
    {
        IsEdit = false;

        MinecraftTypePanel.IsVisible = false;
        CurseForgeResTypePanel.IsVisible = false;

        if (info.Type != SearchResourceType.Unknow)
            ResourceTypeBox.SelectedIndex = (int)info.Type;
        else
            info.Type = (SearchResourceType)ResourceTypeBox.SelectedIndex;

        if (info.Type == SearchResourceType.Minecraft)
            MinecraftTypePanel.IsVisible = true;
        if (info.Type == SearchResourceType.ResourcePack)
            CurseForgeResTypePanel.IsVisible = true;

        info.Key = string.IsNullOrEmpty(info.Key) ? "" : info.Key;

        SearchInfo = info;

        SearchResourceList.Children.Clear();
        ListScrollViewer.IsVisible = false;
        LoadCard.IsVisible = true;
        var items = new List<SearchResultItemInfo>(); // 储存所有结果
        if (info.Type == SearchResourceType.Minecraft)
        {
            Task.Run(() =>
            {
                var lst = VersionHelper.GetVersions()
                    .Where(x => (x.ID.ToLower().Contains(info.Key) ||
                                 x.BuildType.ToString().ToLower().Contains(info.Key)) &&
                                x.Type == (MinecraftGameTypeVersion)GameType.SelectedIndex);

                lst.ToList().ForEach(i =>
                {
                    items.Add(new SearchResultItemInfo()
                    {
                        Name = i.ID,
                        Description = $"{i.BuildType}, {i.Date}",
                        IconUri = i.Type == MinecraftGameTypeVersion.Release
                            ? "avares://Round.Avalonia.Assets/Image/Icon/mc_grassblock_neo.png"
                            : "avares://Round.Avalonia.Assets/Image/Icon/mc_soilblock_neo.png"
                    });
                });
                Dispatcher.UIThread.Invoke(() =>
                {
                    AddItemsBatchAsync(items);
                    LoadCard.IsVisible = false;
                    ListScrollViewer.IsVisible = true;
                });
            });
        }

        IsEdit = true;
    }

    private async Task AddItemsBatchAsync(List<SearchResultItemInfo> items)
    {
        const int batchSize = 10; // 每批添加的项目数量
        var totalCount = items.Count;

        for (int i = 0; i < totalCount; i += batchSize)
        {
            var batch = items.Skip(i).Take(batchSize).ToList();

            // 在UI线程添加一批项目
            foreach (var x in batch)
            {
                SearchResourceList.Children.Add(new SearchItem(x));
            }

            // 短暂延迟，让UI有机会更新
            await Task.Delay(10);
        }
    }

    private void GameType_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            SearchInfo.Key = DownloadSearch.DownloadSearchView.SearchKey;
            SearchInfo.Type = (SearchResourceType)ResourceTypeBox.SelectedIndex;
            OnSearch(SearchInfo);
        }
    }

    private void ResourceTypeBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            SearchInfo.Key = DownloadSearch.DownloadSearchView.SearchKey;
            SearchInfo.Type = (SearchResourceType)ResourceTypeBox.SelectedIndex;
            OnSearch(SearchInfo);
        }
    }
}