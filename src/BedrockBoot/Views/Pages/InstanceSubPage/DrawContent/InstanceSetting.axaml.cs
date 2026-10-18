using Avalonia;
using Avalonia.Controls;
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
        IsNativeException.IsChecked = VersionInfo.Config.IsNativeException;
        CatalogStrategy.SelectedIndex = (int)VersionInfo.Config.IsolationFolderPolicy;
        HttpPatchEnable.IsChecked = VersionInfo.Config.IsEnableHttpPatch;
        PatchServerDiscoveryBlock.IsChecked = VersionInfo.Config.HttpPatchConfig.IsEnableServerDiscoveryBlock;

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
            VersionInfo.Config.IsNativeException = (bool)IsNativeException.IsChecked!;
            VersionInfo.Config.IsEnableHttpPatch = (bool)HttpPatchEnable.IsChecked!;
            VersionInfo.Config.HttpPatchConfig.IsEnableServerDiscoveryBlock = (bool)PatchServerDiscoveryBlock.IsChecked!;

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
}