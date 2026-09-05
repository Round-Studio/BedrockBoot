using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.DrawContent;
using BedrockLauncher.Core;

namespace BedrockBoot.Models.Pack.Search;

public class MinecraftSearch : ISearch
{
    private int _selectedGameTypeIndex;

    public SearchResourceType SearchType => SearchResourceType.Minecraft;
    public bool SupportsPagination => true;

    public void SetExtraParameter(object parameter)
    {
        if (parameter is int index)
            _selectedGameTypeIndex = index;
    }

    public object GetExtraParameter() => _selectedGameTypeIndex;

    public Task<List<SearchResultItemInfo>> SearchAsync(string keyword)
    {
        return SearchAsync(keyword, 1, 50);
    }

    public Task<List<SearchResultItemInfo>> SearchAsync(string keyword, int page, int pageSize)
    {
        return Task.Run(() =>
        {
            var allVersions = VersionHelper.GetVersions()
                .Where(x => x.Type == (MinecraftGameTypeVersion)_selectedGameTypeIndex)
#if LINUX
                    .Where(x => x.BuildType == MinecraftBuildTypeVersion.GDK)
#endif
                .ToList();

            var filteredVersions = allVersions
                .Where(version => IsMinecraftMatch(version, keyword))
                .ToList();

            var inputParts = keyword.Split('.');
            if (inputParts.Length >= 2 && inputParts[0] == "1" && int.TryParse(inputParts[1], out int second) &&
                second >= 26)
            {
                inputParts = inputParts.Skip(1).ToArray();
            }

            int MatchCount(string version)
            {
                var parts = version.Split('.');
                int count = 0;
                int len = Math.Min(parts.Length, inputParts.Length);

                for (int i = 0; i < len; i++)
                {
                    if (parts[i] == inputParts[i])
                        count++;
                    else
                        break;
                }

                return count;
            }

            filteredVersions = filteredVersions
                .OrderByDescending(x => MatchCount(x.Key))
                .ToList();

            var currentIndex = (page - 1) * pageSize;
            var currentPageVersions = filteredVersions
                .Skip(currentIndex)
                .Take(pageSize)
                .ToList();

            var items = new List<SearchResultItemInfo>();
            currentPageVersions.ForEach(i =>
            {
                items.Add(new SearchResultItemInfo
                {
                    Name = i.Key,
                    Description = $"{i.ID}, {i.BuildType}, {i.Date}",
                    IconUri = i.Type == MinecraftGameTypeVersion.Release
                        ? "avares://BedrockBoot/Assets/Icon/Logo/Grass.png"
                        : "avares://BedrockBoot/Assets/Icon/Logo/GrassScript.png",
                    OnClick = s =>
                    {
                        GlobalModel.MainWindow.OpenDraw(new DrawDownloadGameContent(i),
                            $"{I18nManager.Instance["Download.Action.DownloadGame"]} {i.Key}");
                    }
                });
            });

            return items;
        });
    }

    private bool IsMinecraftMatch(dynamic version, string keyword)
    {
        if (string.IsNullOrEmpty(keyword)) return true;

        keyword = keyword.Trim();

        var versionId = version.ID ?? string.Empty;
        var versionKey = version.Key ?? string.Empty;
        var buildType = version.BuildType?.ToString() ?? string.Empty;

        if (keyword.ToLower() == "uwp") return buildType == "UWP";
        else if (keyword.ToLower() == "gdk") return buildType == "GDK";

        if (keyword.StartsWith("1."))
        {
            if (versionId.StartsWith(keyword) || versionKey.StartsWith(keyword)) return true;
        }
        else if (versionId.Contains(keyword) || versionKey.Contains(keyword)) return true;

        var parts = keyword.Split('.');
        if (parts.Length == 4)
        {
            if (parts[0] == "1")
            {
                return ("." + versionKey + ".").Contains($".{parts[1]}.{parts[2]}.") && !versionKey.StartsWith("0.");
            }
        }
        else if (parts.Length == 3)
        {
            if (parts[0] != "1")
            {
                return ("." + versionKey + ".").Contains($".{parts[0]}.{parts[1]}.");
            }
        }

        return false;
    }
}