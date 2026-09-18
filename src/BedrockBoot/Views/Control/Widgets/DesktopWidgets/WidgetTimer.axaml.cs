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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Timers;
using AvaMotion.Controls.Text;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Interface;

namespace BedrockBoot.Views.Control.Widgets.DesktopWidgets;

public partial class WidgetTimer : IWidgetTemplated
{
    private AnimationTextBlock? _timeTextBlock;
    private Timer? _timer;

    public WidgetTimer()
    {
        SupportWidgetSize = new()
        {
            WidgetSize.Small,
            WidgetSize.Medium,
            WidgetSize.Large,
            WidgetSize.ExtraLarge
        };
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _timeTextBlock = this.FindControl<AnimationTextBlock>("TimeTextBlock");
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        _timer = new Timer(1000);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        _timer.Start();
        
        UpdateTime();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (_timer != null)
        {
            _timer.Elapsed -= OnTimerElapsed;
            _timer.Stop();
            _timer.Dispose();
            _timer = null;
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(UpdateTime);
    }

    private void UpdateTime()
    {
        if (_timeTextBlock == null) return;
        _timeTextBlock.Text = DateTime.Now.ToString("H:mm");
    }
}