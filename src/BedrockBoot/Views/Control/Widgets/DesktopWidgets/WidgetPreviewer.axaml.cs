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
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Views.Control.Widgets.DesktopWidgets;

public partial class WidgetPreviewer : UserControl
{
    private readonly WidgetRegisterInfo _wid;

    public WidgetPreviewer()
    {
        InitializeComponent();
    }

    public WidgetPreviewer(WidgetRegisterInfo wid) : this()
    {
        _wid = wid;
        var content = DesktopWorkspace.CreateWidgetFromType(wid.Type);
        WidgetName.Text = wid.Name;
        WidgetDescription.Text = wid.Description;
        ContentControl.Content = content;
    }

    private void AddBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        GlobalModel.MainWindow.CloseDraw();
        DesktopWorkspace.Instance.AddWidget(_wid.Type);
    }
}