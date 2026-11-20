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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using BedrockBoot.Views.Control.Widgets;
using HtmlAgilityPack;
using Markdig;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Models.Helper;

/// <summary>
///     把 Markdown / HTML 描述渲染成 Avalonia 控件。
///     Markdown 统一先经 Markdig 转 HTML，再由 HtmlAgilityPack 解析为块级树渲染；
///     段落内的内联内容（粗体、链接、行内代码、行内图片等）合并进同一个
///     TextBlock 的 Inlines，保持正常的文本流与自动换行。
///     所有颜色均引用主题资源，明暗主题自适应。
/// </summary>
public class HtmlToControlConverter
{
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    private static readonly FontFamily CodeFontFamily = new("Cascadia Mono, Consolas, Courier New");

    // 常见图片后缀；描述里出现 .mcaddon / .mcworld 等下载链接时不当图片处理
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".jfif", ".gif", ".webp", ".bmp", ".avif", ".svg", ".ico"
    };

    /// <summary>
    ///     判断链接是否真的是图片。无后缀的地址（常见于 CDN）仍按图片处理。
    /// </summary>
    private static bool IsImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return true;

        var extension = Path.GetExtension(uri.AbsolutePath);
        return string.IsNullOrEmpty(extension) || ImageExtensions.Contains(extension);
    }

    public static List<Control> ConvertHtmlToControls(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new List<Control>();

        return ConvertHtmlOnly(NormalizeToHtml(content));
    }

    /// <summary>
    ///     非完整 HTML 文档的内容（Markdown、HTML 片段）统一转成 HTML；
    ///     完整 HTML 文档原样返回，避免 Markdig 改写。
    /// </summary>
    public static string NormalizeToHtml(string content) =>
        IsFullHtmlDocument(content) ? content : Markdown.ToHtml(content, MarkdownPipeline);

    private static bool IsFullHtmlDocument(string content) =>
        Regex.IsMatch(content, @"^\s*<!DOCTYPE|^\s*<html[\s>]", RegexOptions.IgnoreCase);

    private static List<Control> ConvertHtmlOnly(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // 依次找语义容器；都没命中时退回 body / html / 根节点，
        // 保证网站抓取类（完整文档）与 API 片段都能渲染
        var root = doc.DocumentNode.SelectSingleNode("//article") ??
                   doc.DocumentNode.SelectSingleNode("//div[contains(@class,'markdown-body')]") ??
                   doc.DocumentNode.SelectSingleNode("//div[@class='md']") ??
                   doc.DocumentNode.SelectSingleNode("//body") ??
                   doc.DocumentNode.SelectSingleNode("//html");

        return RenderBlocks((root ?? doc.DocumentNode).ChildNodes);
    }

    #region 块级渲染

    /// <summary>
    ///     把一组节点渲染为块级控件列表。连续的内联节点会被合并成同一个段落 TextBlock，
    ///     而不是像旧实现那样每个内联元素各占一行。
    /// </summary>
    private static List<Control> RenderBlocks(IEnumerable<HtmlNode> nodes, InlineStyle inlineStyle = default)
    {
        var controls = new List<Control>();
        var inlineGroup = new List<HtmlNode>();

        void FlushInlineGroup()
        {
            var paragraph = BuildParagraphControl(inlineGroup, inlineStyle);
            if (paragraph != null) controls.Add(paragraph);
            inlineGroup.Clear();
        }

        foreach (var node in nodes)
        {
            if (node.NodeType == HtmlNodeType.Comment) continue;

            if (node.NodeType == HtmlNodeType.Text)
            {
                // 纯空白文本：只有夹在行内元素之间时才保留（充当分隔空格）
                if (string.IsNullOrWhiteSpace(node.InnerText) && inlineGroup.Count == 0) continue;
                inlineGroup.Add(node);
                continue;
            }

            if (node.NodeType != HtmlNodeType.Element) continue;
            if (IsIgnoredElement(node) || node.GetAttributeValue("aria-hidden", "") == "true") continue;

            var name = node.Name.ToLowerInvariant();
            if (name == "br" || IsInlineElement(name) ||
                // 未知标签（含资源作者的自定义元素）：只有包着块级内容才算块，
                // 否则按行内处理避免把一段连续文本拆成多行
                (!IsBlockElementName(name) && !HasBlockDescendant(node)))
            {
                inlineGroup.Add(node);
                continue;
            }

            FlushInlineGroup();
            var block = RenderBlockElement(node, inlineStyle);
            if (block != null) controls.Add(block);
        }

        FlushInlineGroup();
        return controls;
    }

    private static Control? RenderBlockElement(HtmlNode node, InlineStyle inlineStyle)
    {
        // 块级元素自身的 style（text-align / font-size）先合入样式，再交给内容渲染
        inlineStyle = ApplySpanStyle(node, inlineStyle);
        switch (node.Name.ToLowerInvariant())
        {
            case "p":
                return BuildParagraphControl(node.ChildNodes, inlineStyle);

            case "h1":
            case "h2":
            case "h3":
            case "h4":
            case "h5":
            case "h6":
                return CreateHeading(node, node.Name[1] - '0');

            case "ul":
            case "ol":
                return CreateList(node, node.Name == "ol");

            case "blockquote":
                return CreateQuote(node, inlineStyle);

            case "pre":
                return CreateCodeBlock(node);

            case "table":
                return CreateTable(node, inlineStyle);

            case "hr":
                return CreateDivider();

            case "img":
                return BuildLinkedImage(node);

            case "iframe":
                return CreateVideoCard(node);

            case "center":
                // 居中容器：内容递归后整体居中（文本还要设 TextAlignment，否则 Stretch 宽度下不生效）
                var centered = new StackPanel();
                centered.Children.AddRange(RenderBlocks(node.ChildNodes, inlineStyle));
                if (centered.Children.Count == 0) return null;
                foreach (var child in centered.Children)
                {
                    if (child is TextBlock text) text.TextAlignment = TextAlignment.Center;
                    if (child.HorizontalAlignment == HorizontalAlignment.Stretch)
                        child.HorizontalAlignment = HorizontalAlignment.Center;
                }
                return centered;

            case "figure":
                // figure = 图片/标题组合；figcaption 渲染为下方小字说明
                var figure = new StackPanel { Margin = new Thickness(0, 8) };
                var caption = node.ChildNodes.FirstOrDefault(n =>
                    n.NodeType == HtmlNodeType.Element && n.Name.ToLowerInvariant() == "figcaption");
                if (caption != null) caption.Remove();

                var figureBody = RenderBlocks(node.ChildNodes, inlineStyle);
                if (figureBody.Count == 0 && caption == null) return null;
                figure.Children.AddRange(figureBody);

                if (caption != null)
                {
                    var captionText = new TextBlock
                    {
                        FontSize = 12,
                        Opacity = 0.7,
                        Margin = new Thickness(0, 4),
                        TextWrapping = TextWrapping.Wrap
                    };
                    captionText.Inlines ??= new InlineCollection();
                    BuildInlines(caption.ChildNodes, captionText.Inlines, default);
                    TrimInlineEdges(captionText.Inlines);
                    if (HasVisibleInline(captionText.Inlines)) figure.Children.Add(captionText);
                }

                return figure.Children.Count > 0 ? figure : null;

            case "details":
                // 折叠容器：summary 作标题，其余内容作折叠体（默认展开渲染，不引入交互控件）
                var detailsPanel = new StackPanel { Margin = new Thickness(0, 6) };
                var summary = node.ChildNodes.FirstOrDefault(n =>
                    n.NodeType == HtmlNodeType.Element && n.Name.ToLowerInvariant() == "summary");
                if (summary != null) summary.Remove();

                if (summary != null)
                {
                    var heading = CreateHeading(summary, 4);
                    if (heading != null) detailsPanel.Children.Add(heading);
                }

                detailsPanel.Children.AddRange(RenderBlocks(node.ChildNodes, inlineStyle));
                return detailsPanel.Children.Count > 0 ? detailsPanel : null;

            default:
                // div / section / article / 自定义标签等一律递归内容，保证
                // 完全自定义的 HTML 结构（资源作者的个性化排版）也能完整渲染
                var panel = new StackPanel();
                panel.Children.AddRange(RenderBlocks(node.ChildNodes, inlineStyle));
                return panel.Children.Count > 0 ? panel : null;
        }
    }

    private static Control? BuildParagraphControl(IEnumerable<HtmlNode> nodes, InlineStyle inlineStyle)
    {
        var nodeList = nodes.ToList();

        // 段落里只有图片（或包在 <a> 里的图库）→ 渲染为独立的图片块
        var elements = nodeList.Where(n => n.NodeType == HtmlNodeType.Element).ToList();
        if (elements.Count > 0 && elements.All(IsImageishNode))
        {
            // float:left/right 的图片是网页端装饰元素（侧边装饰条等），桌面端模拟 float
            // 会把布局挤坏，直接忽略；剩下的图片正常纵排
            var images = new StackPanel { Margin = new Thickness(0, 8), Spacing = 8 };
            foreach (var element in elements)
            {
                if (GetFloatSide(element) != null) continue;
                var image = BuildLinkedImage(element);
                if (image != null) images.Children.Add(image);
            }

            return images.Children.Count > 0 ? images : null;
        }

        var textBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6)
        };
        // 块级 style 里的 text-align / font-size 下沉到段落
        if (inlineStyle.Alignment is { } alignment) textBlock.TextAlignment = alignment;
        if (inlineStyle.FontSize is { } paragraphSize) textBlock.FontSize = paragraphSize;
        textBlock.Inlines ??= new InlineCollection();
        BuildInlines(nodeList, textBlock.Inlines, inlineStyle);
        TrimInlineEdges(textBlock.Inlines);

        return HasVisibleInline(textBlock.Inlines) ? textBlock : null;
    }

    private static Control? CreateHeading(HtmlNode node, int level)
    {
        var textBlock = new TextBlock
        {
            FontSize = level switch { 1 => 26, 2 => 22, 3 => 19, 4 => 17, 5 => 15, _ => 14 },
            FontWeight = level <= 2 ? FontWeight.Bold : FontWeight.SemiBold,
            Margin = new Thickness(0, level <= 2 ? 18 : 14, 0, level <= 2 ? 6 : 4),
            TextWrapping = TextWrapping.Wrap
        };
        // 标题自身的 style（text-align:center / font-size:40px）覆盖默认值
        var style = ApplySpanStyle(node, default);
        if (style.Alignment is { } headingAlignment) textBlock.TextAlignment = headingAlignment;
        if (style.FontSize is { } headingSize) textBlock.FontSize = headingSize;
        textBlock.Inlines ??= new InlineCollection();
        BuildInlines(node.ChildNodes, textBlock.Inlines, style);
        TrimInlineEdges(textBlock.Inlines);

        return HasVisibleInline(textBlock.Inlines) ? textBlock : null;
    }

    private static Control? CreateList(HtmlNode listNode, bool isOrdered)
    {
        var depth = GetListDepth(listNode);
        var start = int.TryParse(listNode.GetAttributeValue("start", ""), out var s) ? s : 1;

        var panel = new StackPanel { Margin = new Thickness(0, 4) };

        var index = start;
        foreach (var li in listNode.ChildNodes.Where(n =>
                     n.NodeType == HtmlNodeType.Element && n.Name.ToLowerInvariant() == "li"))
        {
            var content = new StackPanel();
            content.Children.AddRange(RenderBlocks(li.ChildNodes));
            if (content.Children.Count == 0) continue;

            var marker = new TextBlock
            {
                Text = isOrdered ? $"{index++}." : DepthMarker(depth),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 1, 0, 0)
            };
            BindResource(marker, TextBlock.ForegroundProperty, "PrimaryForegroundSecondaryBrush");

            // 标记列 + 内容列：文本换行时悬挂缩进，而不是绕到标记下方
            var item = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("Auto,*"),
                ColumnSpacing = 6
            };
            Grid.SetColumn(marker, 0);
            Grid.SetColumn(content, 1);
            item.Children.Add(marker);
            item.Children.Add(content);

            panel.Children.Add(item);
        }

        return panel.Children.Count > 0 ? panel : null;
    }

    private static Control? CreateQuote(HtmlNode node, InlineStyle inlineStyle)
    {
        var panel = new StackPanel();
        panel.Children.AddRange(RenderBlocks(node.ChildNodes, inlineStyle));
        if (panel.Children.Count == 0) return null;

        var quote = new Border
        {
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(12, 2, 0, 2),
            Margin = new Thickness(0, 10),
            Child = panel
        };
        BindResource(quote, Border.BorderBrushProperty, "AccentBackgroundBrush");
        return quote;
    }

    private static Control? CreateCodeBlock(HtmlNode node)
    {
        var codeNode = node.ChildNodes.FirstOrDefault(n =>
            n.NodeType == HtmlNodeType.Element && n.Name.ToLowerInvariant() == "code");
        var text = HtmlEntity.DeEntitize((codeNode ?? node).InnerText).TrimEnd();
        if (string.IsNullOrWhiteSpace(text)) return null;

        var border = new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 10),
            Margin = new Thickness(0, 10),
            Child = new TextBlock
            {
                Text = text,
                FontFamily = CodeFontFamily,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            }
        };
        BindResource(border, Border.BackgroundProperty, "PrimarySubtleBackgroundBrush");
        return border;
    }

    private static Control? CreateTable(HtmlNode node, InlineStyle inlineStyle)
    {
        // 只取各层的直接子行，避免把嵌套表格的行也算进来
        var rowNodes = new List<HtmlNode>();
        foreach (var section in node.ChildNodes.Where(n =>
                     n.NodeType == HtmlNodeType.Element &&
                     n.Name.ToLowerInvariant() is "thead" or "tbody" or "tfoot"))
            rowNodes.AddRange(section.ChildNodes.Where(n =>
                n.NodeType == HtmlNodeType.Element && n.Name.ToLowerInvariant() == "tr"));

        if (rowNodes.Count == 0)
            rowNodes.AddRange(node.ChildNodes.Where(n =>
                n.NodeType == HtmlNodeType.Element && n.Name.ToLowerInvariant() == "tr"));

        if (rowNodes.Count == 0) return null;

        var grid = new Grid();

        var columnCount = 0;
        for (var i = 0; i < rowNodes.Count; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            var columnIndex = 0;
            var cells = rowNodes[i].ChildNodes.Where(n =>
                n.NodeType == HtmlNodeType.Element && n.Name.ToLowerInvariant() is "th" or "td");

            foreach (var cell in cells)
            {
                var colspan = Math.Max(1, int.TryParse(cell.GetAttributeValue("colspan", ""), out var c) ? c : 1);

                var cellBorder = new Border
                {
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Padding = new Thickness(8, 6)
                };
                BindResource(cellBorder, Border.BorderBrushProperty, "PrimaryBorderBrush");

                var content = new StackPanel();
                content.Children.AddRange(RenderBlocks(cell.ChildNodes,
                    cell.Name.ToLowerInvariant() == "th"
                        ? inlineStyle with { Weight = FontWeight.SemiBold }
                        : default));
                cellBorder.Child = content;

                if (cell.Name.ToLowerInvariant() == "th")
                    BindResource(cellBorder, Border.BackgroundProperty, "PrimarySubtleBackgroundBrush");

                Grid.SetRow(cellBorder, i);
                Grid.SetColumn(cellBorder, columnIndex);
                if (colspan > 1) Grid.SetColumnSpan(cellBorder, colspan);
                grid.Children.Add(cellBorder);

                columnIndex += colspan;
            }

            columnCount = Math.Max(columnCount, columnIndex);
        }

        if (columnCount == 0) return null;
        for (var i = 0; i < columnCount; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        // 外框画上/左两条边，单元格画右下两条边，拼成粗细一致的完整网格
        var table = new Border
        {
            BorderThickness = new Thickness(1, 1, 0, 0),
            CornerRadius = new CornerRadius(6),
            ClipToBounds = true,
            Margin = new Thickness(0, 10),
            Child = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = grid
            }
        };
        BindResource(table, Border.BorderBrushProperty, "PrimaryBorderBrush");
        return table;
    }

    private static Control CreateDivider()
    {
        var divider = new Border
        {
            Height = 1,
            Margin = new Thickness(0, 12)
        };
        BindResource(divider, Border.BackgroundProperty, "PrimaryBorderBrush");
        return divider;
    }

    private static Control? CreateVideoCard(HtmlNode node)
    {
        var src = node.GetAttributeValue("src", "");
        if (string.IsNullOrWhiteSpace(src)) return null;

        Control link = Uri.TryCreate(src, UriKind.Absolute, out var uri)
            ? new HyperlinkButton { Content = src, NavigateUri = uri, Padding = new Thickness(0) }
            : new TextBlock { Text = src, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.7 };

        var card = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 10),
            Margin = new Thickness(0, 10),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Children =
                {
                    new FontIcon { Glyph = "\uE768", VerticalAlignment = VerticalAlignment.Center },
                    link
                }
            }
        };
        BindResource(card, Border.BorderBrushProperty, "PrimaryBorderBrush");
        if (link is HyperlinkButton hyperlink) hyperlink.VerticalAlignment = VerticalAlignment.Center;
        return card;
    }

    #endregion

    #region 图片与链接

    /// <summary>
    ///     渲染图片块；外层带 <c>&lt;a&gt;</c> 时整张图可点击跳转。
    ///     伪装成图片的下载链接（.mcaddon 等）渲染为超链接。
    /// </summary>
    private static Control? BuildLinkedImage(HtmlNode node)
    {
        var img = node.Name.ToLowerInvariant() == "img"
            ? node
            : node.Descendants("img").FirstOrDefault();
        if (img == null) return null;

        var src = img.GetAttributeValue("src", "");
        if (string.IsNullOrWhiteSpace(src)) return null;

        if (!IsImageUrl(src))
        {
            var alt = img.GetAttributeValue("alt", "");
            var href = node.Name.ToLowerInvariant() == "a"
                ? ResolveRelativeLink(node.GetAttributeValue("href", "")) ?? node.GetAttributeValue("href", "")
                : ResolveRelativeLink(src) ?? src;
            if (string.IsNullOrWhiteSpace(href)) return null;
            return CreateLinkButton(string.IsNullOrWhiteSpace(alt) ? href : alt, href);
        }

        var image = CreateFlowImage(img, src, 380);
        var link = node.Name.ToLowerInvariant() == "a"
            ? ResolveRelativeLink(node.GetAttributeValue("href", "")) ?? node.GetAttributeValue("href", "")
            : GetAncestorLink(node);
        if (!string.IsNullOrWhiteSpace(link) && Uri.TryCreate(link, UriKind.Absolute, out var uri))
            return new HyperlinkButton { Content = image, NavigateUri = uri, Padding = new Thickness(0) };

        return image;
    }

    private static string? GetAncestorLink(HtmlNode node)
    {
        for (var parent = node.ParentNode; parent != null; parent = parent.ParentNode)
        {
            if (parent.Name.ToLowerInvariant() != "a") continue;
            var href = parent.GetAttributeValue("href", "");
            return string.IsNullOrWhiteSpace(href) ? null : href;
        }

        return null;
    }

    /// <summary>读 img 的 float 样式（left/right），含包裹元素传递的值；返回非 null 即视为装饰图不渲染。</summary>
    private static string? GetFloatSide(HtmlNode node)
    {
        for (var current = node; current != null; current = current.ParentNode)
        {
            var match = Regex.Match(current.GetAttributeValue("style", ""),
                @"(?:^|;)\s*float\s*:\s*(left|right)", RegexOptions.IgnoreCase);
            if (match.Success) return match.Groups[1].Value.ToLowerInvariant();
            if (current.Name.ToLowerInvariant() == "p") break; // 不越出段落找
        }

        return null;
    }

    private static Control? CreateLinkButton(string content, string href)
    {
        if (!Uri.TryCreate(href, UriKind.Absolute, out var uri)) return null;
        return new HyperlinkButton { Content = content, NavigateUri = uri, Padding = new Thickness(0) };
    }

    /// <summary>段落里是否只剩图片类节点（含包在链接里的图片）。</summary>
    private static bool IsImageishNode(HtmlNode node)
    {
        if (node.NodeType == HtmlNodeType.Text) return string.IsNullOrWhiteSpace(node.InnerText);
        if (node.NodeType != HtmlNodeType.Element) return false;

        var name = node.Name.ToLowerInvariant();
        if (name == "img") return true;
        if (name == "a")
        {
            var children = node.ChildNodes.Where(n =>
                n.NodeType == HtmlNodeType.Element ||
                (n.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(n.InnerText))).ToList();
            return children.Count > 0 && children.All(IsImageishNode);
        }

        return false;
    }

    #endregion

    #region 内联渲染

    /// <summary>
    ///     内联样式沿递归向下合并（嵌套的粗体斜体、带颜色的加粗等都能正确叠加）。
    /// </summary>
    private readonly record struct InlineStyle(
        FontWeight? Weight,
        FontStyle? Slant,
        TextDecorationCollection? Decorations,
        IBrush? Foreground,
        bool IsCode,
        double? FontSize = null,
        TextAlignment? Alignment = null);

    private static void BuildInlines(IEnumerable<HtmlNode> nodes, InlineCollection inlines, InlineStyle style)
    {
        foreach (var node in nodes)
        {
            switch (node.NodeType)
            {
                case HtmlNodeType.Text:
                    AppendText(inlines, node.InnerText, style);
                    break;

                case HtmlNodeType.Element:
                    if (IsIgnoredElement(node) || node.GetAttributeValue("aria-hidden", "") == "true") break;
                    AppendElementInline(node, inlines, style);
                    break;
            }
        }
    }

    private static void AppendElementInline(HtmlNode node, InlineCollection inlines, InlineStyle style)
    {
        switch (node.Name.ToLowerInvariant())
        {
            case "br":
                inlines.Add(new LineBreak());
                break;

            case "strong" or "b":
                BuildInlines(node.ChildNodes, inlines, style with { Weight = FontWeight.Bold });
                break;

            case "em" or "i" or "cite" or "var":
                BuildInlines(node.ChildNodes, inlines, style with { Slant = FontStyle.Italic });
                break;

            case "del" or "s" or "strike":
                BuildInlines(node.ChildNodes, inlines,
                    style with { Decorations = TextDecorations.Strikethrough });
                break;

            case "u" or "ins":
                BuildInlines(node.ChildNodes, inlines, style with { Decorations = TextDecorations.Underline });
                break;

            case "code" or "kbd" or "samp" or "tt":
                AppendText(inlines, node.InnerText, style with { IsCode = true });
                break;

            case "a":
                AppendLinkInline(node, inlines, style);
                break;

            case "img" or "picture" or "source":
                if (node.Name.ToLowerInvariant() == "img") AppendImageInline(node, inlines);
                else BuildInlines(node.ChildNodes, inlines, style);
                break;

            case "sub" or "sup" or "small" or "big" or "mark" or "abbr" or "time" or "bdi" or "bdo"
                or "label" or "button" or "font" or "span":
                BuildInlines(node.ChildNodes, inlines, ApplySpanStyle(node, style));
                break;

            default:
                // 未知标签一律透传内容，保证不丢文本
                BuildInlines(node.ChildNodes, inlines, style);
                break;
        }
    }

    private static void AppendText(InlineCollection inlines, string raw, InlineStyle style)
    {
        // HTML 源码里的缩进换行折叠成单个空格
        var text = Regex.Replace(HtmlEntity.DeEntitize(raw), @"\s+", " ");
        if (text.Length == 0) return;

        var run = new Run(text);
        ApplyInlineStyle(run, style);
        inlines.Add(run);
    }

    private static void ApplyInlineStyle(Run run, InlineStyle style)
    {
        if (style.Weight is { } weight) run.FontWeight = weight;
        if (style.Slant is { } slant) run.FontStyle = slant;
        if (style.Decorations is { } decorations) run.TextDecorations = decorations;
        if (style.Foreground is { } foreground) run.Foreground = foreground;
        if (style.FontSize is { } fontSize) run.FontSize = fontSize;

        if (!style.IsCode) return;

        run.FontFamily = CodeFontFamily;
        if (Application.Current?.Resources is { } resources)
            run.Bind(Run.BackgroundProperty, resources.GetResourceObservable("PrimarySubtleBackgroundBrush"));
    }

    private static void AppendLinkInline(HtmlNode node, InlineCollection inlines, InlineStyle style)
    {
        var href = node.GetAttributeValue("href", "");
        // 页内锚点或空链接：只渲染文字不渲染可点击链接
        if (string.IsNullOrWhiteSpace(href) || href.StartsWith("#"))
        {
            BuildInlines(node.ChildNodes, inlines, style);
            return;
        }

        // 资源站外链代理（CurseForge /linkout?remoteUrl=...）还原出真实地址；
        // 其余相对链接挂在站点根上解析
        if (!Uri.TryCreate(href, UriKind.Absolute, out var uri))
        {
            var resolved = ResolveRelativeLink(href);
            if (resolved == null || !Uri.TryCreate(resolved, UriKind.Absolute, out uri))
            {
                BuildInlines(node.ChildNodes, inlines, style);
                return;
            }
        }

        // 链接内容单独构建 Inlines，保留嵌套的粗体/颜色等格式
        var content = new TextBlock { TextWrapping = TextWrapping.Wrap };
        content.Inlines ??= new InlineCollection();
        BuildInlines(node.ChildNodes, content.Inlines, style);
        if (content.Inlines.Count == 0) content.Text = href;

        var button = new HyperlinkButton
        {
            Content = content,
            NavigateUri = uri,
            Padding = new Thickness(0),
            MinHeight = 0,
            MinWidth = 0,
            Background = Brushes.Transparent
        };
        inlines.Add(new InlineUIContainer(button));
    }

    /// <summary>还原资源站的外链代理与相对地址（对 absolute 地址直接透传）。</summary>
    private static string? ResolveRelativeLink(string href)
    {
        // /linkout?remoteUrl=<双重 URL 编码的真实地址>
        var match = Regex.Match(href, @"(?:^|/)linkout\?remoteUrl=([^&]+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            // 剥一层百分号编码后得到可解析的绝对地址
            var candidate = Uri.UnescapeDataString(match.Groups[1].Value);
            if (Uri.TryCreate(candidate, UriKind.Absolute, out _)) return candidate;
            return Uri.UnescapeDataString(candidate);
        }

        // CurseForge 相对地址统一挂在主站上
        return href.StartsWith('/') ? $"https://www.curseforge.com{href}" : null;
    }

    private static void AppendImageInline(HtmlNode node, InlineCollection inlines)
    {
        // float 装饰图统一忽略（桌面端无法还原环绕布局，渲染出来只会挤坏排版）
        if (GetFloatSide(node) != null) return;

        var src = node.GetAttributeValue("src", "");
        if (string.IsNullOrWhiteSpace(src)) return;

        if (!IsImageUrl(src))
        {
            var alt = node.GetAttributeValue("alt", "");
            var href = ResolveRelativeLink(src) ?? src;
            var link = CreateLinkButton(string.IsNullOrWhiteSpace(alt) ? href : alt, href);
            if (link != null) inlines.Add(new InlineUIContainer(link));
            return;
        }

        inlines.Add(new InlineUIContainer(CreateFlowImage(node, src, 220)));
    }

    /// <summary>按 img 的 style="width:N%" 限制图片显示宽度（CurseForge 描述的横幅图常用）。</summary>
    private static FlowImage CreateFlowImage(HtmlNode node, string src, double maxDisplayHeight)
    {
        var image = new FlowImage(src) { MaxDisplayHeight = maxDisplayHeight };

        var styleAttr = node.GetAttributeValue("style", "");
        var widthMatch = Regex.Match(styleAttr, @"(?:^|;)\s*width\s*:\s*(\d+(?:\.\d+)?)%");
        if (widthMatch.Success)
            image.MaxDisplayWidthPercent = double.Parse(widthMatch.Groups[1].Value);

        // HTML 里写了固定 width/height 的窄长图（装饰条、分隔柱）按作者意图保持原始尺寸，
        // 不参与小图放大，避免被撑到模糊失真
        var htmlWidth = node.GetAttributeValue("width", "");
        var htmlHeight = node.GetAttributeValue("height", "");
        if (int.TryParse(htmlWidth, out var w) && int.TryParse(htmlHeight, out var h) &&
            Math.Max(w, h) / (double)Math.Max(1, Math.Min(w, h)) > 3)
            image.KeepNaturalSize = true;

        return image;
    }

    /// <summary>
    ///     解析行内样式：颜色、字号、下划线/删除线与块级 text-align 都能叠加。
    ///     CurseForge 描述大量用 span style 做彩色文字与字号微调。
    /// </summary>
    private static InlineStyle ApplySpanStyle(HtmlNode node, InlineStyle style)
    {
        var styleAttr = node.GetAttributeValue("style", "");

        if (Regex.Match(styleAttr, @"(?:^|;)\s*color\s*:\s*(#[0-9a-fA-F]{3,8}|[a-zA-Z]+)") is { Success: true } colorMatch &&
            TryParseColor(colorMatch.Groups[1].Value, out var color))
            style = style with { Foreground = new SolidColorBrush(color) };

        if (Regex.Match(styleAttr, @"(?:^|;)\s*font-size\s*:\s*(\d+(?:\.\d+)?)\s*px") is { Success: true } sizeMatch)
            style = style with { FontSize = double.Parse(sizeMatch.Groups[1].Value) };

        if (Regex.Match(styleAttr, @"(?:^|;)\s*text-decoration\s*:\s*([a-z-]+)") is { Success: true } decoMatch)
            style = style with
            {
                Decorations = decoMatch.Groups[1].Value switch
                {
                    "underline" => TextDecorations.Underline,
                    "line-through" => TextDecorations.Strikethrough,
                    _ => style.Decorations
                }
            };

        // text-align 只对块级容器有意义，记进样式供块渲染时读取
        if (Regex.Match(styleAttr, @"(?:^|;)\s*text-align\s*:\s*(\w+)") is { Success: true } alignMatch)
            style = style with { Alignment = ParseAlignment(alignMatch.Groups[1].Value) };

        var colorAttr = node.Attributes["color"]?.Value;
        if (!string.IsNullOrWhiteSpace(colorAttr) && TryParseColor(colorAttr, out var fontColor))
            style = style with { Foreground = new SolidColorBrush(fontColor) };

        return style;
    }

    private static TextAlignment ParseAlignment(string value) => value switch
    {
        "center" => TextAlignment.Center,
        "right" or "end" => TextAlignment.Right,
        "justify" => TextAlignment.Justify,
        _ => TextAlignment.Left
    };

    private static bool TryParseColor(string value, out Color color)
    {
        try
        {
            color = Color.Parse(value.Trim());
            return true;
        }
        catch
        {
            color = default;
            return false;
        }
    }

    #endregion

    #region 工具方法

    /// <summary>让控件属性跟随主题资源变化（代码版的 DynamicResource）。</summary>
    private static void BindResource(AvaloniaObject target, AvaloniaProperty property, string key)
    {
        if (Application.Current?.Resources is { } resources)
            target.Bind(property, resources.GetResourceObservable(key));
    }

    private static bool HasVisibleInline(InlineCollection inlines)
    {
        return inlines.Any(inline =>
            inline is InlineUIContainer ||
            (inline is Run run && !string.IsNullOrWhiteSpace(run.Text)));
    }

    /// <summary>去掉段落首尾的空白 Run 与换行，让文本从边缘开始。</summary>
    private static void TrimInlineEdges(InlineCollection inlines)
    {
        while (inlines.Count > 0)
        {
            switch (inlines[0])
            {
                case Run run:
                    var trimmedStart = run.Text?.TrimStart();
                    if (string.IsNullOrEmpty(trimmedStart))
                    {
                        inlines.RemoveAt(0);
                        continue;
                    }

                    run.Text = trimmedStart;
                    break;

                case LineBreak:
                    inlines.RemoveAt(0);
                    continue;
            }

            break;
        }

        while (inlines.Count > 0)
        {
            var lastIndex = inlines.Count - 1;
            switch (inlines[lastIndex])
            {
                case Run run:
                    var trimmedEnd = run.Text?.TrimEnd();
                    if (string.IsNullOrEmpty(trimmedEnd))
                    {
                        inlines.RemoveAt(lastIndex);
                        continue;
                    }

                    run.Text = trimmedEnd;
                    break;

                case LineBreak:
                    inlines.RemoveAt(lastIndex);
                    continue;
            }

            break;
        }
    }

    private static bool IsInlineElement(string name) => name switch
    {
        "a" or "span" or "font" or "strong" or "b" or "em" or "i" or "u" or "s" or "del"
            or "strike" or "ins" or "code" or "kbd" or "samp" or "tt" or "small" or "big"
            or "sub" or "sup" or "mark" or "abbr" or "cite" or "q" or "time" or "bdi"
            or "bdo" or "label" or "picture" or "source" or "button" => true,
        _ => false
    };

    /// <summary>已知块级标签集合（RenderBlockElement 能给出专门渲染的都算）。</summary>
    private static bool IsBlockElementName(string name) => name switch
    {
        "p" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6" or "ul" or "ol" or "li"
            or "table" or "thead" or "tbody" or "tfoot" or "tr" or "td" or "th"
            or "blockquote" or "pre" or "hr" or "div" or "section" or "article" or "aside"
            or "header" or "footer" or "nav" or "main" or "figure" or "figcaption"
            or "details" or "summary" or "dl" or "dt" or "dd" or "center"
            or "form" or "fieldset" or "iframe" or "img" or "body" or "html" => true,
        _ => false
    };

    /// <summary>行内元素里包着块级内容（如 a 包 div>img）时，按块处理防止整棵子树进段落。</summary>
    private static bool HasBlockDescendant(HtmlNode node)
    {
        foreach (var d in node.Descendants())
            if (d.NodeType == HtmlNodeType.Element && IsBlockElementName(d.Name.ToLowerInvariant()))
                return true;
        return false;
    }

    private static bool IsIgnoredElement(HtmlNode node)
    {
        switch (node.Name.ToLowerInvariant())
        {
            case "svg" or "path" or "script" or "style" or "head" or "title" or "meta" or "link"
                 or "noscript" or "input" or "select" or "option" or "textarea" or "col" or "colgroup":
                return true;
            default:
                return false;
        }
    }

    private static int GetListDepth(HtmlNode listNode)
    {
        var depth = 0;
        for (var parent = listNode.ParentNode; parent != null; parent = parent.ParentNode)
            if (parent.Name.ToLowerInvariant() is "ul" or "ol")
                depth++;
        return depth;
    }

    private static string DepthMarker(int depth) => depth switch
    {
        0 => "\u2022",  // •
        1 => "\u25E6",  // ◦
        _ => "\u25AA"   // ▪
    };

    #endregion
}
