using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Views.Control.Widgets;
using HtmlAgilityPack;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace BedrockBoot.Models.Helper;

public class HtmlToControlConverter
{
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    public static List<Control> ConvertHtmlToControls(string content)
    {
        var controls = new List<Control>();

        if (string.IsNullOrWhiteSpace(content))
            return controls;

        // 检测内容类型并分别处理
        if (IsMarkdown(content))
        {
            // 如果是 Markdown，先转换为 HTML 再解析
            var html = Markdown.ToHtml(content, MarkdownPipeline);
            return ConvertHtmlOnly(html);
        }
        else
        {
            // 如果是纯 HTML
            return ConvertHtmlOnly(content);
        }
    }

    /// <summary>
    /// 转换混合内容（HTML + Markdown）
    /// </summary>
    public static List<Control> ConvertMixedContent(string content)
    {
        var controls = new List<Control>();

        if (string.IsNullOrWhiteSpace(content))
            return controls;

        // 先尝试将整个内容当作 Markdown 处理
        // 如果包含 HTML 标签，Markdig 会自动处理混合内容
        var html = Markdown.ToHtml(content, MarkdownPipeline);
        return ConvertHtmlOnly(html);
    }

    /// <summary>
    /// 检测内容是否为 Markdown 格式
    /// </summary>
    private static bool IsMarkdown(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        // 检查是否包含 HTML 标签
        if (Regex.IsMatch(content, @"<\s*[a-zA-Z][^>]*>"))
            return false;

        // 检查 Markdown 特征
        var markdownPatterns = new[]
        {
            @"^#{1,6}\s",           // 标题
            @"^\s*[-*+]\s",         // 无序列表
            @"^\s*\d+\.\s",         // 有序列表
            @"\*\*.*?\*\*",         // 粗体
            @"\*.*?\*",             // 斜体
            @"`.*?`",               // 行内代码
            @"```[\s\S]*?```",      // 代码块
            @"\[.*?\]\(.*?\)",      // 链接
            @"!\[.*?\]\(.*?\)",     // 图片
            @"^>\s",                // 引用
            @"^---",                // 分隔线
        };

        return markdownPatterns.Any(pattern => Regex.IsMatch(content, pattern, RegexOptions.Multiline));
    }

    private static List<Control> ConvertHtmlOnly(string html)
    {
        var controls = new List<Control>();

        if (string.IsNullOrWhiteSpace(html))
            return controls;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // 找到 article 或 div 容器
        var articleNode = doc.DocumentNode.SelectSingleNode("//article") ??
                          doc.DocumentNode.SelectSingleNode("//div[@class='md']");

        if (articleNode != null)
            ProcessNodes(articleNode.ChildNodes, controls);
        else
            ProcessNodes(doc.DocumentNode.ChildNodes, controls);

        return controls;
    }

