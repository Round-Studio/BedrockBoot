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
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Standard.Enum.Config;

namespace BedrockBoot.Models.Helper;

/// <summary>
///     启动器自身的「强制下载」直链。
///     <para>
///     这条通道不经过 api.github.com，因此查不到版本信息（限流、断网）时依然可用。
///     正式版使用 <c>releases/latest/download</c>，GitHub 会 302 到最新的非预发布 Release；
///     但 latest 永远不指向预发布版本，所以预览版必须带具体 Tag——Tag 由 version 分支上的
///     <c>info/preview</c> 版本号拼出（见 <see cref="LauncherReleaseSource" />），
///     不需要再手动维护。
///     </para>
/// </summary>
public static class LauncherDirectDownloadHelper
{
    private const string Owner = "Round-Studio";
    private const string Repo = "BedrockBoot";

    /// <summary>Release 产物只覆盖 Windows 与 Linux，其它平台不提供自更新</summary>
    public static bool IsSupported => OperatingSystem.IsWindows() || OperatingSystem.IsLinux();

    /// <summary>与发布 workflow 的产物重命名保持一致</summary>
    public static string AssetName => OperatingSystem.IsLinux()
        ? "BedrockBoot-x86_64-linux.AppImage"
        : "BedrockBoot-x86_64-win.exe";

    /// <summary>正式版直链：latest 指向最新的正式 Release</summary>
    public static string ReleaseUrl =>
        $"https://github.com/{Owner}/{Repo}/releases/latest/download/{AssetName}";

    /// <summary>指定 Tag 的直链：预览版用它，GitHub 不支持预发布的 latest</summary>
    public static string GetUrlForTag(string tag) =>
        $"https://github.com/{Owner}/{Repo}/releases/download/{tag}/{AssetName}";

    /// <summary>
    ///     当前更新通道可用的强制下载地址；该通道没有直链时返回 null。
    ///     <para>
    ///     正式版优先用 version 分支上的版本号拼精确 Tag（与展示的更新日志一致），
    ///     读不到就退回 latest 直链；预览版的 latest 不含预发布，必须依赖版本号。
    ///     Commit 通道的产物挂在 Actions Artifact 上，只能带 Token 走 API，故不提供。
    ///     </para>
    /// </summary>
    public static async Task<string?> ResolveUrlAsync(UpdateType updateType,
        CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
            return null;

        if (updateType is not (UpdateType.Release or UpdateType.Preview))
            return null;

        var version = await LauncherReleaseSource.TryGetVersionAsync(updateType, cancellationToken);
        if (version != null) return LauncherReleaseSource.GetDownloadUrl(version);

        // 正式版还有 latest 这条不看版本号的退路；预览版没有，读不到版本号就只能如实返回
        return updateType == UpdateType.Release ? ReleaseUrl : null;
    }
}
