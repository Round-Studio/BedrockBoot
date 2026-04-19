using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
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

        ProcessNodes(doc.DocumentNode.ChildNodes, controls);

        return controls;
    }

    private static void ProcessNodes(HtmlNodeCollection nodes, List<Control> controls)
    {
        if (nodes == null || nodes.Count == 0)
            return;

        foreach (var node in nodes)
            try
            {
                var nodeControls = ConvertNodeToControls(node);
                if (nodeControls != null && nodeControls.Count > 0) controls.AddRange(nodeControls);
            }
            catch
            {
            }
    }

    private static List<Control> ConvertNodeToControls(HtmlNode node)
    {
        var controls = new List<Control>();

        if (node == null)
            return controls;

        switch (node.NodeType)
        {
            case HtmlNodeType.Text:
                var text = HtmlEntity.DeEntitize(node.InnerText).Trim();
                if (!string.IsNullOrWhiteSpace(text) && text != "&nbsp;")
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
                    "h1" => CreateHeading(node, 24, FontWeight.Bold),
                    "h2" => CreateHeading(node, 20, FontWeight.Bold),
                    "h3" => CreateHeading(node, 18, FontWeight.SemiBold),
                    "p" => CreateParagraph(node),
                    "ul" => CreateList(node, false),
                    "ol" => CreateList(node, true),
                    "strong" or "b" => CreateStyledText(node, FontWeight.Bold),
                    "em" or "i" => CreateStyledText(node, FontWeight.Normal, FontStyle.Italic),
                    "u" => CreateUnderlinedText(node),
                    "a" => CreateHyperlink(node),
                    "img" => CreateImage(node),
                    "br" => new Border { Height = 8 },
                    "iframe" => CreateVideoPlaceholder(node),
                    "span" => CreateSpan(node),
                    "div" => null,
                    _ => null
                };

                if (control != null)
                    controls.Add(control);
                else if (node.Name.ToLower() == "div" && node.HasChildNodes)
                    ProcessNodes(node.ChildNodes, controls);
                else if (node.HasChildNodes) ProcessNodes(node.ChildNodes, controls);
                break;
        }

        return controls;
    }

    private static Control CreateHeading(HtmlNode node, int fontSize, FontWeight fontWeight)
    {
        var text = GetInnerText(node);
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = fontWeight,
            Margin = new Thickness(0, 12, 0, 8),
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
            n.Name.ToLower() is "img" or "a" or "strong" or "em" or "span" or "b" or "i" or "u" or "iframe");

        if (hasComplexContent)
        {
            var panel = new StackPanel
            {
                Spacing = 4,
                Margin = new Thickness(0, 6)
            };

            var inlineControls = new List<Control>();
            ProcessNodes(node.ChildNodes, inlineControls);

            foreach (var control in inlineControls) panel.Children.Add(control);

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

        var index = 1;
        foreach (var li in node.ChildNodes.Where(n => n.Name.ToLower() == "li"))
        {
            var prefix = isOrdered ? $"{index}. " : "• ";
            var itemPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            itemPanel.Children.Add(new TextBlock
            {
                Text = prefix,
                VerticalAlignment = VerticalAlignment.Top
            });

            var text = GetInnerText(li);
            itemPanel.Children.Add(new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top
            });

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
            Content = text,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 2)
        };

        if (!string.IsNullOrEmpty(href) && Uri.TryCreate(href, UriKind.Absolute, out var uri)) button.NavigateUri = uri;

        return button;
    }

    private static Control CreateImage(HtmlNode node)
    {
        var src = node.GetAttributeValue("src", "");
        var width = node.GetAttributeValue("width", "");

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

            if (Uri.TryCreate(src, UriKind.Absolute, out var uri)) button.NavigateUri = uri;

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

    private static string GetInnerText(HtmlNode node)
    {
        if (node == null)
            return string.Empty;

        if (node.ChildNodes.All(n => n.NodeType == HtmlNodeType.Text))
            return HtmlEntity.DeEntitize(node.InnerText).Trim();

        var textBuilder = new StringBuilder();
        foreach (var child in node.ChildNodes)
            if (child.NodeType == HtmlNodeType.Text)
            {
                textBuilder.Append(HtmlEntity.DeEntitize(child.InnerText));
            }
            else if (child.NodeType == HtmlNodeType.Element)
            {
                textBuilder.Append(GetInnerText(child));
                textBuilder.Append(" ");
            }

        return textBuilder.ToString().Trim();
    }

    private static void ApplyColor(HtmlNode node, TextBlock textBlock)
    {
        var style = node.GetAttributeValue("style", "");
        if (style.Contains("color:"))
        {
            var colorMatch = Regex.Match(style, @"color:\s*#([0-9a-fA-F]{6})");
            if (colorMatch.Success)
                try
                {
                    textBlock.Foreground = new SolidColorBrush(Color.Parse($"#{colorMatch.Groups[1].Value}"));
                }
                catch
                {
                }
        }
    }

    private static void ApplyTextDecoration(HtmlNode node, TextBlock textBlock)
    {
        var style = node.GetAttributeValue("style", "");
        if (style.Contains("text-decoration") && style.Contains("underline"))
            textBlock.TextDecorations = TextDecorations.Underline;
    }
}