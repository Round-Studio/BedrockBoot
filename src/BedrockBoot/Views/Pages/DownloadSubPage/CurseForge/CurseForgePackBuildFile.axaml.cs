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
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Views.Control.Items;

namespace BedrockBoot.Views.Pages.DownloadSubPage.CurseForge;

public partial class CurseForgePackBuildFile : UserControl
{
    public CurseForgeResponse.ModData ModData;

    public CurseForgePackBuildFile()
    {
        InitializeComponent();
    }

    public CurseForgePackBuildFile(CurseForgeResponse.ModData mod) : this()
    {
        ModData = mod;

        Update();
    }

    private void Update()
    {
        Console.WriteLine($@"查看模组详细信息：{ModData.Id}");
        NoneBox.IsVisible = false;

        Task.Run(() =>
        {
            try
            {
                var files = new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey)
                    .GetModFilesAsync(ModData.Id).Result;

                Dispatcher.UIThread.Invoke(() =>
                {
                    files.Data.ForEach(f => { List.Children.Add(new CurseForgeModBuildFileItem(f)); });

                    LoadingRing.IsVisible = false;
                });
            }
            catch
            {
                Dispatcher.UIThread.Invoke(() => LoadingRing.IsVisible = false);
                Dispatcher.UIThread.Invoke(() => NoneBox.IsVisible = true);
            }
        });
    }
}