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

namespace BedrockBoot.Standard.Interface;

/// <summary>
///     需要用户单独配置的桌面组件（例如「游戏版本」组件可以固定到某个实例）。
///     工作区会据此在右键菜单里加出配置入口。
/// </summary>
public interface IWidgetConfigurable
{
    /// <summary>是否提供配置入口。</summary>
    bool HasSettings { get; }

    /// <summary>右键菜单里显示的配置项文字。</summary>
    string SettingsText { get; }

    /// <summary>打开该组件的配置界面。</summary>
    void ShowSettings();
}
