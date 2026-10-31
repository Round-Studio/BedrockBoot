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
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Core.Global;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Enum.Config;
using BedrockBoot.Standard.Helper;
using BedrockLauncher.Core;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceSetting : UserControl
{
    public InstanceSetting()
    {
        InitializeComponent();
    }

    public InstanceSetting(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;

        UpdateUi();
    }

    public VersionConfig VersionInfo { get; set; }
    public bool IsEdit { get; set; }

    public void UpdateUi()
    {
        IsEdit = false;

#if LINUX
        IsolationCard.IsVisible = false;
        HighLevel.IsVisible = false;
        IsRunAdminCard.IsVisible = false;
#endif

#if RELEASE
        InstanceMod.IsVisible = BedrockBoot.Models.Global.GlobalModel.FunctionOption.IsEnableGameInstanceMods;
#endif

        InstanceIsolationPanel.IsVisible = InstanceIsolationPanel.IsEnabled =
            VersionInfo.Info.BuildType == MinecraftBuildTypeVersion.GDK;
#if WINDOWS
        IsRunAdminCard.IsVisible = VersionInfo.Info.BuildType == MinecraftBuildTypeVersion.GDK &&
                                   !GlobalModel.Config.Data.IsUseMultipleUsers;
#endif
        InstanceArgs.Text = VersionInfo.Config.OtherCommand;
        InstanceConsole.IsChecked = VersionInfo.Config.IsConsole;
        InstanceEdit.IsChecked = VersionInfo.Config.IsEditModel;
        InstanceMod.IsChecked = VersionInfo.Config.IsModes;
        InstanceIsolated.IsChecked = VersionInfo.Config.IsVersionIsolated;
        InstanceDetailedLogs.IsChecked = VersionInfo.Config.IsDetailedLog;
        GameConfigSwitch.IsChecked = VersionInfo.Config.IsSyncPublicOptions;
        IsRunAdmin.IsChecked = VersionInfo.Config.SysWindowsConfig.IsUseAdminRun;
        CatalogStrategy.SelectedIndex = (int)VersionInfo.Config.IsolationFolderPolicy;
        HttpPatchEnable.IsChecked = VersionInfo.Config.IsEnableHttpPatch;
        PatchServerDiscoveryBlock.IsChecked = VersionInfo.Config.HttpPatchConfig.IsEnableServerDiscoveryBlock;
        PatchVisualBlur.IsChecked = VersionInfo.Config.VisualConfig.IsEnableMotionBlur;
        MotionValue.Value = VersionInfo.Config.VisualConfig.MotionBlurStrength;

        IsEdit = true;
    }

    private void BoolTypeConfig_OnChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            VersionInfo.Config.IsConsole = (bool)InstanceConsole.IsChecked!;
            VersionInfo.Config.IsEditModel = (bool)InstanceEdit.IsChecked!;
            VersionInfo.Config.IsVersionIsolated = (bool)InstanceIsolated.IsChecked!;
            VersionInfo.Config.IsModes = (bool)InstanceMod.IsChecked!;
            VersionInfo.Config.IsDetailedLog = (bool)InstanceDetailedLogs.IsChecked!;
            VersionInfo.Config.IsSyncPublicOptions = (bool)GameConfigSwitch.IsChecked!;
            VersionInfo.Config.SysWindowsConfig.IsUseAdminRun = (bool)IsRunAdmin.IsChecked!;
            VersionInfo.Config.IsNativeException = true;
            VersionInfo.Config.IsEnableHttpPatch = (bool)HttpPatchEnable.IsChecked!;
            VersionInfo.Config.HttpPatchConfig.IsEnableServerDiscoveryBlock = (bool)PatchServerDiscoveryBlock.IsChecked!;
            VersionInfo.Config.VisualConfig.IsEnableMotionBlur = (bool)PatchVisualBlur.IsChecked!;

            GameInfoHelper.SaveVersionConfig(VersionInfo);
        }
    }

    private void CatalogStrategy_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            VersionInfo.Config.IsolationFolderPolicy = (CatalogStrategyEnum)CatalogStrategy.SelectedIndex;
            VersionInfo.Config.FolderPolicyStr =
                IsolationPolicyHelper.ParsePolicyConfig(VersionInfo.Config.IsolationFolderPolicy);

            GameInfoHelper.SaveVersionConfig(VersionInfo);
        }
    }


    private void TextTypeConfig_OnChanged(object? sender, TextChangedEventArgs e)
    {
        if (IsEdit)
        {
            VersionInfo.Config.OtherCommand = InstanceArgs.Text;
            GameInfoHelper.SaveVersionConfig(VersionInfo);
        }
    }

    private void MotionValue_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (IsEdit)
        {
            VersionInfo.Config.VisualConfig.MotionBlurStrength = (int)MotionValue.Value;
            GameInfoHelper.SaveVersionConfig(VersionInfo);
        }
    }
}