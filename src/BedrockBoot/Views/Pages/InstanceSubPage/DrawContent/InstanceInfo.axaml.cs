using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceInfo : UserControl
{
    public InstanceInfo()
    {
        IsEdit = false;

        InitializeComponent();

#if RELEASE
        InstanceMod.IsVisible = GlobalModel.FunctionOption.IsEnableGameInstanceMods;
#endif
    }

    public InstanceInfo(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;

        UpdateUI();
    }

    public bool IsEdit { get; set; }
    public VersionConfig VersionInfo { get; set; }

    public void UpdateUI()
    {
        Task.Run(() =>
        {
            IsEdit = false;

            Dispatcher.UIThread.Invoke(() =>
            {
                InstanceName.Text = VersionInfo.Info.VersionName;

                if (VersionInfo.Config == null)
                    VersionInfo.Config = new VersionConfig.VersionConfigEntry();

                InstanceArgs.Text = VersionInfo.Config.OtherCommand;
                InstanceConsole.IsChecked = VersionInfo.Config.IsConsole;
                InstanceEdit.IsChecked = VersionInfo.Config.IsEditModel;
                InstanceMod.IsChecked = VersionInfo.Config.IsModes;
                InstanceIsolated.IsChecked = VersionInfo.Config.IsVersionIsolated;
                InstanceDetailedLogs.IsChecked = VersionInfo.Config.IsDetailedLog;
            });

            Thread.Sleep(500);
            IsEdit = true;
        });
    }

    private void TextTypeConfig_OnChanged(object? sender, TextChangedEventArgs e)
    {
        if (IsEdit)
        {
            if (string.IsNullOrEmpty(InstanceName.Text))
                VersionInfo.Info.VersionName = Path.GetFileName(VersionInfo.VersionPath);
            else VersionInfo.Info.VersionName = InstanceName.Text;

            VersionInfo.Config.OtherCommand = InstanceArgs.Text;
            GameInfoHelper.SaveVersionConfig(VersionInfo);
        }
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

            GameInfoHelper.SaveVersionConfig(VersionInfo);
        }
    }
}