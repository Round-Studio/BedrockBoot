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

namespace BedrockBoot.Standard.Entity.Info.Download;

public class ResourceFileInfo
{
    public string? VersionGroup { get; set; }

    // 结构化分组（优先于 VersionGroup）：GroupKey 是分组唯一键，Title/Subtitle 是分组标题两行
    public string? GroupKey { get; set; }
    public string? GroupTitle { get; set; }
    public string? GroupSubtitle { get; set; }

    // 预发布角标（与 CurseForge 的 ReleaseType 数值语义无关）
    public bool IsPrerelease { get; set; } = false;

    public string? FileName { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public uint FileSize { get; set; } = 0;
    public int? ReleaseType { get; set; }
    public int? DownloadCount { get; set; }
    public List<string>? GameVersions { get; set; }
    public string? Version { get; set; }
    public Action<string>? OnDownload { get; set; }
    public Action<string>? OnSaveAs { get; set; }
    public bool IsEnableSaveAs { get; set; } = false;
}