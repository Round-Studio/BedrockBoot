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
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Standard.Enum.Config;

namespace BedrockBoot.Models.Helper;

/// <summary>
///     启动器自身的版本号与更新日志来源（GitHub API 不可用时的兜底通道）。
///
///     <para>
///     发布 workflow 会把每次构建的信息写进仓库的 <c>version</c> 分支：
///     <list type="bullet">
///         <item><c>info/release</c>：正式版版本号，如 <c>2.1.11.103</c>（不带 v）。</item>
///         <item><c>info/preview</c>：预览版版本号，如 <c>26.11.27.1834</c>（不带 v）。</item>
///         <item><c>info/commit</c>：CI 提交版版本号。</item>
///         <item><c>changelog/&lt;版本号&gt;.md</c>：该版本的更新日志（Markdown）。</item>
///     </list>
///     Release Tag 就是 <c>v</c> + 版本号，因此拿到版本号就能拼出资产直链，不需要 API。
///     </para>
///
///     <para>
///     这些文件都在 raw.githubusercontent.com 上，属于普通网页请求，不消耗 api.github.com
///     的 Rate Limit 配额，限流或未登录时依然可读。
///     </para>
/// </summary>
public static class LauncherReleaseSource
{
    private const string Owner = "Round-Studio";
    private const string Repo = "BedrockBoot";

    /// <summary>发布 workflow 写入构建信息的分支</summary>
    private const string VersionBranch = "version";

    /// <summary>正式版版本号文件</summary>
    private const string ReleaseVersionFile = "info/release";

    /// <summary>预览版版本号文件</summary>
    private const string PreviewVersionFile = "info/preview";

    /// <summary>version 分支的 raw 根地址</summary>
    private static string RawBase =>
        $"https://raw.githubusercontent.com/{Owner}/{Repo}/refs/heads/{VersionBranch}";

    /// <summary>正式版版本号文件地址</summary>
    public static string ReleaseVersionUrl => $"{RawBase}/{ReleaseVersionFile}";

    /// <summary>预览版版本号文件地址</summary>
    public static string PreviewVersionUrl => $"{RawBase}/{PreviewVersionFile}";

    /// <summary>单次请求预算：这里是 API 失败后的兜底，宁可快速失败也别让用户干等</summary>
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(10);

    /// <summary>版本号形如 <c>2.1.11.103</c> 或 <c>26.11.27.1834</c>，只接受数字与点</summary>
    private static readonly Regex VersionPattern = new(@"^\d+(\.\d+){1,3}$",
        RegexOptions.Compiled);

    /// <summary>该更新通道在 version 分支上的版本号文件地址；没有对应文件时返回 null</summary>
    public static string? GetVersionUrl(UpdateType updateType) => updateType switch
    {
        UpdateType.Release => ReleaseVersionUrl,
        UpdateType.Preview => PreviewVersionUrl,
        _ => null
    };

    /// <summary>更新日志地址：<c>changelog/&lt;版本号&gt;.md</c></summary>
    public static string GetChangelogUrl(string version) => $"{RawBase}/changelog/{version}.md";

    /// <summary>版本号转 Release Tag：workflow 里 Tag 就是 <c>v</c> + 版本号</summary>
    public static string ToTag(string version) =>
        version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : $"v{version}";

    /// <summary>
    ///     读取指定通道的最新版本号（不带 v）。文件缺失、内容异常或网络失败返回 null，
    ///     由调用方决定是否继续抛错/走强制下载。
    /// </summary>
    public static async Task<string?> TryGetVersionAsync(UpdateType updateType,
        CancellationToken cancellationToken = default)
    {
        var url = GetVersionUrl(updateType);
        if (url == null) return null;

        var text = await TryGetTextAsync(url, "版本号", cancellationToken);
        if (text == null) return null;

        var version = text.Trim().Trim('"', '\'', ' ');
        if (!VersionPattern.IsMatch(version))
        {
            Console.WriteLine($@"启动器{updateType}通道的版本号内容异常，按不可用处理：{Truncate(text)}");
            return null;
        }

        Console.WriteLine($@"启动器版本号兜底来源返回 {version}（通道：{updateType}）");
        return version;
    }

    /// <summary>
    ///     读取某个版本的更新日志。workflow 还没写入该版本（或读不到）时返回 null，
    ///     调用方按“没有更新内容”处理即可，不影响更新流程。
    /// </summary>
    public static async Task<string?> TryGetChangelogAsync(string version,
        CancellationToken cancellationToken = default)
    {
        var text = await TryGetTextAsync(GetChangelogUrl(version), "更新日志", cancellationToken);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <summary>
    ///     按版本号拼出资产直链。Tag 与版本号一一对应，因此下载到的就是读到更新日志的那个构建。
    /// </summary>
    public static string GetDownloadUrl(string version)
    {
        var assetName = LauncherDirectDownloadHelper.AssetName;
        return $"https://github.com/{Owner}/{Repo}/releases/download/{ToTag(version)}/{assetName}";
    }

    private static async Task<string?> TryGetTextAsync(string url, string label,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(FetchTimeout);

        try
        {
            using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "BedrockBoot");

            using var response = await client.GetAsync(url, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($@"启动器{label}地址不可用（{url}）：HTTP {(int)response.StatusCode}");
                return null;
            }

            var text = await response.Content.ReadAsStringAsync(timeout.Token);
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            Console.WriteLine($@"启动器{label}获取超时（{FetchTimeout.TotalSeconds:F0} 秒），放弃该通道");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"启动器{label}地址请求失败（{url}）：{ex.Message}");
            return null;
        }
    }

    private static string Truncate(string text) =>
        text.Length <= 80 ? text.ReplaceLineEndings(" ") : text[..80].ReplaceLineEndings(" ") + "…";
}
