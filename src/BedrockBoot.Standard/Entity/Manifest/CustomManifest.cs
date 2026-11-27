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

namespace BedrockBoot.Standard.Entity.Manifest;

public class CustomManifest
{
    [JsonPropertyName("format_version")]
    public int FormatVersion { get; set; }

    /// <summary>
    ///     自定义主页欢迎语。留空时使用当前语言的内置欢迎语（见 <c>WelcomeMessages</c>，带出现频率）。
    /// </summary>
    /// <remarks>
    ///     这里填的文案<b>原样上屏、不做翻译</b>——它是使用者自己的话，不是产品文案。
    ///     想让默认欢迎语随语言切换，就不要填这个字段。
    /// </remarks>
    [JsonPropertyName("randomStr")] public List<string> RandomStr { get; set; } = new();

    [JsonPropertyName("title")] public string Title { get; set; } = "BedrockBoot {{version}}";

    [JsonPropertyName("pageTitles")] public PageTitles PageTitles { get; set; } = new();

    [JsonPropertyName("isShowHelpBtn")] public bool IsShowHelpBtn { get; set; } = false;

    [JsonPropertyName("helpLinks")] public List<HelpLink> HelpLinks { get; set; } = new();
}

public class PageTitles
{
    /// <summary>
    ///     主页标题模板。<c>{{random}}</c> 会被替换成一条欢迎语；
    ///     写成整句则视为自定义标题，原样显示。
    /// </summary>
    /// <remarks>
    ///     默认值不写死任何中文：「随机串为空」与「没有标题」都由代码兜底到 <c>Home.Title</c> 键，
    ///     这样清单加载失败时主页也不会变成一句未本地化的中文。
    /// </remarks>
    [JsonPropertyName("pageHome")] public string PageHome { get; set; } = "{{random}}";
}

public class HelpLink
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonPropertyName("link")]
    public string Link { get; set; }
}