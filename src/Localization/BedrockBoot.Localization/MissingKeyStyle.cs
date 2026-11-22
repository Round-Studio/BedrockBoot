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

namespace BedrockBoot.Localization;

/// <summary>
///     当前语言没有译文（值来自回退链）时的显示策略。
/// </summary>
/// <remarks>
///     外语语言文件普遍只覆盖一部分键，回退是常态而非异常，因此"怎么显示回退值"
///     必须是可选的：发布态要静默（用户看到的是完整句子，只是语言不对），
///     翻译校对时才需要标记（一眼看出哪些还没翻）。
///     <para>
///         枚举值直接作为设置页下拉框的索引使用，不要调整顺序。
///     </para>
/// </remarks>
public enum MissingKeyStyle
{
    /// <summary>静默回退：直接显示兜底语言的文本。</summary>
    Silent = 0,

    /// <summary>标记回退值：用 <c>⟦ ⟧</c> 包裹兜底文本。</summary>
    MarkValue = 1,

    /// <summary>显示键名：用 <c>⟦ ⟧</c> 包裹键名，便于定位缺失的键。</summary>
    ShowKey = 2,
}
