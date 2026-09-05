using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Entry.Pack.Market;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Plugin.Market;
using BedrockBoot.Models.Pack.Search;
using BedrockBoot.Views.DrawContent;

namespace BedrockBoot.Models.Pack.Search
{
    public class PluginPackSearch : ISearch
    {
        private readonly MarketClient _marketClient;
        private bool _enableFuzzySearch;

        public SearchResourceType SearchType => SearchResourceType.PluginPack;
        public bool SupportsPagination => true;

        public PluginPackSearch()
        {
            _marketClient = new MarketClient();
            _enableFuzzySearch = Core.Global.GlobalModel.Config.Data.IsEnableFuzzySearch;
        }

        public void SetExtraParameter(object parameter)
        {
        }

        public object GetExtraParameter() => null;

        public Task<List<SearchResultItemInfo>> SearchAsync(string keyword)
        {
            return SearchAsync(keyword, 1, 50);
        }

        public async Task<List<SearchResultItemInfo>> SearchAsync(string keyword, int page, int pageSize)
        {
            var result = await _marketClient.GetPluginsAsync();
            var filteredResult = result
                .Where(plugin => IsPluginMatch(plugin, keyword))
                .ToList();

            var currentIndex = (page - 1) * pageSize;
            var currentPagePlugins = filteredResult
                .Skip(currentIndex)
                .Take(pageSize)
                .ToList();

            var items = new List<SearchResultItemInfo>();
            currentPagePlugins.ForEach(plugin =>
            {
                plugin.IconUrl = $"{SourceList.MarketApiHost}{plugin.IconUrl}";
                var item = new SearchResultItemInfo
                {
                    Name = plugin.PluginName,
                    Id = 0,
                    Description = plugin.Description,
                    Authors = new List<string>() { plugin.Username },
                    DownloadCount = 0,
                    IconUri = plugin.IconUrl,
                    Labels = plugin.Labels,
                    Images = null,
                    SourceWebsite = plugin.RepositoryUrl,
                    JsonData = JsonSerializer.Serialize(plugin)
                };
                item.OnClick = s =>
                {
                    Console.WriteLine($@"View Plugin: {s}");
                    GlobalModel.MainWindow.OpenDraw(new DrawDownloadPluginContent(plugin),
                        $"插件详细信息：{plugin.PluginName}");
                };
                items.Add(item);
            });

            return items;
        }

        private bool IsPluginMatch(MarketResponse.PluginInfo plugin, string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return true;

            var pluginName = plugin.PluginName ?? string.Empty;
            var pluginDescription = plugin.Description ?? string.Empty;
            var repositoryUrl = plugin.RepositoryUrl ?? string.Empty;
            var username = plugin.Username ?? string.Empty;
            var combinedText = $"{pluginName} {pluginDescription} {repositoryUrl} {username}".ToLower();

            if (!_enableFuzzySearch)
            {
                return combinedText.Contains(keyword.ToLower());
            }

            var keywordLower = keyword.ToLower();

            if (combinedText.Contains(keywordLower)) return true;

            if (FuzzyMatchHelper.IsFuzzyMatch(pluginName.ToLower(), keywordLower, 0.7)) return true;
            if (FuzzyMatchHelper.IsFuzzyMatch(pluginDescription.ToLower(), keywordLower, 0.6)) return true;
            if (FuzzyMatchHelper.IsFuzzyMatch(repositoryUrl.ToLower(), keywordLower, 0.7)) return true;
            if (FuzzyMatchHelper.IsFuzzyMatch(username.ToLower(), keywordLower, 0.7)) return true;

            if (plugin.Labels != null)
            {
                foreach (var label in plugin.Labels)
                {
                    var labelName = label?.ToLower() ?? string.Empty;
                    if (FuzzyMatchHelper.IsFuzzyMatch(labelName, keywordLower, 0.7)) return true;
                    if (labelName.Contains(keywordLower)) return true;
                }
            }

            return false;
        }
    }
}