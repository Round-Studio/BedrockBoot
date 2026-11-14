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

using System.Text.Json.Serialization;

namespace BedrockBoot.Standard.Entity.Game.Pack.Capes.Manifest;

public class CapePackManifest
{
    [JsonPropertyName("format_version")] public int FormatVersion { get; set; } = 1;
    [JsonPropertyName("header")] public CapePackHeader Header { get; set; } = new();
    [JsonPropertyName("modules")] public List<CapePackModule> Modules { get; set; } = new();
    [JsonPropertyName("metadata")] public CapePackMetadata? Metadata { get; set; }
}

public class CapePackHeader
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("uuid")] public string Uuid { get; set; } = "";
    [JsonPropertyName("version")] public List<int> Version { get; set; } = new();
}

public class CapePackModule
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("uuid")] public string Uuid { get; set; } = "";
    [JsonPropertyName("version")] public List<int> Version { get; set; } = new();
}

public class CapePackMetadata
{
    [JsonPropertyName("authors")] public List<string> Authors { get; set; } = new();
}