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
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace BedrockBoot.Views.Control.Widgets;

public partial class SmallGameView : UserControl
{
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<SmallGameView, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    public SmallGameView()
    {
        InitializeComponent();
        MainButton.Click += (s, e) => RaiseEvent(new RoutedEventArgs(ClickEvent));
    }

    public string Version
    {
        get => VersionText.Text ?? "";
        set => VersionText.Text = value;
    }

    public string Description
    {
        get => DescriptionText.Text ?? "";
        set => DescriptionText.Text = value;
    }

    public IImage Icon
    {
        get => IconImage.Source;
        set => IconImage.Source = value;
    }

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }
}