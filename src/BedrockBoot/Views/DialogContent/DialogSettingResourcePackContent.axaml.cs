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
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogSettingResourcePackContent : UserControl
{
    private readonly ResourcePackManifest _manifest;

    public DialogSettingResourcePackContent()
    {
        InitializeComponent();
    }

    public DialogSettingResourcePackContent(ResourcePackManifest manifest) : this()
    {
        _manifest = manifest;
        UpdateUi();
    }

    public void UpdateUi()
    {
        PackName.Text = _manifest.Header.Name;
        PackDescription.Text = _manifest.Header.Description;
        AllowAchievement.IsChecked = _manifest.Metadata.ProductType == "addon";
    }

    public void OnSave()
    {
        _manifest.Header.Name = PackName.Text!;
        _manifest.Header.Description = PackDescription.Text!;
        _manifest.Metadata.ProductType = (bool)AllowAchievement.IsChecked! ? "addon" : "";
        _manifest.SaveConfig();
    }
}