using System.Collections.Generic;
using Avalonia.Interactivity;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;

public partial class GameLaunchCommand : ISettingPage
{
    public GameLaunchCommand()
    {
        InitializeComponent();

        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Game.Breadcrumb.Root"],
                ItemClickAction = s => MainSettingPage.NavigateTo(new SettingGame())
            },
            new()
            {
                ItemName = I18nManager.Instance["Settings.Game.LaunchCommand.Title"]
            }
        };

        // 从配置中还原各项设置
        var config = GlobalModel.Config.Data.LaunchCommandConfig;

        MainSwitch.IsChecked = config.IsEnable;
        PreLaunchInput.Text = config.PreLaunchCommand;
        PostExitInput.Text = config.PostExitCommand;
        WrapperInput.Text = config.WrapperCommand;
        WaitSwitch.IsChecked = config.IsWaitForPreLaunch;
        TimeoutNum.Value = config.PreLaunchTimeout;
        AbortSwitch.IsChecked = config.IsAbortOnPreLaunchFailure;

        // Windows 的游戏本体通过 UWP/GDK 包激活启动，没有可供包装的子进程命令行
#if WINDOWS
        WrapperCard.IsVisible = false;
        WrapperUnsupportedCard.IsVisible = true;
#endif

        UpdateUI();

        IsEdit = true;
    }

    /// <summary>根据总开关与等待开关的状态同步各卡片的可用性</summary>
    private void UpdateUI()
    {
        var isEnable = MainSwitch.IsChecked == true;

        PreLaunchCard.IsEnabled = isEnable;
        PostExitCard.IsEnabled = isEnable;
        WrapperCard.IsEnabled = isEnable;
        WaitCard.IsEnabled = isEnable;

        // 超时与失败中止仅在等待启动前命令时才有意义
        var isWait = isEnable && WaitSwitch.IsChecked == true;
        TimeoutCard.IsEnabled = isWait;
        AbortCard.IsEnabled = isWait;
    }

    private void MainSwitch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        UpdateUI();
        if (!IsEdit) return;

        GlobalModel.Config.Data.LaunchCommandConfig.IsEnable = MainSwitch.IsChecked == true;
        GlobalModel.Config.Save();
    }

    private void CommandInput_OnTextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        if (!IsEdit) return;

        var config = GlobalModel.Config.Data.LaunchCommandConfig;
        config.PreLaunchCommand = PreLaunchInput.Text ?? string.Empty;
        config.PostExitCommand = PostExitInput.Text ?? string.Empty;
        config.WrapperCommand = WrapperInput.Text ?? string.Empty;
        GlobalModel.Config.Save();
    }

    private void WaitSwitch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        UpdateUI();
        if (!IsEdit) return;

        GlobalModel.Config.Data.LaunchCommandConfig.IsWaitForPreLaunch = WaitSwitch.IsChecked == true;
        GlobalModel.Config.Save();
    }

    private void TimeoutNum_OnValueChanged(object? sender, RoutedEventArgs e)
    {
        if (!IsEdit) return;

        GlobalModel.Config.Data.LaunchCommandConfig.PreLaunchTimeout = (int)TimeoutNum.Value;
        GlobalModel.Config.Save();
    }

    private void AbortSwitch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (!IsEdit) return;

        GlobalModel.Config.Data.LaunchCommandConfig.IsAbortOnPreLaunchFailure = AbortSwitch.IsChecked == true;
        GlobalModel.Config.Save();
    }
}
