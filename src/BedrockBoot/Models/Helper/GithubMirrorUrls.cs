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
}
