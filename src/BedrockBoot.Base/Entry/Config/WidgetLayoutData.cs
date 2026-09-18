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
using BedrockBoot.Base.Enum.Type;

namespace BedrockBoot.Base.Entry.Config;

public class WidgetLayoutData
{
    [JsonPropertyName("gridX")]
    public int GridX { get; set; }
        
    [JsonPropertyName("gridY")]
    public int GridY { get; set; }
    [JsonPropertyName("widgetType")]
    public WidgetType WidgetType { get; set; } =  WidgetType.Timer;
        
    [JsonPropertyName("size")]
    public WidgetSize Size { get; set; }
}