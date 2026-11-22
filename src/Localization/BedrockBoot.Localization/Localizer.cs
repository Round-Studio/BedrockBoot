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

using System.Globalization;

namespace BedrockBoot.Localization;

/// <summary>
///     查串的统一入口。UI 框架无关：任何程序集引用 <c>BedrockBoot.Localization</c> 即可使用。
/// </summary>
/// <remarks>
///     语言文件默认由 <see cref="BuiltinStrings" /> 指向本仓库内置的那一份；
///     因此调用方只需要 <c>Localizer.Get("Core.Download.Failed")</c>，
///     既不需要初始化，也不需要知道 Avalonia 的存在。
/// </remarks>
public static class Localizer
{
    /// <summary>本地化运行时状态（语言、键表、来源）。</summary>
    public static LocalizationContext Current => LocalizationContext.Current;

    /// <summary>当前语言代码。</summary>
    public static string Culture => Current.Culture;

    /// <summary>当前语言对应的 <see cref="System.Globalization.CultureInfo" />。</summary>
    public static CultureInfo CultureInfo => Current.CultureInfo;

    /// <summary>查串，缺失时返回键名。</summary>
    public static string Get(string key, params object?[]? args) => Current.Get(key, args);
}
