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
using BedrockBoot.Models.Global;
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
///     的 Rate Limit 配额，限流或未登录时依然可读。读取时与下载链路共用
///     <see cref="SourceList.UpdateDownloadSources" /> 里的加速源：原始地址失败会依次换镜像，
///     GitHub 直连不通时仍能拿到版本号和更新日志（见 <see cref="GithubMirrorUrls" />）。
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
    private static readonly TimeSpan TotalTimeout = TimeSpan.FromSeconds(12);

    /// <summary>单个候选地址（含镜像）的预算：文件只有几十字节，4 秒足够，失败立刻换源</summary>
    private static readonly TimeSpan PerRequestTimeout = TimeSpan.FromSeconds(4);

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
    ///     <para>
    ///     原始地址排在首位，后面依次是 <see cref="SourceList.UpdateDownloadSources" /> 里的
    ///     GitHub 加速源，与下载链路保持同一套镜像，GitHub 直连不通时仍能读到版本号。
    ///     </para>
    /// </summary>
    public static async Task<string?> TryGetVersionAsync(UpdateType updateType,
        CancellationToken cancellationToken = default)
    {
        var url = GetVersionUrl(updateType);
        if (url == null) return null;

        var text = await TryGetTextAsync(url, "版本号", IsVersionText, cancellationToken);
        if (text == null) return null;

        var version = text.Trim().Trim('"', '\'', ' ');
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
        // 部分加速源会把错误页以 200 返回；更新日志是 Markdown，出现 HTML/404 文本即判为坏源，
        // 交给校验函数拦掉后继续尝试下一个源
        return await TryGetTextAsync(GetChangelogUrl(version), "更新日志",
            text => !LooksLikeErrorPage(text), cancellationToken);
    }

    /// <summary>版本号正文的校验：只有数字与点才算可用，HTML 错误页会被挡掉</summary>
    private static bool IsVersionText(string text) =>
        VersionPattern.IsMatch(text.Trim().Trim('"', '\'', ' '));

    /// <summary>
    ///     按版本号拼出资产直链。Tag 与版本号一一对应，因此下载到的就是读到更新日志的那个构建。
    /// </summary>
    public static string GetDownloadUrl(string version)
    {
        var assetName = LauncherDirectDownloadHelper.AssetName;
        return $"https://github.com/{Owner}/{Repo}/releases/download/{ToTag(version)}/{assetName}";
    }

    /// <summary>
    ///     依次尝试“原始地址 + 各加速源”，返回第一个通过 <paramref name="isValid" /> 的非空正文。
    ///     每个候选地址单独限时，失败（含内容不可用）立刻换源；整体预算耗尽即放弃。
    /// </summary>
    private static async Task<string?> TryGetTextAsync(string url, string label,
        Func<string, bool> isValid, CancellationToken cancellationToken)
    {
        var urls = GithubMirrorUrls.Expand(url);

        using var total = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        total.CancelAfter(TotalTimeout);

        using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "BedrockBoot");

        foreach (var candidate in urls)
        {
            if (total.IsCancellationRequested)
            {
                Console.WriteLine($@"启动器{label}获取超出总预算（{TotalTimeout.TotalSeconds:F0} 秒），放弃剩余源");
                return null;
            }

            using var request = CancellationTokenSource.CreateLinkedTokenSource(total.Token);
            request.CancelAfter(PerRequestTimeout);

            try
            {
                using var response = await client.GetAsync(candidate, request.Token);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($@"启动器{label}地址不可用（{candidate}）：HTTP {(int)response.StatusCode}");
                    continue;
                }

                var text = await response.Content.ReadAsStringAsync(request.Token);
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (isValid(text)) return text;

                // 加速源把错误页当正文返回是常见现象，换个源继续而不是直接判死
                Console.WriteLine(
                    $@"启动器{label}内容不可用，换下一个源：{candidate}（{Truncate(text)}）");
            }
            catch (OperationCanceledException) when (total.IsCancellationRequested)
            {
                Console.WriteLine($@"启动器{label}获取超出总预算（{TotalTimeout.TotalSeconds:F0} 秒），放弃剩余源");
                return null;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine(
                    $@"启动器{label}地址超时（{PerRequestTimeout.TotalSeconds:F0} 秒），换下一个源：{candidate}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"启动器{label}地址请求失败（{candidate}）：{ex.Message}");
            }
        }

        return null;
    }

    /// <summary>加速源把错误页当正文返回时（HTML 或 raw 的 404 文本），这里拦掉</summary>
    private static bool LooksLikeErrorPage(string text)
    {
        var trimmed = text.TrimStart();
        return trimmed.StartsWith('<') ||
               trimmed.StartsWith("404", StringComparison.Ordinal) ||
               trimmed.StartsWith("Not Found", StringComparison.OrdinalIgnoreCase);
    }

    private static string Truncate(string text) =>
        text.Length <= 80 ? text.ReplaceLineEndings(" ") : text[..80].ReplaceLineEndings(" ") + "…";
}
