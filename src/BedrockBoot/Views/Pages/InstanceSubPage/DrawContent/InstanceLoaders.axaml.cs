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
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Interface;
using BedrockBoot.Interface.ModLoader;
using BedrockBoot.Models.Pack.Game.Loaders;
using BedrockBoot.Views.Control.Items.Instance;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceLoaders : UserControl
{
    private readonly VersionConfig _versionInfo;

    public InstanceLoaders()
    {
        InitializeComponent();
    }

    public InstanceLoaders(VersionConfig versionInfo) : this()
    {
        _versionInfo = versionInfo;
        UpdateUi();
    }

    public void UpdateUi()
    {
        LoadersList.Children.Clear();
        if (LoadersManager.ModsLoaders != null)
        {
            foreach (var loaderType in LoadersManager.ModsLoaders)
            {
                if (typeof(IModsLoader).IsAssignableFrom(loaderType))
                {
                    var instance = (IModsLoader)Activator.CreateInstance(loaderType);
                    instance.OnUpdate = () => UpdateUi();
                    LoadersList.Children.Add(new ModLoaderItem(_versionInfo, instance));
                }
            }
        }
    }
}