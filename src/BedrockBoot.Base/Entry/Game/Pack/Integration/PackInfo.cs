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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game.Pack.Integration;

public class PackInfo
{
    [JsonPropertyName("packVersion")] public int PackVersion { get; set; } = 1;
    [JsonPropertyName("name")] public string Name { get; set; } = "pack.name";
    [JsonPropertyName("description")] public string Description { get; set; } = "pack.description";
    [JsonPropertyName("version")] public string Version { get; set; } = "0.0.0.1";
    [JsonPropertyName("author")] public List<PackAuthor> Authors { get; set; }
    [JsonPropertyName("versionInfo")] public GameVersionInfo VersionInfo { get; set; }
    [JsonIgnore] public PackEnableConfig EnableConfig { get; set; } = new();
    [JsonIgnore] public VersionConfig? VersionConfig { get; set; } = null;
    [JsonIgnore] public string PackIconFile { get; set; } = string.Empty;
    [JsonIgnore] public string PackSavePath { get; set; } = string.Empty;

    public class GameVersionInfo
    {
        [JsonPropertyName("buildType")] public string BuildType { get; set; } = string.Empty;
        [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;
    }
}