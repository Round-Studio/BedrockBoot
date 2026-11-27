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

using Avalonia.Controls;
using BedrockBoot.Standard.Entity.Config;
using BedrockBoot.Standard.Enum.Type;

namespace BedrockBoot.Standard.Interface;

public class IWidgetTemplated : UserControl
{
    public List<WidgetSize> SupportWidgetSize { get; set; } = new()
    {
        WidgetSize.ExtraLarge,
        WidgetSize.Large,
        WidgetSize.Medium,
        WidgetSize.Small
    };

    /// <summary>
    ///     下发该组件实例的布局配置。创建后立即调用一次，组件可据此恢复自定义设置。
    /// </summary>
    public virtual void LoadWidgetConfig(WidgetLayoutData config)
    {
    }
}