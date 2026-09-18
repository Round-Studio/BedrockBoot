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
using BedrockBoot.Base.Enum;

namespace BedrockBoot.Base.Entry.Pack.Theme
{
    public class ThemePackManifest
    {
        [JsonIgnore] public string? PackHash { get; set; } = string.Empty;
        [JsonIgnore] public bool IsSelectThis { get; set; } = false;
    
        [JsonPropertyName("formatVersion")] public int FormatVersion { get; set; } = 1;
        [JsonPropertyName("packName")] public string? PackName { get; set; } = "Unknown";
        [JsonPropertyName("packDescription")] public string? PackDescription { get; set; } = "Unknown";
        [JsonPropertyName("packAuthor")] public string? PackAuthor { get; set; } = "Unknown";
        [JsonPropertyName("packSupport")] public List<string>? PackSupport { get; set; } = new() { "BedrockBoot" };
    
        [JsonPropertyName("themeType")] public ThemeModelEnum ThemeType { get; set; } = ThemeModelEnum.Dark;
        [JsonPropertyName("themeColorCode")] public string? ThemeColor { get; set; } = string.Empty;
    
        [JsonPropertyName("backgroundUse3D")] public bool BackgroundUse3D { get; set; } = false;
        [JsonPropertyName("backgroundImageOpacity")] public int BackgroundImageOpacity { get; set; } = 100;
        [JsonPropertyName("backgroundImageBlur")] public int BackgroundImageBlur { get; set; } = 1;
        [JsonPropertyName("backgroundAnimation")] public bool BackgroundAnimation { get; set; } = false;
    
        [JsonPropertyName("backgroundImageFileName")] public string? BackgroundImageFileName { get; set; } = string.Empty;
        [JsonPropertyName("backgroundMusicFileName")] public string? BackgroundMusicFileName { get; set; } = string.Empty;
        [JsonPropertyName("packIconFileName")] public string? PackIconFileName { get; set; } = string.Empty;
    }
}