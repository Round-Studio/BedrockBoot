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
using BedrockBoot.Standard.Enum.Type;

namespace BedrockBoot.Standard.Entity.Config;

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

    /// <summary>
    ///     组件绑定的游戏目录（绝对路径）。为空表示跟随实例列表中当前选中的目录。
    /// </summary>
    [JsonPropertyName("gameFolderPath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GameFolderPath { get; set; }

    /// <summary>
    ///     组件绑定的实例目录名，与 <see cref="GameFolderPath" /> 一起指向一个固定版本。
    ///     为空表示跟随实例列表中当前选中的版本。
    /// </summary>
    [JsonPropertyName("gameVersionFolder")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GameVersionFolder { get; set; }

    /// <summary>
    ///     复制布局数据，避免导出时把可变对象直接交给序列化之外的代码。
    /// </summary>
    public WidgetLayoutData Clone() => new()
    {
        GridX = GridX,
        GridY = GridY,
        WidgetType = WidgetType,
        Size = Size,
        GameFolderPath = GameFolderPath,
        GameVersionFolder = GameVersionFolder
    };
}