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
///     一个键值来源。每个程序集（或插件）自带一个实现，各自持有自己的语言文件。
/// </summary>
public interface IStringSource
{
    /// <summary>来源标识，仅用于日志与冲突排查。</summary>
    string Id { get; }

    /// <summary>
    ///     加载指定语言的全部键值；该来源没有这门语言时返回 <c>null</c>。
    ///     实现方应自行缓存，避免被重复调用时反复解析文件。
    /// </summary>
    IReadOnlyDictionary<string, string>? Load(string culture);
}
