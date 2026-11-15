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

using BedrockBoot.Standard.Entity.Pack.Market;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Account.Github;
using Octokit;
using Octokit.Internal;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Pack.Plugin.Market;

public class MarketClient
{
    private readonly HttpClient _httpClient;
    private static string _apiUrl = SourceList.PluginApi;

    public MarketClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <summary>
    /// 从市场获取所有插件列表
    /// </summary>
    public async Task<List<MarketResponse.PluginInfo>> GetPluginsAsync()
    {
        Console.WriteLine(@"获取可下载的插件列表...");
        try
        {
            var response = await _httpClient.GetFromJsonAsync<MarketResponse>(_apiUrl);

            return response?.Plugins ?? new List<MarketResponse.PluginInfo>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"获取插件列表失败: {ex.Message}");
            return new List<MarketResponse.PluginInfo>();
        }
    }

    public static async Task<(Repository Repository, IReadOnlyList<Release> Releases)> GetPluginRepositoryFullInfo(
        MarketResponse.PluginInfo info)
    {
        Console.WriteLine($@"获取目标插件仓库信息：{info.PluginName}");
        var github = GitHubApiClient.CreateClient();
        var owner = info.RepositoryOwner;
        var repo = info.RepositoryName;

        // 并行获取数据以提高性能
        var repositoryTask = github.Repository.Get(owner, repo);
        var releasesTask = github.Repository.Release.GetAll(owner, repo);

        try
        {
            await Task.WhenAll(repositoryTask, releasesTask);

            return (await repositoryTask, await releasesTask);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"获取插件仓库信息失败：{ex}");
            throw;
        }
    }

    public static async Task<string> GetReadmeHtml(string owner, string repo)
    {
        Console.WriteLine($@"获取仓库 README: {owner}/{repo}");
        using (var httpClient = new HttpClient())
        using (var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/readme"))
        {
            request.Headers.Add("Accept", "application/vnd.github.html");
            request.Headers.Add("User-Agent", "BedrockBoot");

            var token = GitHubApiClient.GetUserToken();
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new global::System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            return null;
        }
    }
}