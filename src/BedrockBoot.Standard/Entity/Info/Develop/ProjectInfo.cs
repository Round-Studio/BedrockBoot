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
using Round.SDK.Entry;

namespace BedrockBoot.Standard.Entity.Info.Develop;

public class ProjectInfo
{
    [JsonPropertyName("projectName")] public string ProjectName { get; set; } = "";
    [JsonIgnore] public string ProjectPath { get; set; } = "";
    [JsonPropertyName("packInfo")] public PackConfig PackInfo { get; set; } = new PackConfig();
}