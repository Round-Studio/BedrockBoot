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

using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.GravityCone;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.Pages.GravityConePage;
using BedrockBoot.Views.Pages.MultiplayerPage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation;
using Round.SDK.Helper;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainGravityConePage : BedrockBootPage
{
    public static NavigationFrame NavigationFrame;

    public MainGravityConePage()
    {
        InitializeComponent();
        NavigationFrame = this.MainFrame;
        
        var ext = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
        if (!File.Exists(Path.Combine(DialogDownloadMultiPlayerDependenceContent.GravityConeExePath, $"gravitycone-cli-{OS.GetSystemType()}-amd64{ext}")) ||
            !File.Exists(Path.Combine(DialogDownloadMultiPlayerDependenceContent.EasyTierPath, $"easytier-cli{ext}")))
            MainFrame.NavigateTo(new MultiplayerDependenceDownload());
        else
        {
            if (GlobalModel.GravityConeClient == null)
            {
                NavigationFrame.NavigateTo(new GravityConeInit());
            }
            else if (GlobalModel.GravityConeClient != null)
            {
                if (GlobalModel.CurrentRoomState != null)
                {
                    NavigationFrame.NavigateTo(new GravityConeRoom());
                }
                else
                {
                    NavigationFrame.NavigateTo(new GravityConeRoot());
                }
            }
        }
    }
}