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

using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Interface.Download;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.Control.Items.Download;

namespace BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;

public partial class ResultFiles : UserControl
{
    private readonly IDownloadResult _service;

    public ResultFiles()
    {
        InitializeComponent();
    }

    public ResultFiles(IDownloadResult service) : this()
    {
        _service = service;
        _ = UpdateUi();
    }

    private List<string> _versions = new();

    private async Task UpdateUi()
    {
        var files = await _service.GetFiles();
        files.ForEach(f =>
        {
            if (!string.IsNullOrEmpty(f.VersionGroup))
            {
                if (!_versions.Contains(f.VersionGroup))
                {
                    _versions.Add(f.VersionGroup);
                    FilesList.Children.Add(new TextBlock()
                    {
                        Text = f.VersionGroup,
                        FontSize = 14,
                        FontWeight = FontWeight.Bold
                    });
                }
            }

            FilesList.Children.Add(new ResourceFileItem(f));
        });
    }
}