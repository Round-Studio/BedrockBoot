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

namespace BedrockBoot.Base.Entry.Info.Download;

public class ResourceFileInfo
{
    public string? VersionGroup { get; set; }
    public string? FileName { get; set; }
    public string? Description { get; set; }
    public uint FileSize { get; set; } = 0;
    public string? Version { get; set; }
    public Action<string>? OnDownload { get; set; }
    public Action<string>? OnSaveAs { get; set; }
    public bool IsEnableSaveAs { get; set; } = false;
}