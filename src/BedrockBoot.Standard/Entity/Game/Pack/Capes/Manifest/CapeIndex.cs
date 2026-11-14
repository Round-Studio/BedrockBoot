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

public class CapeIndex
{
    [JsonPropertyName("serialize_name")]
    public string SerializeName { get; set; } = "";

    [JsonPropertyName("localization_name")]
    public string LocalizationName { get; set; } = "";

    [JsonPropertyName("capes")]
    public List<CapeDefinition> Capes { get; set; } = new();
}

public class CapeDefinition
{
    [JsonPropertyName("localization_name")]
    public string LocalizationName { get; set; } = "";

    [JsonPropertyName("texture")]
    public string Texture { get; set; } = "";

    [JsonPropertyName("rarity")]
    public string? Rarity { get; set; }

    [JsonPropertyName("elytra")]
    public string? Elytra { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }
}