using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Search;

namespace BedrockBoot.Models.Pack.Search
{
    public class DllModsSearch : ISearch
    {
        private static string IndexUrl => SourceList.DllModsApi;
        private static string UserAgent => $"BedrockBoot/{GlobalModel.BodyVersion}";
        private bool _enableFuzzySearch;
        private List<DllPackage> _cachedPackages;
        private DateTime _cacheTime;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        public SearchResourceType SearchType => SearchResourceType.DllMods;
        public bool SupportsPagination => true;

        public DllModsSearch()
        {
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
            var packages = await GetPackagesAsync();

            var filteredPackages = packages
                .Where(pkg => IsDllModMatch(pkg, keyword))
                .ToList();

            var currentIndex = (page - 1) * pageSize;
            var currentPagePackages = filteredPackages
                .Skip(currentIndex)
                .Take(pageSize)
                .ToList();

            var items = new List<SearchResultItemInfo>();
            currentPagePackages.ForEach(pkg =>
            {
                var item = new SearchResultItemInfo
                {
                    Name = pkg.Header.Name,
                    Id = 0,
                    Description = pkg.Header.Description ?? string.Empty,
                    Authors = new List<string>() { pkg.Header.Author },
                    DownloadCount = 0,
                    IconUri = "avares://BedrockBoot/Assets/Icon/Files/NoneIcon.png",
                    Labels = pkg.Header.Tags ?? new List<string>(),
                    Images = null,
                    SourceWebsite = pkg.Header.Reop,
                    JsonData = JsonSerializer.Serialize(pkg)
                };

                item.OnClick = s => { Console.WriteLine(s); };
                items.Add(item);
            });

            return items;
        }

        private async Task<List<DllPackage>> GetPackagesAsync()
        {
            if (_cachedPackages != null && DateTime.Now - _cacheTime < _cacheDuration)
                return _cachedPackages;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

            var response = await client.GetAsync(IndexUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DllIndexResponse>(json);

            _cachedPackages = result?.Packages ?? new List<DllPackage>();
            _cacheTime = DateTime.Now;

            return _cachedPackages;
        }

        private bool IsDllModMatch(DllPackage package, string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return true;

            var name = package.Header.Name ?? string.Empty;
            var description = package.Header.Description ?? string.Empty;
            var author = package.Header.Author ?? string.Empty;
            var tags = package.Header.Tags != null ? string.Join(" ", package.Header.Tags) : string.Empty;
            var id = package.Id ?? string.Empty;

            var combinedText = $"{name} {description} {author} {tags} {id}".ToLower();

            if (!_enableFuzzySearch)
            {
                return combinedText.Contains(keyword.ToLower());
            }

            var keywordLower = keyword.ToLower();

            if (combinedText.Contains(keywordLower)) return true;

            if (FuzzyMatchHelper.IsFuzzyMatch(name.ToLower(), keywordLower, 0.7)) return true;
            if (FuzzyMatchHelper.IsFuzzyMatch(description.ToLower(), keywordLower, 0.6)) return true;
            if (FuzzyMatchHelper.IsFuzzyMatch(author.ToLower(), keywordLower, 0.7)) return true;
            if (FuzzyMatchHelper.IsFuzzyMatch(id.ToLower(), keywordLower, 0.7)) return true;

            if (package.Header.Tags != null)
            {
                foreach (var tag in package.Header.Tags)
                {
                    var tagName = tag?.ToLower() ?? string.Empty;
                    if (FuzzyMatchHelper.IsFuzzyMatch(tagName, keywordLower, 0.7)) return true;
                    if (tagName.Contains(keywordLower)) return true;
                }
            }

            return false;
        }
    }

    public class DllIndexResponse
    {
        [JsonPropertyName("format_version")] public int FormatVersion { get; set; }
        [JsonPropertyName("total")] public int Total { get; set; }
        [JsonPropertyName("packages")] public List<DllPackage> Packages { get; set; }
    }

    public class DllPackage
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("format_version")] public int FormatVersion { get; set; }
        [JsonPropertyName("header")] public DllHeader Header { get; set; }
        [JsonPropertyName("files")] public Dictionary<string, ModFile> Files { get; set; }
    }

    public class DllHeader
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("reop")] public string Reop { get; set; }
        [JsonPropertyName("author")] public string Author { get; set; }
        [JsonPropertyName("tags")] public List<string> Tags { get; set; }
    }

    public class ModFile
    {
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
    }
}