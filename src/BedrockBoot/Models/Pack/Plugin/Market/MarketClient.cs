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
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Account.Github;
using Octokit;
using Octokit.Internal;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
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
        var owner = info.RepositoryOwner;
        var repo = info.RepositoryName;

        try
        {
            // 并行获取数据以提高性能；401 时 ExecuteAsync 会刷新令牌后整体重试一次
            return await GitHubApiClient.ExecuteAsync(async github =>
            {
                var repositoryTask = github.Repository.Get(owner, repo);
                var releasesTask = github.Repository.Release.GetAll(owner, repo);
                await Task.WhenAll(repositoryTask, releasesTask);

                return (await repositoryTask, await releasesTask);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"获取插件仓库信息失败：{ex}");
            throw;
        }
    }

    public static async Task<string?> GetReadmeHtml(string owner, string repo)
    {
        Console.WriteLine($@"获取仓库 README: {owner}/{repo}");

        try
        {
            var token = await GitHubTokenManager.GetValidTokenAsync();
            var (success, html) = await RequestReadmeAsync(owner, repo, token);

            // 令牌被提前撤销（密码变更、用户撤销授权等）时会返回 401：强制续期后重试一次
            if (!success && !string.IsNullOrEmpty(token))
            {
                token = await GitHubTokenManager.GetValidTokenAsync(true, token);
                if (!string.IsNullOrEmpty(token))
                    (success, html) = await RequestReadmeAsync(owner, repo, token);
            }

            if (success && !string.IsNullOrWhiteSpace(html)) return html;
        }
        catch (Exception ex)
        {
            // 限流、断网、DNS 失败都走同一条兜底，不让详情页因为 API 挂了就白屏
            Console.WriteLine($@"README 接口请求失败（改用 raw）：{ex.Message}");
        }

        return await RequestReadmeRawAsync(owner, repo);
    }

    /// <summary>
    ///     raw 兜底：直接读仓库里的 README 文件，走与版本号、更新日志相同的镜像链。
    ///     返回的是 Markdown，交给 <c>HtmlToControlConverter</c> 时会自动转成控件。
    /// </summary>
    private static async Task<string?> RequestReadmeRawAsync(string owner, string repo)
    {
        // raw 地址对文件名大小写敏感，市场上两种写法都出现过，按顺序各试一次
        foreach (var fileName in ReadmeCandidateFiles)
        {
            var url = $"https://raw.githubusercontent.com/{owner}/{repo}/HEAD/{fileName}";
            var text = await GithubMirrorUrls.TryGetTextAsync(url, $"README({fileName})",
                RawTotalTimeout, RawPerRequestTimeout, IsReadmeText);

            if (!string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine($@"README 已通过 raw 兜底获取：{owner}/{repo}/{fileName}");
                return ResolveRelativeUrls(text, owner, repo, fileName);
            }
        }

        Console.WriteLine($@"README 的 raw 兜底也失败：{owner}/{repo}");
        return null;
    }

    /// <summary>
    ///     API 返回的是 GitHub 渲染后的 HTML，图片等相对地址已经被换成了绝对地址；
    ///     raw 拿到的是原始 Markdown，这里按仓库 HEAD 补全，避免兜底时图片全裂。
    /// </summary>
    private static string ResolveRelativeUrls(string text, string owner, string repo, string fileName)
    {
        var baseUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/HEAD/";
        var directory = Path.GetDirectoryName(fileName);

        var prefix = string.IsNullOrEmpty(directory)
            ? baseUrl
            : $"{baseUrl}{directory.Replace('\\', '/')}/";

        string Absolutize(string target)
        {
            if (string.IsNullOrWhiteSpace(target)) return target;
            if (target.StartsWith("//", StringComparison.Ordinal)) return target;

            var trimmed = target.Trim();
            if (trimmed.Contains("://", StringComparison.Ordinal) ||
                trimmed.StartsWith('#') ||
                trimmed.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return target;

            return prefix + trimmed.TrimStart('.', '/');
        }

        // Markdown 链接/图片：](target)；HTML 属性：src="target"
        text = Regex.Replace(text, @"(?<=\]\()(?<target>[^)\s]+)", m => Absolutize(m.Groups["target"].Value));
        text = Regex.Replace(text, @"(?<=src="")[^""]+", m => Absolutize(m.Value));

        return text;
    }

    /// <summary>市场收录的仓库约定用 README.md；小写写法在部分仓库里存在</summary>
    private static readonly string[] ReadmeCandidateFiles = ["README.md", "readme.md"];

    /// <summary>raw 兜底的总预算：详情页在等这份内容，宁可早点失败后展示空页</summary>
    private static readonly TimeSpan RawTotalTimeout = TimeSpan.FromSeconds(12);

    /// <summary>单个镜像源的预算</summary>
    private static readonly TimeSpan RawPerRequestTimeout = TimeSpan.FromSeconds(4);

    /// <summary>raw 拿不到文件时返回 <c>404: Not Found</c>，个别加速源会回一张 HTML 错误页</summary>
    private static bool IsReadmeText(string text)
    {
        var trimmed = text.TrimStart();
        if (trimmed.Length < 5) return false;

        return !trimmed.StartsWith("404", StringComparison.Ordinal) &&
               !trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) &&
               !trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<(bool Success, string Html)> RequestReadmeAsync(string owner, string repo,
        string? token)
    {
        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.github.com/repos/{owner}/{repo}/readme");

        request.Headers.Add("Accept", "application/vnd.github.html");
        request.Headers.Add("User-Agent", "BedrockBoot");

        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization =
                new global::System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return (true, await response.Content.ReadAsStringAsync());

        return (false, null);
    }
}