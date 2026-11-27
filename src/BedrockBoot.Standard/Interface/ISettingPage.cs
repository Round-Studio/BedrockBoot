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

using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Standard.Interface;

public class ISettingPage : ISetting
{
    /// <summary>
    ///     面包屑条目。
    /// </summary>
    /// <remarks>
    ///     <see cref="BreadcrumbItemInfo.ItemName" /> 存的是<b>本地化键</b>（如
    ///     <c>"Setting.Language.Title"</c>），不是当前语言下的文案——页面构造时并不知道
    ///     用户之后会不会切语言，只有存键才能由 <c>MainSettingPage</c> 在每次渲染（含切语言）时
    ///     重新解析。少数非文案的条目（用户名、产品名）直接写原样字符串即可：
    ///     键表里没有这个键，查串会原样返回。
    /// </remarks>
    public List<BreadcrumbItemInfo> BreadcrumbItem { get; set; } = new();
    public bool IsEdit { get; set; } = false;
}