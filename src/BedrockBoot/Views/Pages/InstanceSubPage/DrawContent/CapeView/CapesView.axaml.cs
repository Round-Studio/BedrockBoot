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
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Capes;
using BedrockBoot.Standard.Entity.Game;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent.CapeView;

public partial class CapesView : UserControl
{
    public CapesPackManager Manager { get; set; }

    public CapesView(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
        Manager = new CapesPackManager(versionInfo);
        UpdateContent();
    }

    public CapesView()
    {
        InitializeComponent();
    }

    public VersionConfig VersionInfo { get; set; }

    private void UpdateContent()
    {
        SavesBox.Children.Clear();

        var packs = Manager.LoadPacks();
        NullBox.IsVisible = packs.Count == 0;

        foreach (var pack in packs)
        {
            //SavesBox.Children.Add();
        }
    }

    private void ImportPackBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void CreateGroupBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
}