    private static Control ConvertMarkdownBlockToControl(MarkdownObject block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                return CreateMarkdownHeading(heading);
            
            case ParagraphBlock paragraph:
                return CreateMarkdownParagraph(paragraph);
            
            case ListBlock list:
                return CreateMarkdownList(list);
            
            case CodeBlock codeBlock:
                return CreateCodeBlock(codeBlock);
            
            case QuoteBlock quoteBlock:
                return CreateQuoteBlock(quoteBlock);
            
            case ThematicBreakBlock:
                return CreateDivider();
            
            default:
                return null;
        }
    }

    private static Control CreateMarkdownHeading(HeadingBlock heading)
    {
        var text = ExtractInlineText(heading.Inline);
        
        var fontSize = heading.Level switch
        {
            1 => 28,
            2 => 24,
            3 => 20,
            4 => 18,
            5 => 16,
            6 => 14,
            _ => 20
        };

        var fontWeight = heading.Level <= 2 ? FontWeight.Bold : FontWeight.SemiBold;

        return new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = fontWeight,
            Margin = new Thickness(0, 16, 0, 8),
            TextWrapping = TextWrapping.Wrap
        };
    }

    private static Control CreateMarkdownParagraph(ParagraphBlock paragraph)
    {
        if (paragraph.Inline == null)
            return new Border { Height = 6 };

        var panel = new StackPanel
        {
            Spacing = 4,
            Margin = new Thickness(0, 6)
        };

        ProcessMarkdownInlines(paragraph.Inline, panel);

        if (panel.Children.Count == 0)
        {
            var text = ExtractInlineText(paragraph.Inline);
            if (!string.IsNullOrWhiteSpace(text))
                panel.Children.Add(new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap
                });
        }

        return panel;
    }

    private static void ProcessMarkdownInlines(ContainerInline container, StackPanel panel)
    {
        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    if (!string.IsNullOrWhiteSpace(literal.Content.ToString()))
                    {
                        panel.Children.Add(new TextBlock
                        {
                            Text = literal.Content.ToString(),
                            TextWrapping = TextWrapping.Wrap
                        });
                    }
                    break;

                case EmphasisInline emphasis:
                    var text = ExtractInlineText(emphasis);
                    var fontWeight = emphasis.DelimiterCount == 2 ? FontWeight.Bold : FontWeight.Normal;
                    var fontStyle = emphasis.DelimiterCount == 1 ? FontStyle.Italic : FontStyle.Normal;
                    
                    var textBlock = new TextBlock
                    {
                        Text = text,
                        FontWeight = fontWeight,
                        FontStyle = fontStyle,
                        TextWrapping = TextWrapping.Wrap
                    };

                    // 处理删除线
                    if (emphasis.DelimiterChar == '~' && emphasis.DelimiterCount == 2)
                    {
                        textBlock.TextDecorations = TextDecorations.Strikethrough;
                    }

                    panel.Children.Add(textBlock);
                    break;

                case LinkInline link:
                    var linkText = ExtractInlineText(link);
                    var url = link.Url;
                    
                    if (link.IsImage)
                    {
                        // 处理图片
                        panel.Children.Add(new LocalImageRenderWidget(url)
                        {
                            Margin = new Thickness(0, 8),
                            Width = 400,
                            Height = 225
                        });
                    }
                    else
                    {
                        var hyperlinkButton = new HyperlinkButton
                        {
                            Content = string.IsNullOrWhiteSpace(linkText) ? url : linkText,
                            Padding = new Thickness(0),
                            Margin = new Thickness(0, 2),
                            Background = new SolidColorBrush(Colors.Transparent)
                        };

                        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                            hyperlinkButton.NavigateUri = uri;

                        panel.Children.Add(hyperlinkButton);
                    }
                    break;

                case CodeInline codeInline:
                    panel.Children.Add(new TextBlock
                    {
                        Text = codeInline.Content,
                        FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                        Background = new SolidColorBrush(Color.Parse("#F5F5F5")),
                        Padding = new Thickness(4, 2),
                        TextWrapping = TextWrapping.Wrap
                    });
                    break;

                case LineBreakInline:
                    panel.Children.Add(new Border { Height = 8 });
                    break;
            }
        }
    }

    private static Control CreateMarkdownList(ListBlock list)
    {
        var listPanel = new StackPanel
        {
            Spacing = 4,
            Margin = new Thickness(20, 8, 0, 8)
        };

        var index = 1;
        foreach (var item in list)
        {
            if (item is ListItemBlock listItem)
            {
                var itemPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8
                };

                var prefix = list.IsOrdered ? $"{index}. " : "• ";
                
                itemPanel.Children.Add(new TextBlock
                {
                    Text = prefix,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 2, 0, 0)
                });

                // 处理列表项的段落内容
                foreach (var block in listItem)
                {
                    if (block is ParagraphBlock paragraph)
                    {
                        var contentPanel = new StackPanel();
                        ProcessMarkdownInlines(paragraph.Inline, contentPanel);
                        
                        if (contentPanel.Children.Count == 0)
                        {
                            var text = ExtractInlineText(paragraph.Inline);
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                contentPanel.Children.Add(new TextBlock
                                {
                                    Text = text,
                                    TextWrapping = TextWrapping.Wrap,
                                    VerticalAlignment = VerticalAlignment.Top,
                                    Margin = new Thickness(0, 2, 0, 0)
                                });
                            }
                        }
                        
                        itemPanel.Children.Add(contentPanel);
                    }
                }

                listPanel.Children.Add(itemPanel);
                index++;
            }
        }

        return listPanel;
    }

    private static Control CreateCodeBlock(CodeBlock codeBlock)
    {
        var codeText = codeBlock.Lines.ToString();
        var language = (codeBlock is FencedCodeBlock fencedCode) ? fencedCode.Info : "";

        var stackPanel = new StackPanel
        {
            Margin = new Thickness(0, 12),
            Spacing = 4
        };

        // 语言标签（如果有）
        if (!string.IsNullOrWhiteSpace(language))
        {
            stackPanel.Children.Add(new TextBlock
            {
                Text = language,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#666666")),
                Margin = new Thickness(4, 0)
            });
        }

        // 代码内容
        var codeBorder = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#F5F5F5")),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12),
            Child = new TextBlock
            {
                Text = codeText.TrimEnd(),
                FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            }
        };

        stackPanel.Children.Add(codeBorder);
        return stackPanel;
    }

    private static Control CreateQuoteBlock(QuoteBlock quoteBlock)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(0, 12),
            Spacing = 6
        };

        foreach (var block in quoteBlock)
        {
            if (block is ParagraphBlock paragraph)
            {
                var text = ExtractInlineText(paragraph.Inline);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var quoteBorder = new Border
                    {
                        BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC")),
                        BorderThickness = new Thickness(4, 0, 0, 0),
                        Padding = new Thickness(12, 4),
                        Child = new TextBlock
                        {
                            Text = text,
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = new SolidColorBrush(Color.Parse("#666666")),
                            FontStyle = FontStyle.Italic
                        }
                    };
                    panel.Children.Add(quoteBorder);
                }
            }
        }

        return panel;
    }

    private static string ExtractInlineText(ContainerInline container)
    {
        if (container == null)
            return string.Empty;

        var textBuilder = new StringBuilder();
        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    textBuilder.Append(literal.Content.ToString());
                    break;
                case EmphasisInline emphasis:
                    textBuilder.Append(ExtractInlineText(emphasis));
                    break;
                case LinkInline link:
                    textBuilder.Append(ExtractInlineText(link));
                    break;
                case CodeInline codeInline:
                    textBuilder.Append(codeInline.Content);
                    break;
            }
        }

        return textBuilder.ToString().Trim();
    }

    // 下面是原有的 HTML 处理方法...

    // 修改 GetInnerTextWithoutAnchor 方法
    private static string GetInnerTextWithoutAnchor(HtmlNode node)
    {
        if (node == null)
            return string.Empty;

        var clone = node.CloneNode(true);

        var anchors = clone.SelectNodes(".//a[@href]");
        if (anchors != null)
        {
            foreach (var anchor in anchors)
            {
                var href = anchor.GetAttributeValue("href", "");
                if (href.StartsWith("#"))
                {
                    anchor.Remove();
                }
            }
        }

        return GetInnerText(clone);
    }

    private static string GetInnerText(HtmlNode node)
    {
        if (node == null)
            return string.Empty;

        if (node.Name == "svg" || node.Name == "path")
            return string.Empty;

        if (node.Name == "a")
        {
            var href = node.GetAttributeValue("href", "");
            if (href.StartsWith("#"))
                return string.Empty;
        }

        if (node.ChildNodes.All(n => n.NodeType == HtmlNodeType.Text || n.Name == "svg" || n.Name == "path"))
        {
            var text = HtmlEntity.DeEntitize(node.InnerText).Trim();
            text = Regex.Replace(text, @"^#+\s*", "");
            return Regex.Replace(text, @"\s+", " ").Trim();
        }

        var textBuilder = new StringBuilder();
        foreach (var child in node.ChildNodes)
        {
            if (child.Name == "svg" || child.Name == "path")
                continue;

            if (child.Name == "a")
            {
                var href = child.GetAttributeValue("href", "");
                if (href.StartsWith("#"))
                    continue;
            }

            if (child.NodeType == HtmlNodeType.Text)
            {
                var text = HtmlEntity.DeEntitize(child.InnerText);
                textBuilder.Append(text);
            }
            else if (child.NodeType == HtmlNodeType.Element)
            {
                var childText = GetInnerText(child);
                if (!string.IsNullOrWhiteSpace(childText))
                {
                    textBuilder.Append(childText);
                    textBuilder.Append(" ");
                }
            }
        }

        var result = Regex.Replace(textBuilder.ToString(), @"\s+", " ").Trim();
        result = Regex.Replace(result, @"^#+\s*", "");
        return result;
    }

    private static void ProcessNodes(HtmlNodeCollection nodes, List<Control> controls)
    {
        if (nodes == null || nodes.Count == 0)
            return;

        foreach (var node in nodes)
        {
            try
            {
                if (node.Name == "svg" || node.Name == "path" ||
                    node.GetAttributeValue("aria-hidden", "") == "true")
                    continue;

                if (node.Name == "a")
                {
                    var href = node.GetAttributeValue("href", "");
                    if (href.StartsWith("#"))
                        continue;
                }

                var nodeControls = ConvertNodeToControls(node);
                if (nodeControls != null && nodeControls.Count > 0)
                    controls.AddRange(nodeControls);
            }
            catch
            {
                // 忽略转换错误
            }
        }
    }

    private static List<Control> ConvertNodeToControls(HtmlNode node)
    {
        var controls = new List<Control>();

        if (node == null)
            return controls;

        if (node.Name == "markdown-accessiblity-table")
        {
            var tableControl = CreateTable(node);
            if (tableControl != null)
                controls.Add(tableControl);
            return controls;
        }

        switch (node.NodeType)
        {
            case HtmlNodeType.Text:
                var text = HtmlEntity.DeEntitize(node.InnerText).Trim();
                if (!string.IsNullOrWhiteSpace(text) && text != "&nbsp;" && text != " ")
                    controls.Add(new TextBlock
                    {
                        Text = text,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 2)
                    });
                break;

            case HtmlNodeType.Element:
                var control = node.Name.ToLower() switch
                {
                    "h1" => CreateHeading(node, 28, FontWeight.Bold),
                    "h2" => CreateHeading(node, 24, FontWeight.Bold),
                    "h3" => CreateHeading(node, 20, FontWeight.SemiBold),
                    "h4" => CreateHeading(node, 18, FontWeight.SemiBold),
                    "p" => CreateParagraph(node),
                    "ul" => CreateList(node, false),
                    "ol" => CreateList(node, true),
                    "strong" or "b" => CreateStyledText(node, FontWeight.Bold),
                    "em" or "i" => CreateStyledText(node, FontWeight.Normal, FontStyle.Italic),
                    "u" => CreateUnderlinedText(node),
                    "a" => CreateHyperlink(node),
                    "img" => CreateImage(node),
                    "br" => new Border { Height = 8 },
                    "hr" => CreateDivider(),
                    "iframe" => CreateVideoPlaceholder(node),
                    "span" => CreateSpan(node),
                    "div" => null,
                    "table" => CreateTable(node),
                    "thead" or "tbody" => null,
                    "tr" => null,
                    "th" or "td" => null,
                    _ => null
                };

                if (control != null)
                {
                    controls.Add(control);
                }
                else if (node.Name.ToLower() == "div" && node.HasChildNodes)
                {
                    ProcessNodes(node.ChildNodes, controls);
                }
                else if (node.HasChildNodes && node.Name.ToLower() != "svg")
                {
                    ProcessNodes(node.ChildNodes, controls);
                }

                break;
        }

        return controls;
    }

    private static Control CreateTable(HtmlNode node)
    {
        var grid = new Grid();
        var rows = new List<List<Control>>();
        var columnCount = 0;

        var tableNode = node.Name == "table" ? node : node.SelectSingleNode(".//table");
        if (tableNode == null)
            return null;

        var rowsList = new List<HtmlNode>();

        var thead = tableNode.SelectSingleNode(".//thead");
        if (thead != null)
        {
            rowsList.AddRange(thead.SelectNodes(".//tr") ?? new HtmlNodeCollection(null));
        }

        var tbody = tableNode.SelectSingleNode(".//tbody");
        if (tbody != null)
        {
            rowsList.AddRange(tbody.SelectNodes(".//tr") ?? new HtmlNodeCollection(null));
        }

        if (rowsList.Count == 0)
        {
            rowsList.AddRange(tableNode.SelectNodes(".//tr") ?? new HtmlNodeCollection(null));
        }

        foreach (var row in rowsList)
        {
            var cells = new List<Control>();
            var headers = row.SelectNodes("th");
            var dataCells = row.SelectNodes("td");

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    var text = GetInnerText(header);
                    var textBlock = new TextBlock
                    {
                        Text = text,
                        FontWeight = FontWeight.Bold,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(8, 6),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    cells.Add(textBlock);
                }
            }

            if (dataCells != null)
            {
                foreach (var cell in dataCells)
                {
                    var text = GetInnerText(cell);
                    var textBlock = new TextBlock
                    {
                        Text = text,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(8, 6),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    cells.Add(textBlock);
                }
            }

            if (cells.Count > 0)
            {
                rows.Add(cells);
                columnCount = Math.Max(columnCount, cells.Count);
            }
        }

        if (rows.Count == 0)
            return null;

        for (int i = 0; i < columnCount; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        }

        for (int i = 0; i < rows.Count; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            for (int j = 0; j < rows[i].Count; j++)
            {
                var cell = rows[i][j];
                Grid.SetRow(cell, i);
                Grid.SetColumn(cell, j);
                grid.Children.Add(cell);
            }
        }

        var border = new Border
        {
            Child = grid,
            BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0")),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 12, 0, 12),
            CornerRadius = new CornerRadius(4)
        };

        return border;
    }

    private static Control CreateDivider()
    {
        return new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.Parse("#E0E0E0")),
            Margin = new Thickness(0, 12)
        };
    }

    private static Control CreateHeading(HtmlNode node, int fontSize, FontWeight fontWeight)
    {
        var text = GetInnerTextWithoutAnchor(node);

        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = fontWeight,
            Margin = new Thickness(0, 16, 0, 8),
            TextWrapping = TextWrapping.Wrap
        };

        ApplyColor(node, textBlock);
        ApplyTextDecoration(node, textBlock);

        return textBlock;
    }

    private static Control CreateParagraph(HtmlNode node)
    {
        if (node == null || !node.HasChildNodes)
            return new Border { Height = 6 };

        var hasComplexContent = node.ChildNodes.Any(n =>
            n.NodeType == HtmlNodeType.Element &&
            n.Name.ToLower() is "img" or "a" or "strong" or "em" or "span" or "b" or "i" or "u" or "iframe" or "code");

        if (hasComplexContent)
        {
            var panel = new StackPanel
            {
                Spacing = 4,
                Margin = new Thickness(0, 6)
            };

            var inlineControls = new List<Control>();
            ProcessNodes(node.ChildNodes, inlineControls);

            foreach (var control in inlineControls)
            {
                if (control != null)
                    panel.Children.Add(control);
            }

            if (panel.Children.Count == 0)
            {
                var text = GetInnerText(node);
                if (!string.IsNullOrWhiteSpace(text))
                    panel.Children.Add(new TextBlock
                    {
                        Text = text,
                        TextWrapping = TextWrapping.Wrap
                    });
            }

            return panel;
        }

        var plainText = GetInnerText(node);
        if (string.IsNullOrWhiteSpace(plainText))
            return new Border { Height = 6 };

        var textBlock = new TextBlock
        {
            Text = plainText,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6)
        };

        ApplyColor(node, textBlock);
        return textBlock;
    }

    private static Control CreateList(HtmlNode node, bool isOrdered)
    {
        var listPanel = new StackPanel
        {
            Spacing = 4,
            Margin = new Thickness(20, 8, 0, 8)
        };

        var items = node.SelectNodes(".//li");
        if (items == null)
            return listPanel;

        var index = 1;
        foreach (var li in items)
        {
            var itemPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            var prefix = isOrdered ? $"{index}. " : "• ";

            var prefixBlock = new TextBlock
            {
                Text = prefix,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 0, 0)
            };

            itemPanel.Children.Add(prefixBlock);

            var hasComplexContent = li.ChildNodes.Any(n =>
                n.Name.ToLower() is "a" or "strong" or "em");

            if (hasComplexContent)
            {
                var contentPanel = new StackPanel();
                ProcessNodes(li.ChildNodes, new List<Control>());

                var contentControls = new List<Control>();
                ProcessNodes(li.ChildNodes, contentControls);

                foreach (var control in contentControls)
                {
                    contentPanel.Children.Add(control);
                }

                itemPanel.Children.Add(contentPanel);
            }
            else
            {
                var text = GetInnerText(li);
                itemPanel.Children.Add(new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }

            listPanel.Children.Add(itemPanel);
            index++;
        }

        return listPanel;
    }

    private static Control CreateStyledText(HtmlNode node, FontWeight fontWeight,
        FontStyle fontStyle = FontStyle.Normal)
    {
        var text = GetInnerText(node);
        var textBlock = new TextBlock
        {
            Text = text,
            FontWeight = fontWeight,
            FontStyle = fontStyle,
            TextWrapping = TextWrapping.Wrap
        };

        ApplyColor(node, textBlock);
        return textBlock;
    }

    private static Control CreateUnderlinedText(HtmlNode node)
    {
        var text = GetInnerText(node);
        var textBlock = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            TextDecorations = TextDecorations.Underline
        };

        ApplyColor(node, textBlock);
        return textBlock;
    }

    private static Control CreateHyperlink(HtmlNode node)
    {
        var text = GetInnerText(node);
        var href = node.GetAttributeValue("href", "");

        var button = new HyperlinkButton
        {
            Content = string.IsNullOrWhiteSpace(text) ? href : text,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 2),
            Background = new SolidColorBrush(Colors.Transparent)
        };

        if (!string.IsNullOrEmpty(href) && Uri.TryCreate(href, UriKind.Absolute, out var uri))
            button.NavigateUri = uri;

        return button;
    }

    private static Control CreateImage(HtmlNode node)
    {
        var src = node.GetAttributeValue("src", "");
        var alt = node.GetAttributeValue("alt", "");

        if (string.IsNullOrEmpty(src))
            return new Border { Height = 0 };

        var imageWidget = new LocalImageRenderWidget(src)
        {
            Margin = new Thickness(0, 8),
            Width = 400,
            Height = 225
        };

        return imageWidget;
    }

    private static Control CreateVideoPlaceholder(HtmlNode node)
    {
        var src = node.GetAttributeValue("src", "");

        var panel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 12)
        };

        panel.Children.Add(new TextBlock
        {
            Text = "📹 视频内容",
            FontSize = 16,
            FontWeight = FontWeight.SemiBold
        });

        if (!string.IsNullOrEmpty(src))
        {
            var button = new HyperlinkButton
            {
                Content = "在浏览器中观看",
                Padding = new Thickness(0)
            };

            if (Uri.TryCreate(src, UriKind.Absolute, out var uri))
                button.NavigateUri = uri;

            panel.Children.Add(button);
        }

        return panel;
    }

    private static Control CreateSpan(HtmlNode node)
    {
        var text = GetInnerText(node);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var textBlock = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap
        };

        ApplyColor(node, textBlock);
        ApplyTextDecoration(node, textBlock);

        return textBlock;
    }

    private static void ApplyColor(HtmlNode node, TextBlock textBlock)
    {
        var style = node.GetAttributeValue("style", "");
        if (string.IsNullOrEmpty(style))
            return;

        if (style.Contains("color:"))
        {
            var colorMatch = Regex.Match(style, @"color:\s*#([0-9a-fA-F]{6})");
            if (colorMatch.Success)
            {
                try
                {
                    textBlock.Foreground = new SolidColorBrush(Color.Parse($"#{colorMatch.Groups[1].Value}"));
                }
                catch
                {
                    // 忽略颜色解析错误
                }
            }
        }
    }

    private static void ApplyTextDecoration(HtmlNode node, TextBlock textBlock)
    {
        var style = node.GetAttributeValue("style", "");
        if (!string.IsNullOrEmpty(style) && style.Contains("text-decoration") && style.Contains("underline"))
            textBlock.TextDecorations = TextDecorations.Underline;
    }
}