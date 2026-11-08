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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Enum;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.DllMods;
using BedrockBoot.Models.Pack.Game.Isolation;
using BedrockBoot.Standard.Core;
using BedrockBoot.Standard.Interface.Mods;
using BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;
using BedrockBoot.Views.TaskItem;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation.LeftSelectBar;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawInstanceContent : UserControl
{
    private List<IModSupport> _supports;

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

        _supports = ModSupports.GetEnableSupports(VersionInfo);
        _supports.ForEach(support =>
        {
            InstanceTabControl.Items.Add(new LeftSelectBarItem()
            {
                ItemText = support.SupportName,
                Glyph = support.Glyph,
                Tag = support.Tag
            });
        });

        IsEditMode = true;
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        ImageLoader.Shared.ClearMemoryCache();

#if WINDOWS
        BedrockBoot.Windows.Models.ProcessMemoryTrimmer.TrimProcess();
#endif
    }

    private void InstanceTabControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            var tag = ((LeftSelectBarItem)InstanceTabControl.SelectedItem).Tag.ToString();
            VersionInfo = GameInfoHelper.GetVersionConfig(VersionInfo.VersionPath);

            LaunchBtn.IsVisible = tag != "Info";
            object? page = null; 

            switch (tag)
            {
                case "Info":
                    page = new InstanceInfo(VersionInfo);
                    break;
                case "Setting":
                    page = new InstanceSetting(VersionInfo);
                    break;
                case "Mods":
                    page = new InstanceMods(VersionInfo);
                    break;
                case "Loaders":
                    page = new InstanceLoaders(VersionInfo);
                    break;
                case "Pack":
                    page = new InstancePack(VersionInfo);
                    break;
                case "Save":
                    page = new InstanceSave(VersionInfo);
                    break;
                case "Screenshots":
                    page = new InstanceScreenshots(VersionInfo);
                    break;
                case "Server":
                    page = new InstanceServer(VersionInfo);
                    break;
                case "Controls":
                    page = new InstanceControls(VersionInfo);
                    break;
                case "Information":
                    page = new InstanceInformation(VersionInfo);
                    break;
            }

            if (page == null)
            {
                page = _supports.Find(x => x.Tag == tag)?.SupportPage;
            }
            
            if (page != null)
            {
                InstanceFrame.NavigateTo(page);
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
        PlatformCore.OpenFolder!.Open(VersionInfo.VersionPath);
    }

    private void MenuOpenConfigFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        PlatformCore.OpenFolder!.Open(Path.Combine(VersionInfo.VersionPath, "config"));
    }

    private void MenuOpenSkinFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        PlatformCore.OpenFolder!.Open(IsolationCore.GetInstanceFolderPath(VersionInfo, InstanceFolderType.SkinPackFolder));
    }

    private void MenuOpenBehaviorFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        PlatformCore.OpenFolder!.Open(IsolationCore.GetInstanceFolderPath(VersionInfo, InstanceFolderType.BehaviorPackFolder));
    }

    private void MenuOpenResourceFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        PlatformCore.OpenFolder!.Open(IsolationCore.GetInstanceFolderPath(VersionInfo, InstanceFolderType.ResourcePackFolder));
    }

    private void MenuOpenSkinDevFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        PlatformCore.OpenFolder!.Open(
            IsolationCore.GetInstanceFolderPath(VersionInfo, InstanceFolderType.DevelopSkinPackFolder));
    }

    private void MenuOpenBehaviorDevFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        PlatformCore.OpenFolder!.Open(
            IsolationCore.GetInstanceFolderPath(VersionInfo, InstanceFolderType.DevelopBehaviorPackFolder));
    }

    private void MenuOpenResourceDevFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        PlatformCore.OpenFolder!.Open(
            IsolationCore.GetInstanceFolderPath(VersionInfo, InstanceFolderType.DevelopResourcePackFolder));
    }
}