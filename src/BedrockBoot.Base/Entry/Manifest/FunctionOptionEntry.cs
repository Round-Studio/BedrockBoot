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

namespace BedrockBoot.Base.Entry.Manifest;

public class FunctionOptionEntry
{
    [JsonPropertyName("isEnableImportGamePack")]
    public bool IsEnableImportGamePack { get; set; }

    [JsonPropertyName("isEnableGameInstanceControl")]
    public bool IsEnableGameInstanceControl { get; set; }

    [JsonPropertyName("isEnableGameInstanceMods")]
    public bool IsEnableGameInstanceMods { get; set; }

    [JsonPropertyName("isEnableWebProtocol")]
    public bool IsEnableWebProtocol { get; set; }

    [JsonPropertyName("isEnableSettingPersonalization")]
    public bool IsEnableSettingPersonalization { get; set; }

    [JsonPropertyName("isEnableSettingBackground")]
    public bool IsEnableSettingBackground { get; set; }

    [JsonPropertyName("isEnableSettingColor")]
    public bool IsEnableSettingColor { get; set; }

    [JsonPropertyName("isEnablePlugin")] public bool IsEnablePlugin { get; set; }
    [JsonPropertyName("isEnableToolsBox")] public bool IsEnableToolsBox { get; set; }

    [JsonPropertyName("isEnableMouseLock")]
    public bool IsEnableMouseLock { get; set; }

    [JsonPropertyName("isEnableMcPackOpenWithBody")]
    public bool IsEnableMcPackOpenWithBody { get; set; }

    [JsonPropertyName("isEnableSettingPersonalizationHome")]
    public bool IsEnableSettingPersonalizationHome { get; set; }
    
    [JsonPropertyName("isEnableToolsBoxUsingPackTranslate")]
    public bool IsEnableToolsBoxUsingPackTranslate { get; set; }
    
    [JsonPropertyName("isEnableGameProtonManager")]
    public bool IsEnableGameProtonManager { get; set; }
}