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
using BedrockBoot.Standard.Enum.Config;

namespace BedrockBoot.Models.Helper;

/// <summary>
///     启动器自身的「强制下载」直链。
///     <para>
///     这条通道不经过 api.github.com，因此查不到版本信息（限流、断网）时依然可用。
///     正式版使用 <c>releases/latest/download</c>，GitHub 会 302 到最新的非预发布 Release；
///     但 latest 永远不指向预发布版本，所以预览版必须带具体 Tag，见 <see cref="PreviewTag" />。
///     </para>
/// </summary>
public static class LauncherDirectDownloadHelper
{
    private const string Owner = "Round-Studio";
    private const string Repo = "BedrockBoot";

    /// <summary>
    ///     预览版强制下载使用的 Tag，手动维护：
    ///     每次发布预览版后把它改成对应的 Tag（如 <c>v26.11.22.0308</c>）。
    ///     留空表示暂不支持预览版强制下载，界面上不会出现该按钮。
    /// </summary>
    public const string PreviewTag = "";

    /// <summary>与发布 workflow 的产物重命名保持一致</summary>
    public static string AssetName => OperatingSystem.IsLinux()
        ? "BedrockBoot-x86_64-linux.AppImage"
        : "BedrockBoot-x86_64-win.exe";

    /// <summary>正式版直链：latest 指向最新的正式 Release</summary>
    public static string ReleaseUrl =>
        $"https://github.com/{Owner}/{Repo}/releases/latest/download/{AssetName}";

    /// <summary>预览版直链：GitHub 不支持预发布的 latest，必须写死 Tag</summary>
    public static string PreviewUrl =>
        $"https://github.com/{Owner}/{Repo}/releases/download/{PreviewTag}/{AssetName}";

    /// <summary>
    ///     当前更新通道可用的强制下载地址；该通道没有直链时返回 null。
    ///     Commit 通道的产物挂在 Actions Artifact 上，只能带 Token 走 API，故不提供。
    /// </summary>
    public static string? GetUrl(UpdateType updateType)
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
            return null;

        return updateType switch
        {
            UpdateType.Release => ReleaseUrl,
            UpdateType.Preview => string.IsNullOrWhiteSpace(PreviewTag) ? null : PreviewUrl,
            _ => null
        };
    }
}
