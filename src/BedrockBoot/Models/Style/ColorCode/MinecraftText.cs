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

// MinecraftText.cs

using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace BedrockBoot.Models.Style;

public class MinecraftTextBlock : TextBlock
{
    public static readonly StyledProperty<string> MinecraftTextProperty =
        AvaloniaProperty.Register<MinecraftTextBlock, string>(
            nameof(MinecraftText),
            string.Empty);

    public static readonly StyledProperty<bool> ShowRawTextProperty =
        AvaloniaProperty.Register<MinecraftTextBlock, bool>(
            nameof(ShowRawText));

    private List<MinecraftTextParser.TextSegment>? _currentSegments;

    static MinecraftTextBlock()
    {
        MinecraftTextProperty.Changed.AddClassHandler<MinecraftTextBlock>((x, e) => x.OnTextChanged());
        ShowRawTextProperty.Changed.AddClassHandler<MinecraftTextBlock>((x, e) => x.OnTextChanged());
    }

    public MinecraftTextBlock()
    {
        IBrush GetFontColorResourceFromApp()
        {
            // Application.Current 是一个全局的入口点
            var app = Application.Current;

            if (app != null)
                // 从应用程序的资源中查找 :cite[1]
                // 注意：这里查找的是 Application.Resources 里定义的资源
                if (app.TryFindResource("PrimaryForegroundBrush", out var resourceValue))
                    return resourceValue as IBrush;

            return new SolidColorBrush(Colors.Gray);
        }

        Foreground = GetFontColorResourceFromApp();
        TextWrapping = TextWrapping.Wrap;
    }

    public string MinecraftText
    {
        get => GetValue(MinecraftTextProperty);
        set => SetValue(MinecraftTextProperty, value);
    }

    public bool ShowRawText
    {
        get => GetValue(ShowRawTextProperty);
        set => SetValue(ShowRawTextProperty, value);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        CleanupSegments();
    }

    private void OnTextChanged()
    {
        // 清理旧的混淆文本定时器
        CleanupSegments();

        if (ShowRawText)
        {
            // 显示原始文本
            Inlines?.Clear();
            Text = MinecraftText;
        }
        else
        {
            // 显示解析后的富文本
            Text = null;

            // 获取元组，只取第一个元素（InlineCollection）
            var (inlines, segments) = MinecraftTextParser.ConvertToInlines(MinecraftText);
            Inlines = inlines;

            // 保存segment引用并启动混淆文本定时器
            _currentSegments = segments;
            StartObfuscation();
        }
    }

    private void StartObfuscation()
    {
        if (_currentSegments == null) return;

        foreach (var segment in _currentSegments)
            if (segment.IsObfuscated)
                segment.StartObfuscation();
    }

    private void CleanupSegments()
    {
        if (_currentSegments != null)
        {
            foreach (var segment in _currentSegments) segment.Dispose();

            _currentSegments.Clear();
            _currentSegments = null;
        }
    }
}