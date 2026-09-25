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
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Enum;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Isolation;
using BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;
using BedrockBoot.Views.TaskItem;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation.LeftSelectBar;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawInstanceContent : UserControl
{
    public DrawInstanceContent()
    {
        InitializeComponent();

        IsEditMode = true;

#if RELEASE
        GameControls.IsEnabled = BedrockBoot.Models.Global.GlobalModel.FunctionOption.IsEnableGameInstanceControl;
#endif

/*#if LINUX
        Mods.IsVisible = false;
        Loaders.IsVisible = false;
#endif*/
    }

    public DrawInstanceContent(VersionConfig info) : this()
    {
        VersionInfo = info;

        Update();
    }

    public VersionConfig VersionInfo { get; set; }
    public bool IsEditMode { get; set; }

    public void Update()
    {
        IsEditMode = false;
        InstanceFrame.NavigateTo(new InstanceInfo(VersionInfo));

        IsEditMode = true;
    }

    private void InstanceTabControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            var tag = ((LeftSelectBarItem)InstanceTabControl.SelectedItem).Tag.ToString();
            VersionInfo = GameInfoHelper.GetVersionConfig(VersionInfo.VersionPath);

            LaunchBtn.IsVisible = tag != "Info";

            switch (tag)
            {
                case "Info":
                    InstanceFrame.NavigateTo(new InstanceInfo(VersionInfo));
                    break;
                case "Mods":
                    InstanceFrame.NavigateTo(new InstanceMods(VersionInfo));
                    break;
                case "Loaders":
                    InstanceFrame.NavigateTo(new InstanceLoaders(VersionInfo));
                    break;
                case "Pack":
                    InstanceFrame.NavigateTo(new InstancePack(VersionInfo));
                    break;
                case "Save":
                    InstanceFrame.NavigateTo(new InstanceSave(VersionInfo));
                    break;
                case "Screenshots":
                    InstanceFrame.NavigateTo(new InstanceScreenshots(VersionInfo));
                    break;
                case "Server":
                    InstanceFrame.NavigateTo(new InstanceServer(VersionInfo));
                    break;
                case "Controls":
                    InstanceFrame.NavigateTo(new InstanceControls(VersionInfo));
                    break;
                case "Information":
                    InstanceFrame.NavigateTo(new InstanceInformation(VersionInfo));
                    break;
            }
        }
    }

    private void LaunchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        VersionInfo = GameInfoHelper.GetVersionConfig(VersionInfo.VersionPath);
        TaskLaunchGameItem.Launch(VersionInfo);
    }

    private void MenuOpenFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenFolderHelper.Open(VersionInfo.VersionPath);
    }

    private void MenuOpenConfigFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenFolderHelper.Open(Path.Combine(VersionInfo.VersionPath, "config"));
    }

    private void MenuOpenSkinFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenFolderHelper.Open(IsolationCore.GetInstanceFolderPath(VersionInfo, InstanceFolderType.SkinPackFolder));
    }

    private void MenuOpenBehaviorFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenFolderHelper.Open(IsolationCore.GetInstanceFolderPath(VersionInfo, InstanceFolderType.BehaviorPackFolder));
    }

    private void MenuOpenResourceFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenFolderHelper.Open(IsolationCore.GetInstanceFolderPath(VersionInfo, InstanceFolderType.ResourcePackFolder));
    }

    private void MenuOpenSkinDevFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenFolderHelper.Open(
            IsolationCore.GetInstanceFolderPath(VersionInfo, InstanceFolderType.DevelopSkinPackFolder));
    }

    private void MenuOpenBehaviorDevFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenFolderHelper.Open(
            IsolationCore.GetInstanceFolderPath(VersionInfo, InstanceFolderType.DevelopBehaviorPackFolder));
    }

    private void MenuOpenResourceDevFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenFolderHelper.Open(
            IsolationCore.GetInstanceFolderPath(VersionInfo, InstanceFolderType.DevelopResourcePackFolder));
    }
}