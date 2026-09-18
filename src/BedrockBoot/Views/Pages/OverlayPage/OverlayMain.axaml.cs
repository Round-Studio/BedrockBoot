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
using System.Timers;
using Avalonia.Controls;
using Avalonia.Threading;

namespace BedrockBoot.Views.Pages.OverlayPage;

public partial class OverlayMain : UserControl
{
    private readonly DispatcherTimer _dispatcherTimer;
    private readonly TextBlock _timeBlock;
    private readonly Timer _timer;

    public OverlayMain()
    {
        InitializeComponent();

        _timeBlock = TimeBlock;

        _dispatcherTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _dispatcherTimer.Tick += DispatcherTimer_Tick;
        _dispatcherTimer.Start();

        UpdateTime();
    }

    private void DispatcherTimer_Tick(object? sender, EventArgs e)
    {
        UpdateTime();
    }

    private void UpdateTime()
    {
        if (_timeBlock != null) _timeBlock.Text = DateTime.Now.ToString("HH:mm:ss");
    }
}