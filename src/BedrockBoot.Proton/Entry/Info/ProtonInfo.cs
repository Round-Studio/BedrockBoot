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
using BedrockBoot.Proton.Enum;

namespace BedrockBoot.Proton.Entry.Info;

public class ProtonInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;
    [JsonPropertyName("branch")] public ProtonSource Branch { get; set; }
    [JsonPropertyName("isDefault")] public bool IsDefault { get; set; } = false;
    [JsonPropertyName("isGameInputInstalled")] public bool IsGameInputInstalled { get; set; } = false;
    [JsonPropertyName("installDate")] public DateTime InstallDate { get; set; } = DateTime.Now;
    [JsonPropertyName("releaseUrl")] public string ReleaseUrl { get; set; } = string.Empty;
    [JsonPropertyName("releaseSize")] public long ReleaseSize { get; set; } = 0;
    [JsonIgnore] public string InstallPath { get; set; } = string.Empty;
}