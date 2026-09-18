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

namespace BedrockBoot.Base.Entry.Game.Pack.Archive.Export;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class TemplateManifest
{
    [JsonPropertyName("format_version")] public int FormatVersion { get; set; } = 2;
    [JsonPropertyName("header")] public HeaderEntry Header { get; set; }
    [JsonPropertyName("modules")] public List<ModuleEntry> Modules { get; set; }

    public class HeaderEntry
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("base_game_version")] public List<int> BaseGameVersion { get; set; }
        [JsonPropertyName("uuid")] public string Uuid { get; set; }
        [JsonPropertyName("version")] public List<int> Version { get; set; }
        [JsonPropertyName("allow_random_seed")] public bool AllowRandomSeed { get; set; }
        [JsonPropertyName("lock_template_options")] public bool LockTemplateOptions { get; set; }
        [JsonPropertyName("platform_locked")] public bool PlatformLocked { get; set; } = false;
    }

    public class ModuleEntry
    {
        [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
        [JsonPropertyName("type")] public string Type { get; set; } = "world_template";
        [JsonPropertyName("uuid")] public string Uuid { get; set; }
        [JsonPropertyName("version")] public List<int> Version { get; set; }
    }
}