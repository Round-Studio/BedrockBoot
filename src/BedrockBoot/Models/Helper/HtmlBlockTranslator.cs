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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace BedrockBoot.Models.Helper;

/// <summary>
///     资源详情的逐条目翻译器：把描述按块级条目（段落、标题、列表项、表格单元格…）
///     拆成独立的小片段逐条送翻，而不是整篇 HTML 一次性翻译。
///     图片、行内代码、代码块等非文本内容不进入翻译载荷，原位保留，
///     避免整篇直译时接口把 img 标签/URL 改坏导致图片显示错误。
/// </summary>
public class HtmlBlockTranslator
{
    /// <summary>整棵子树都不参与翻译，原样保留（代码块、嵌入内容等）。</summary>
    private static readonly HashSet<string> SkippedSubtrees = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "svg", "path", "pre", "iframe", "video", "audio", "canvas",
        "textarea", "noscript", "head", "math"
    };

    /// <summary>夹在文本中间、不参与翻译但要留在原位的行内元素。</summary>
    private static readonly HashSet<string> PreservedInlines = new(StringComparer.OrdinalIgnoreCase)
    {
        "img", "picture", "source", "code", "kbd", "samp", "tt"
    };

    private static readonly Regex HasLetterRegex = new(@"\p{L}", RegexOptions.Compiled);

    /// <summary>
    ///     逐条目翻译内容并返回翻译后的 HTML。
    ///     Markdown 输入会先归一化为 HTML；一条都翻译不成功时返回 null（视为失败）。
    /// </summary>
    /// <param name="content">原始描述（Markdown 或 HTML）</param>
    /// <param name="translateFragment">单条翻译委托，入参出参均为 HTML 片段</param>
    /// <param name="progress">翻译进度回调（已完成条数 / 总条数）</param>
    public static async Task<string?> TranslateAsync(
        string content,
        Func<string, Task<string>> translateFragment,
        Action<int, int>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        var doc = new HtmlDocument();
        doc.LoadHtml(HtmlToControlConverter.NormalizeToHtml(content));

        var units = CollectUnits(doc.DocumentNode);
        if (units.Count == 0) return null;

        var translatedCount = 0;
        for (var i = 0; i < units.Count; i++)
        {
            progress?.Invoke(i, units.Count);
            var unit = units[i];

            try
            {
                var payload = string.Concat(unit.Nodes.Select(n => n.OuterHtml));
                var translated = await translateFragment(payload);

                if (string.IsNullOrWhiteSpace(translated) || translated == payload) continue;
                if (ReplaceUnit(doc, unit, translated)) translatedCount++;
            }
            catch (Exception ex)
            {
                // 单条失败只影响该条，保留原文继续
                Console.WriteLine($@"条目翻译失败，保留原文: {ex.Message}");
            }
        }

        progress?.Invoke(units.Count, units.Count);
        return translatedCount > 0 ? doc.DocumentNode.OuterHtml : null;
    }

    private sealed record Unit(HtmlNode Anchor, List<HtmlNode> Nodes);

    /// <summary>
    ///     遍历文档收集翻译条目。块级元素是条目边界；块级边界之间的连续
    ///     行内节点（文本 + 保留元素的间隔）切成一个条目，图片等保留元素被切分点隔开。
    /// </summary>
    private static List<Unit> CollectUnits(HtmlNode root)
    {
        var units = new List<Unit>();
        Collect(root, units);
        return units;
    }

    private static void Collect(HtmlNode container, List<Unit> units)
    {
        var run = new List<HtmlNode>();

        void Flush()
        {
            if (run.Count > 0 && HasTranslatableText(run))
                units.Add(new Unit(run[0], new List<HtmlNode>(run)));
            run.Clear();
        }

        foreach (var child in container.ChildNodes)
        {
            if (child.NodeType == HtmlNodeType.Comment) continue;

            if (child.NodeType == HtmlNodeType.Text)
            {
                run.Add(child);
                continue;
            }

            if (child.NodeType != HtmlNodeType.Element) continue;
            var name = child.Name.ToLowerInvariant();

            if (SkippedSubtrees.Contains(name) || child.GetAttributeValue("aria-hidden", "") == "true")
            {
                Flush();
                continue;
            }

            if (PreservedInlines.Contains(name))
            {
                // 图片、行内代码：留在原位，并把两侧文本切成独立条目
                Flush();
                continue;
            }

            if (IsBlockElement(name) || HasBlockDescendant(child))
            {
                Flush();
                Collect(child, units);
                continue;
            }

            run.Add(child);
        }

        Flush();
    }

    /// <summary>把条目原文替换为翻译后的 HTML 片段，返回是否发生了替换。</summary>
    private static bool ReplaceUnit(HtmlDocument ownerDoc, Unit unit, string translatedHtml)
    {
        var fragment = new HtmlDocument();
        fragment.LoadHtml(translatedHtml);

        var translatedNodes = fragment.DocumentNode.ChildNodes
            .Where(n => n.NodeType == HtmlNodeType.Element ||
                        (n.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(n.InnerText)))
            .ToList();
        if (translatedNodes.Count == 0) return false;

        // 行内条目里塞回一个块级 <p> 会产生嵌套 <p>，脱掉这层包装
        var parentName = unit.Anchor.ParentNode?.Name.ToLowerInvariant();
        if (parentName is "p" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6" &&
            translatedNodes.Count == 1 && translatedNodes[0].Name.ToLowerInvariant() == "p")
            translatedNodes = translatedNodes[0].ChildNodes
                .Where(n => n.NodeType == HtmlNodeType.Element ||
                            (n.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(n.InnerText)))
                .ToList();

        if (translatedNodes.Count == 0) return false;

        var anchor = unit.Anchor;
        foreach (var node in translatedNodes)
        {
            var imported = ImportNode(ownerDoc, node);
            anchor.ParentNode!.InsertBefore(imported, anchor);
        }

        foreach (var node in unit.Nodes) node.Remove();
        return true;
    }

    /// <summary>把片段文档里的节点复制进目标文档（HtmlAgilityPack 无跨文档导入 API）。</summary>
    private static HtmlNode ImportNode(HtmlDocument ownerDoc, HtmlNode node)
    {
        switch (node.NodeType)
        {
            case HtmlNodeType.Text:
                return ownerDoc.CreateTextNode(((HtmlTextNode)node).Text);
            case HtmlNodeType.Comment:
                return ownerDoc.CreateComment(node.InnerHtml);
            default:
                var element = ownerDoc.CreateElement(node.Name);
                foreach (var attribute in node.Attributes)
                    element.Attributes.Add(attribute.Name, attribute.Value);
                foreach (var child in node.ChildNodes)
                    element.AppendChild(ImportNode(ownerDoc, child));
                return element;
        }
    }

    private static bool HasTranslatableText(List<HtmlNode> nodes) =>
        HasLetterRegex.IsMatch(HtmlEntity.DeEntitize(string.Concat(nodes.Select(n => n.InnerText))));

    private static bool IsBlockElement(string name) => name switch
    {
        "p" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6" or "ul" or "ol" or "li"
            or "table" or "thead" or "tbody" or "tfoot" or "tr" or "td" or "th"
            or "blockquote" or "pre" or "hr" or "div" or "section" or "article" or "aside"
            or "header" or "footer" or "nav" or "main" or "figure" or "figcaption"
            or "details" or "summary" or "dl" or "dt" or "dd" or "center"
            or "markdown-accessiblity-table" or "form" or "fieldset" => true,
        _ => false
    };

    /// <summary>行内元素里包着块级内容（如 a 包 h2）时，按块处理防止整棵子树进翻译载荷。</summary>
    private static bool HasBlockDescendant(HtmlNode node) =>
        node.Descendants().Any(d =>
            d.NodeType == HtmlNodeType.Element && IsBlockElement(d.Name.ToLowerInvariant()));
}
