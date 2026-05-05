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

namespace BedrockBoot.Models.Helper;

public class HtmlToControlConverter
{
    public static List<Control> ConvertHtmlToControls(string html)
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

    // 修改 GetInnerTextWithoutAnchor 方法
    private static string GetInnerTextWithoutAnchor(HtmlNode node)
    {
        if (node == null)
            return string.Empty;

        // 克隆节点并移除所有锚点链接
        var clone = node.CloneNode(true);

        // 移除所有 anchor 锚点（href 以 # 开头）
        var anchors = clone.SelectNodes(".//a[@href]");
        if (anchors != null)
        {
            foreach (var anchor in anchors)
            {
                var href = anchor.GetAttributeValue("href", "");
                // 如果是锚点链接（以 # 开头），则移除
                if (href.StartsWith("#"))
                {
                    anchor.Remove();
                }
            }
        }

        return GetInnerText(clone);
    }

// 修改 GetInnerText 方法中的锚点过滤
    private static string GetInnerText(HtmlNode node)
    {
        if (node == null)
            return string.Empty;

        // 跳过 SVG 元素
        if (node.Name == "svg" || node.Name == "path")
            return string.Empty;

        // 如果是锚点链接（href 以 # 开头），跳过
        if (node.Name == "a")
        {
            var href = node.GetAttributeValue("href", "");
            if (href.StartsWith("#"))
                return string.Empty;
        }

        if (node.ChildNodes.All(n => n.NodeType == HtmlNodeType.Text || n.Name == "svg" || n.Name == "path"))
        {
            var text = HtmlEntity.DeEntitize(node.InnerText).Trim();
            // 移除开头的 # 符号
            text = Regex.Replace(text, @"^#+\s*", "");
            // 清理多余空格
            return Regex.Replace(text, @"\s+", " ").Trim();
        }

        var textBuilder = new StringBuilder();
        foreach (var child in node.ChildNodes)
        {
            if (child.Name == "svg" || child.Name == "path")
                continue;

            // 跳过锚点链接
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
        // 移除开头的 # 符号
        result = Regex.Replace(result, @"^#+\s*", "");
        return result;
    }

// 同时更新 ProcessNodes 方法，提前跳过后面的锚点元素
    private static void ProcessNodes(HtmlNodeCollection nodes, List<Control> controls)
    {
        if (nodes == null || nodes.Count == 0)
            return;

        foreach (var node in nodes)
        {
            try
            {
                // 跳过 SVG 和锚点元素
                if (node.Name == "svg" || node.Name == "path" ||
                    node.GetAttributeValue("aria-hidden", "") == "true")
                    continue;

                // 跳过锚点链接
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

        // 处理特殊的 markdown-accessiblity-table 标签
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
                    "thead" or "tbody" => null, // 已在 table 中处理
                    "tr" => null, // 已在 table 中处理
                    "th" or "td" => null, // 已在 table 中处理
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

    // 新增：创建表格
    private static Control CreateTable(HtmlNode node)
    {
        var grid = new Grid();
        var rows = new List<List<Control>>();
        var columnCount = 0;

        // 查找实际的 table 元素
        var tableNode = node.Name == "table" ? node : node.SelectSingleNode(".//table");
        if (tableNode == null)
            return null;

        // 获取所有行
        var rowsList = new List<HtmlNode>();

        // 先处理 thead
        var thead = tableNode.SelectSingleNode(".//thead");
        if (thead != null)
        {
            rowsList.AddRange(thead.SelectNodes(".//tr") ?? new HtmlNodeCollection(null));
        }

        // 再处理 tbody
        var tbody = tableNode.SelectSingleNode(".//tbody");
        if (tbody != null)
        {
            rowsList.AddRange(tbody.SelectNodes(".//tr") ?? new HtmlNodeCollection(null));
        }

        // 如果没有 thead/tbody，直接找 tr
        if (rowsList.Count == 0)
        {
            rowsList.AddRange(tableNode.SelectNodes(".//tr") ?? new HtmlNodeCollection(null));
        }

        foreach (var row in rowsList)
        {
            var cells = new List<Control>();
            var headers = row.SelectNodes("th");
            var dataCells = row.SelectNodes("td");

            // 处理表头
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

            // 处理数据单元格
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

        // 设置 Grid 列定义
        for (int i = 0; i < columnCount; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        }

        // 添加行到 Grid
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

        // 添加边框
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

    // 新增：创建分隔线
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
        // 清理标题中的锚点链接
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

        // 检查是否有需要特殊处理的子元素
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

            // 检查 li 内是否有链接或其他复杂内容
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

        // 支持 color 和 background-color
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