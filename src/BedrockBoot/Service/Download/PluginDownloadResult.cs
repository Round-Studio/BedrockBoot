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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Entry.Info.Download;
using BedrockBoot.Base.Entry.Pack.Market;
using BedrockBoot.Interface.Download;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Plugin;
using BedrockBoot.Models.Pack.Plugin.Market;
using BedrockBoot.Plugin;
using BedrockBoot.Views.TaskItem.Plugin;
using Octokit;
using Round.SDK.Entry;

namespace BedrockBoot.Service.Download;

public class PluginDownloadResult : IDownloadResult
{
    private MarketResponse.PluginInfo? _pluginInfo;
    private (Repository repository, IReadOnlyList<Release> releases)? _fullInfo;
    private PackConfig? _packConfig;

    public PluginDownloadResult(SearchResultItemInfo searchResultItemInfo)
    {
        SearchInfo = searchResultItemInfo;
    }

    public SearchResultItemInfo SearchInfo { get; set; }
    public bool IsHasManyFiles { get; } = false;

    private async Task<MarketResponse.PluginInfo> GetPluginInfo()
    {
        if (_pluginInfo == null)
        {
            _pluginInfo = JsonSerializer.Deserialize<MarketResponse.PluginInfo>(SearchInfo.JsonData);
        }

        return _pluginInfo;
    }

    private async Task<(Repository repository, IReadOnlyList<Release> releases)> GetFullInfo()
    {
        if (_fullInfo == null)
        {
            var info = await GetPluginInfo();
            _fullInfo = await MarketClient.GetPluginRepositoryFullInfo(info);
        }

        return _fullInfo.Value;
    }

    private async Task<IReadOnlyList<Release>> GetReleases()
    {
        var (_, releases) = await GetFullInfo();
        return releases;
    }

    public async Task<List<Control>?> DescriptionControls()
    {
        var info = await GetPluginInfo();
        var html = await MarketClient.GetReadmeHtml(info.RepositoryOwner, info.RepositoryName);
        return HtmlToControlConverter.ConvertHtmlToControls(html);
    }

    public async Task<uint> GetDownloadCount()
    {
        var releases = await GetReleases();
        var totalDownloads = releases.Sum(r => r.Assets.Sum(a => a.DownloadCount));
        return (uint)totalDownloads;
    }

    public async Task<bool> IsInstalled()
    {
        var releases = await GetReleases();
        var latestRelease = releases.FirstOrDefault();

        if (latestRelease == null || latestRelease.Assets == null || !latestRelease.Assets.Any())
            return false;

        var installStatus = latestRelease.Assets.All(asset =>
        {
            var fileName = Path.GetFileName(asset.BrowserDownloadUrl);
            var installedPath = PluginLoader.FindInstalledPackageFile(fileName);

            if (File.Exists(installedPath))
            {
                _packConfig = PluginHelper.ReadPackConfig(installedPath);
                return true;
            }

            return false;
        });

        return installStatus;
    }

    public async Task Install()
    {
        TaskDownloadPluginItem.Install((await GetReleases()).First(), await GetPluginInfo());
    }

    public async Task ReInstall()
    {
        Delete();
        TaskDownloadPluginItem.Install((await GetReleases()).First(), await GetPluginInfo());
    }

    public void Delete()
    {
        PluginLoader.Delete(_packConfig);
    }

    public Task<List<ResourceFileInfo>> GetFiles() => throw new System.NotImplementedException();
}