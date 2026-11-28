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

/// <summary>
///     GitHub 地址的镜像展开。
///
///     <para>
///     下载走 <c>GithubFilesDownloader</c> 时会自动在 <see cref="SourceList.UpdateDownloadSources" />
///     里测速选源；但纯文本请求（版本号、更新日志）不经下载器，必须自己把同一张表套上去，
///     否则 GitHub 直连不通时这些请求就只剩失败一条路。
///     </para>
///
///     <para>
///     <c>{url}</c> 模板可以用完整地址替换（<c>https://gh-proxy.com/https://raw.githubusercontent.com/...</c>）；
///     <c>{route}</c> 形式的模板只适用于同域名拼接，套完整 URL 会拼出无效地址，直接跳过。
///     </para>
/// </summary>
public static class GithubMirrorUrls
{
    /// <summary>
    ///     把地址展开成“原始地址 + 统一加速源 + 调用方额外指定的镜像”的候选列表（去重保序）。
    /// </summary>
    /// <param name="url">GitHub 原始地址（github.com 或 raw.githubusercontent.com）</param>
    /// <param name="extraPrefixes">调用方在上游指定的专用镜像，排在统一加速源之前</param>
    public static IReadOnlyList<string> Expand(string url, IReadOnlyList<string>? extraPrefixes = null)
    {
        var urls = new List<string> { url };

        void Add(string candidate)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && !urls.Contains(candidate)) urls.Add(candidate);
        }

        if (extraPrefixes != null)
            foreach (var prefix in extraPrefixes)
                Add(prefix.Replace("{url}", url));

        foreach (var source in SourceList.UpdateDownloadSources.Values)
        {
            if (source.Contains("{route}", StringComparison.Ordinal)) continue;
            Add(source.Replace("{url}", url));
        }

        return urls;
    }

    /// <summary>
    ///     按镜像优先级读取原始地址的纯文本，返回第一个通过 <paramref name="isValid" /> 的正文。
    ///     <para>
    ///     每个源单独限时：GitHub 直连被墙时往往是干等而不是报错，共用一份预算会让后面的
    ///     镜像没有机会出手。内容不合格（加速源把错误页当 200 返回）也继续换源。
    ///     </para>
    /// </summary>
    /// <param name="url">GitHub 原始地址</param>
    /// <param name="label">日志里的用途名，例如「README」「版本号」</param>
    /// <param name="totalTimeout">整条镜像链的总预算</param>
    /// <param name="perRequestTimeout">单个源的预算</param>
    /// <param name="isValid">正文校验；为空表示只要非空就算可用</param>
    public static async Task<string?> TryGetTextAsync(string url, string label, TimeSpan totalTimeout,
        TimeSpan perRequestTimeout, Func<string, bool>? isValid = null,
        CancellationToken cancellationToken = default)
    {
        using var total = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        total.CancelAfter(totalTimeout);

        using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "BedrockBoot");

        foreach (var candidate in Expand(url))
        {
            if (total.IsCancellationRequested)
            {
                Console.WriteLine($@"{label}获取超出总预算（{totalTimeout.TotalSeconds:F0} 秒），放弃剩余源");
                return null;
            }

            using var request = CancellationTokenSource.CreateLinkedTokenSource(total.Token);
            request.CancelAfter(perRequestTimeout);

            try
            {
                using var response = await client.GetAsync(candidate, request.Token);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($@"{label}地址不可用（{candidate}）：HTTP {(int)response.StatusCode}");
                    continue;
                }

                var text = await response.Content.ReadAsStringAsync(request.Token);
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (isValid == null || isValid(text)) return text;
            }
            catch (OperationCanceledException) when (total.IsCancellationRequested)
            {
                Console.WriteLine($@"{label}获取超出总预算（{totalTimeout.TotalSeconds:F0} 秒），放弃剩余源");
                return null;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($@"{label}地址超时（{perRequestTimeout.TotalSeconds:F0} 秒），换下一个源：{candidate}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"{label}地址请求失败（{candidate}）：{ex.Message}");
            }
        }

        return null;
    }
}
