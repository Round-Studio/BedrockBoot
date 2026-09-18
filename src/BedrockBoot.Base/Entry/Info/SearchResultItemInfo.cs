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
using BedrockBoot.Base.Enum.Search;

namespace BedrockBoot.Base.Entry.Info;

public class SearchResultItemInfo
{
    public string IconUri { get; set; } = string.Empty;
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> Authors { get; set; } = new();
    public DateTime DateUpdated { get; set; }
    public DateTime DateCreated { get; set; }
    public uint DownloadCount { get; set; } = 0;
    public string Id { get; set; }
    public List<string> Labels { get; set; } = new();
    public Type DataType { get; set; }
    public string JsonData { get; set; } = string.Empty;
    public Action<string>? OnClick { get; set; }
    public List<string>? Images { get; set; }
    public string SourceWebsite { get; set; }
    public SearchResourceType ResourceType { get; set; }
}