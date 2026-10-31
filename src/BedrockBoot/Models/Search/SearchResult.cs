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

using System;

namespace BedrockBoot.Models.Search;

/// <summary>
///     全局搜索的一条结果。
/// </summary>
public class SearchResult
{
    /// <summary>结果标题</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>结果副标题（可能为空）</summary>
    public string? Description { get; init; }

    /// <summary>是否含副标题，供模板绑定显示</summary>
    public bool HasDescription => !string.IsNullOrEmpty(Description);

    /// <summary>分类徽标文本（页面 / 设置 / 实例 / 聚合搜索）</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Segoe Fluent Icons 图标</summary>
    public string Glyph { get; init; } = "\uE721";

    /// <summary>关联的原始数据（如 VersionConfig），供外部使用</summary>
    public object? Payload { get; init; }

    /// <summary>命中分数，仅用于排序展示</summary>
    public double Score { get; set; }

    /// <summary>执行动作：关闭搜索浮层后在 UI 线程上调用</summary>
    public Action? Execute { get; init; }
}
