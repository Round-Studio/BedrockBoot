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
using Avalonia.Threading;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Views.Pages.SetupPage;

public partial class SetupCompleted : UserControl
{
    public SetupCompleted()
    {
        InitializeComponent();
    }

    private void Start_OnClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine(@"跳转主页面.jpg");
        Dispatcher.UIThread.Invoke(() => GlobalModel.MainWindow.MainFrame.NavigateTo(new MainPage()));
        Core.Global.GlobalModel.Config.Data.IsFirstRun = false;
        Core.Global.GlobalModel.Config.Save();
    }
}