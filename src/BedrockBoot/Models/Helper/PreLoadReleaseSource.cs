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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Helper;

/// <summary>PreLoad.NET 的发布通道</summary>
public enum PreLoadReleaseChannel
{
    /// <summary>正式版（Releases 中的 latest）</summary>
    Stable = 0,

    /// <summary>预览版（预发布通道，待上游提供版本号来源）</summary>
    Preview = 1
}

/// <summary>版本号查询结果</summary>
/// <param name="Tag">Release Tag，例如 v26.11.22.0308</param>
/// <param name="Channel">该版本号所属通道</param>
public sealed record PreLoadReleaseInfo(string Tag, PreLoadReleaseChannel Channel);

/// <summary>
///     PreLoad.NET 的版本号、镜像与更新内容来源。
///
///     <para>
///     “怎么拿版本号、走哪些镜像、更新内容从哪读”统一收敛在本类：启动守卫、启动器更新前
///     检查、设置页的手动检查都只依赖这里的接口。上游后续补完信息时，通常只需要改本类的
///     几个 URL 常量与 <see cref="MirrorPrefixes" />，调用方不用动。
///     </para>
///
///     <para>
///     现状：正式版在 GitHub API 可用时仍以 API 为准（能同时拿到资产对象，可带授权头下载）；
///     本类提供的是 API 不可用时的兜底通道——直接读仓库 main 分支上的纯文本版本文件，
///     属于普通网页请求，不消耗 api.github.com 的 Rate Limit 配额。
///     预览版版本号、专用镜像、更新内容目前只有接口与占位地址，等上游确定后填上即可生效。
///     </para>
/// </summary>
public static class PreLoadReleaseSource
{
    /// <summary>PreLoad.NET 发布仓库（独立仓库，由 PreLoad.NET 的 workflow 发布）</summary>
    public const string Owner = "Round-Studio";

    public const string Repo = "PreLoad.NET-Release";

    /// <summary>Release 中唯一的资产名，与发布 workflow 的产物命名保持一致</summary>
    public const string AssetName = "PreLoad.NET.dll";

    /// <summary>
    ///     版本号文件所在的分支：PreLoad.NET-Release 仓库的 main，根目录下的纯文本文件。
    ///     内容形如 <c>26.11.27.1705</c>（不带 v 前缀），与 Release Tag <c>v26.11.27.1705</c> 对应。
    /// </summary>
    private const string VersionBranch = "main";

    /// <summary>正式版版本号文件名</summary>
    private const string StableVersionFile = "version";

    /// <summary>TODO(上游)：预览版版本号的文件名确定后替换</summary>
    private const string PreviewVersionFile = "version-preview";

    /// <summary>正式版版本号文件地址：GitHub API 不可用时的兜底来源</summary>
    public static string StableVersionUrl => VersionFileUrl(StableVersionFile, VersionBranch);

    /// <summary>预览版版本号文件地址（占位，待上游提供）</summary>
    public static string PreviewVersionUrl => VersionFileUrl(PreviewVersionFile, VersionBranch);

    /// <summary>TODO(上游)：更新内容（Release Notes）来源确定后替换</summary>
    public static string ReleaseNotesUrl => VersionFileUrl("changelog.md", VersionBranch);

    /// <summary>
    ///     额外的镜像前缀（<c>{url}</c> 为 GitHub 原始地址），按优先级排列。
    ///     <para>
    ///     默认空：下载走 <c>GithubFilesDownloader</c>，它本身会在
    ///     <see cref="SourceList.UpdateDownloadSources" /> 里测速选源；
    ///     这里只影响不经下载器的纯文本请求（版本号、更新内容）。
    ///     TODO(上游)：指定专用镜像后填入，例如 <c>https://gh-proxy.com/{url}</c>。
    ///     </para>
    /// </summary>
    public static IReadOnlyList<string> MirrorPrefixes { get; } = Array.Empty<string>();

    /// <summary>Release 页的 latest 直链：GitHub 服务端 302 到当前最新 Tag 的资产地址</summary>
    public static string LatestDownloadUrl =>
        $"https://github.com/{Owner}/{Repo}/releases/latest/download/{AssetName}";

    /// <summary>版本号请求的总预算，超时即判定该通道不可用（调用方处在阻塞路径上）</summary>
    private static readonly TimeSpan TextFetchTimeout = TimeSpan.FromSeconds(15);

    /// <summary>取版本号文件的地址，按通道区分</summary>
    public static string GetVersionUrl(PreLoadReleaseChannel channel) =>
        channel == PreLoadReleaseChannel.Preview ? PreviewVersionUrl : StableVersionUrl;

