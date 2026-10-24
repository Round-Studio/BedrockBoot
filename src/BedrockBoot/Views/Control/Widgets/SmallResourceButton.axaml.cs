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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BedrockBoot.Views.Control.Widgets;

public partial class SmallResourceButton : UserControl
{
    // 定义属性
    public static readonly StyledProperty<string> ResourceNameProperty =
        AvaloniaProperty.Register<SmallResourceButton, string>(nameof(ResourceName));

    public static readonly StyledProperty<string> AuthorProperty =
        AvaloniaProperty.Register<SmallResourceButton, string>(nameof(Author));

    public static readonly StyledProperty<string> IconUrlProperty =
        AvaloniaProperty.Register<SmallResourceButton, string>(nameof(IconUrl));

    // 定义向外暴露的 Click 事件
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<SmallResourceButton, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    public SmallResourceButton()
    {
        InitializeComponent();
    }

    public string ResourceName
    {
        get => GetValue(ResourceNameProperty);
        set => SetValue(ResourceNameProperty, value);
    }

    public string Author
    {
        get => GetValue(AuthorProperty);
        set => SetValue(AuthorProperty, value);
    }

    public string IconUrl
    {
        get => GetValue(IconUrlProperty);
        set => SetValue(IconUrlProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    // 内部 Button 点击时，触发 UserControl 的 Click 事件
    private void OnInternalButtonClick(object? sender, RoutedEventArgs e)
    {
        var args = new RoutedEventArgs(ClickEvent);
        RaiseEvent(args);
    }
}