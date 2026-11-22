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

using System.Reflection;
using System.Xml.Linq;

namespace BedrockBoot.Localization;

/// <summary>一条欢迎语及其出现权重。</summary>
public sealed class WelcomeMessage
{
    /// <param name="text">上屏文本。</param>
    /// <param name="weight">出现权重；小于等于 0 表示停用（临时下线而不删除条目）。</param>
    public WelcomeMessage(string text, int weight)
    {
        Text = text;
        Weight = weight;
    }

    /// <summary>上屏文本。</summary>
    public string Text { get; }

    /// <summary>出现权重；小于等于 0 表示停用。</summary>
    public int Weight { get; }
}

/// <summary>
///     按语言提供主页欢迎语，带权重随机。
/// </summary>
/// <remarks>
///     欢迎语<b>不进主键表</b>：主表要求"键集合与语言无关"，而欢迎语是"每门语言各自一组、
///     还带频率"的集合，塞进 <c>zh_CN.axaml</c> 只能靠键名对齐，既别扭又无法表达权重。
///     因此单独放一个目录，文件格式也换成面向内容的元素形式：
///     <code>
///     &lt;WelcomeMessages&gt;
///         &lt;Message Weight="6"&gt;你好，欢迎回来&lt;/Message&gt;
///         &lt;Message Weight="2"&gt;欢迎回来，今天想玩点什么？&lt;/Message&gt;
///     &lt;/WelcomeMessages&gt;
///     </code>
///     <para>
///         加载方式与 <see cref="ResourceStringModule" /> 一致：读 <c>EmbeddedResource</c> +
///     <see cref="XDocument" />，不碰 <c>avares://</c> 与 Avalonia 的 XAML 引擎。
///     </para>
///     <para>
///         <b>不做跨语言兜底</b>：这里只回答"这门语言有没有自己的欢迎语集合"。
///         某门语言没写欢迎语文件时，调用方应回退到 <c>Home.Title</c> 这条普通键——
///         它本来就有各语言译文。若在这里直接兜底英文，日文用户会看到一句英文欢迎语，
///         比看到自己语言的那一句更糟。
///     </para>
/// </remarks>
public static class WelcomeMessages
{
    /// <summary>欢迎语文件在程序集内的目录。</summary>
    public const string Folder = "Resources/Welcome";

    /// <summary>欢迎语文件的扩展名。</summary>
    public const string FileExtension = ".axaml";

    private const string RootElement = "WelcomeMessages";
    private const string ItemElement = "Message";

    /// <summary>欢迎语所在的程序集（与本程序集的内置文案同处一个包）。</summary>
    public static Assembly Assembly => typeof(WelcomeMessages).Assembly;

    /// <summary>取某门语言自己的欢迎语；没有该语言的文件时返回空列表（不跨语言兜底）。</summary>
    public static IReadOnlyList<WelcomeMessage> Get(string? culture)
    {
        if (string.IsNullOrEmpty(culture))
            return Array.Empty<WelcomeMessage>();

        var list = LoadFrom(Assembly, Folder, culture);
        return list is { Count: > 0 } ? list : Array.Empty<WelcomeMessage>();
    }

    /// <summary>
    ///     按权重随机取一条该语言自己的欢迎语；没有可用条目时返回 <c>null</c>（调用方应回退到键表）。
    /// </summary>
    public static string? Pick(string? culture)
    {
        var list = Get(culture);
        if (list.Count == 0)
            return null;

        var total = 0;
        foreach (var message in list)
            if (message.Weight > 0)
                total += message.Weight;

        // 全部被停用：宁可显示第一条，也不要把主页标题留空。
        if (total <= 0)
            return list[0].Text;

        var roll = Random.Shared.Next(total);
        foreach (var message in list)
        {
            if (message.Weight <= 0)
                continue;

            roll -= message.Weight;
            if (roll < 0)
                return message.Text;
        }

        return list[^1].Text;
    }

    /// <summary>把欢迎语文件解析成条目列表；格式不符时返回空列表。</summary>
    public static List<WelcomeMessage> Parse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var result = new List<WelcomeMessage>();
        var document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        var root = document.Root;

        if (root is null || !string.Equals(root.Name.LocalName, RootElement, StringComparison.OrdinalIgnoreCase))
        {
            LocalizationLog.Warn($"欢迎语文件的根元素应为 {RootElement}，实际为 {root?.Name.LocalName ?? "空"}");
            return result;
        }

        foreach (var element in root.Elements())
        {
            if (!string.Equals(element.Name.LocalName, ItemElement, StringComparison.OrdinalIgnoreCase))
                continue;

            var text = element.Value.Trim();
            if (text.Length == 0)
                continue;

            // 缺省权重为 1：只写文本也能用，想调频率再加 Weight。
            var weight = 1;
            if (int.TryParse(element.Attribute("Weight")?.Value, out var parsed))
                weight = parsed;

            result.Add(new WelcomeMessage(text, weight));
        }

        return result;
    }

    /// <summary>从程序集里读取一门语言的欢迎语；没有该语言文件时返回 <c>null</c>。</summary>
    public static List<WelcomeMessage>? LoadFrom(Assembly assembly, string folder, string culture)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        if (string.IsNullOrWhiteSpace(culture))
            return null;

        try
        {
            var prefix = "." + folder.Replace('/', '.');
            var suffix = $"{prefix}.{culture}{FileExtension}";

            var name = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

            if (name is null)
                return null;

            using var stream = assembly.GetManifestResourceStream(name);
            return stream is null ? null : Parse(stream);
        }
        catch (Exception ex)
        {
            LocalizationLog.Warn($"读取 {culture} 欢迎语失败：{ex.GetType().Name} {ex.Message}");
            return null;
        }
    }
}
