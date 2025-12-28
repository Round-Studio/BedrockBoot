using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingUniversal : UserControl
{
    public SettingUniversal()
    {
        InitializeComponent();
        MainSettingPage.SettingBreadcrumbBar.SetItems(new List<BreadcrumbItemInfo>()
        {
            new ()
            {
                ItemName = "通用"
            }
        });

#if RELEASE
        PluginPanel.IsVisible = GlobalModel.FunctionOption.IsEnablePlugin;
#endif
    }

    private void SoftwareUpdate_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigationFrame.NavigateTo(new UniversalSoftwareUpdate());
    }

    private void DebugBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigationFrame.NavigateTo(new UniversalDebug());
    }
}