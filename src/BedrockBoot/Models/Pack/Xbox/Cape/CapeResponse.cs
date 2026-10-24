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

namespace BedrockBoot.Models.Pack.Xbox.Cape;

public class CapeResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }

    [JsonPropertyName("totalCapes")] public int TotalCapes { get; set; }

    [JsonPropertyName("capes")] public List<Cape> Capes { get; set; } = new();
}

public class Cape
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";

    [JsonPropertyName("title")] public string Title { get; set; } = "";

    [JsonPropertyName("ownership")] public string Ownership { get; set; } = "";

    [JsonPropertyName("rarity")] public string Rarity { get; set; } = "";

    [JsonPropertyName("thumbnail")] public string Thumbnail { get; set; } = "";

    [JsonPropertyName("packUuid")] public string PackUuid { get; set; } = "";
}