    /// <summary>取版本号文件的候选地址（原始地址 + 镜像），按优先级排列</summary>
    public static IReadOnlyList<string> GetVersionUrls(PreLoadReleaseChannel channel) =>
        GetMirroredUrls(GetVersionUrl(channel));

    /// <summary>取更新内容的候选地址（原始地址 + 镜像）</summary>
    public static IReadOnlyList<string> GetReleaseNotesUrls() => GetMirroredUrls(ReleaseNotesUrl);

    /// <summary>
    ///     取某个版本对应的资产地址。
    ///     <para>
    ///     版本文件里的号不带前缀（<c>26.11.27.1705</c>），而 Release Tag 带
    ///     <c>v</c>（<c>v26.11.27.1705</c>）；拿到的号不能确认就是 Tag，
    ///     此时退回 latest 直链（永远指向当前最新产物），避免拼出 404 地址。
    ///     </para>
    /// </summary>
    public static string GetDownloadUrl(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || !tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            return LatestDownloadUrl;

        return BuildReleaseAssetUrl(tag);
    }

    /// <summary>把地址展开成“原始地址 + 统一加速源 + 预留专用镜像”的候选列表（去重保序）</summary>
    public static IReadOnlyList<string> GetMirroredUrls(string url)
    {
        var urls = new List<string> { url };

        void Add(string candidate)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && !urls.Contains(candidate)) urls.Add(candidate);
        }

        // 先按上游指定的专用镜像，再用启动器统一的加速源表兜底
        foreach (var prefix in MirrorPrefixes) Add(prefix.Replace("{url}", url));

        foreach (var source in SourceList.UpdateDownloadSources.Values)
        {
            // {route} 形式的模板是给同域名拼接用的，套不了完整 URL
            if (source.Contains("{route}", StringComparison.Ordinal)) continue;
            Add(source.Replace("{url}", url));
        }

        return urls;
    }

    /// <summary>
    ///     按约定读取版本号。所有候选地址都失败、内容为空或明显是错误页面时返回 null。
    /// </summary>
    public static async Task<PreLoadReleaseInfo?> TryGetLatestAsync(
        PreLoadReleaseChannel channel = PreLoadReleaseChannel.Stable,
        CancellationToken cancellationToken = default)
    {
        var text = await TryGetTextAsync(GetVersionUrls(channel), cancellationToken);
        if (text == null) return null;

        var tag = text.Trim().Trim('"', '\'', ' ');
        if (tag.Length == 0 || tag.Length > 64 || tag.Contains('<'))
        {
            Console.WriteLine($@"PreLoad.NET 版本号内容异常，按不可用处理：{Truncate(text)}");
            return null;
        }

        Console.WriteLine($@"PreLoad.NET 版本号来源返回 {tag}（通道：{channel}）");
        return new PreLoadReleaseInfo(tag, channel);
    }

    /// <summary>
    ///     取某个版本的更新内容，供更新提示展示。
    ///     TODO(上游)：更新内容格式确定后实现（读 <see cref="ReleaseNotesUrl" /> 或 Release Body）。
    ///     当前返回 null，调用方应按“没有更新内容”处理。
    /// </summary>
    public static Task<string?> TryGetReleaseNotesAsync(
        string? tag,
        CancellationToken cancellationToken = default)
    {
        _ = tag;
        _ = cancellationToken;
        return Task.FromResult<string?>(null);
    }

    private static string VersionFileUrl(string file, string branch) =>
        $"https://raw.githubusercontent.com/{Owner}/{Repo}/refs/heads/{branch}/{file}";

    private static string BuildReleaseAssetUrl(string tag) =>
        $"https://github.com/{Owner}/{Repo}/releases/download/{Uri.EscapeDataString(tag)}/{AssetName}";

    /// <summary>依次尝试候选地址，返回第一个成功且非空的响应文本</summary>
    private static async Task<string?> TryGetTextAsync(IReadOnlyList<string> urls, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TextFetchTimeout);

        using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "BedrockBoot");

        foreach (var url in urls)
        {
            try
            {
                using var response = await client.GetAsync(url, timeout.Token);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($@"PreLoad.NET 版本号地址不可用（{url}）：HTTP {(int)response.StatusCode}");
                    continue;
                }

                var text = await response.Content.ReadAsStringAsync(timeout.Token);
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                Console.WriteLine($@"PreLoad.NET 版本号获取超时（{TextFetchTimeout.TotalSeconds:F0} 秒），放弃该通道");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"PreLoad.NET 版本号地址请求失败（{url}）：{ex.Message}");
            }
        }

        return null;
    }

    private static string Truncate(string text) =>
        text.Length <= 80 ? text.ReplaceLineEndings(" ") : text[..80].ReplaceLineEndings(" ") + "…";
}
