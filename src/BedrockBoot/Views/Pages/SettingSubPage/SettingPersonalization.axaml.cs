using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingPersonalizationPages;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingPersonalization : UserControl
{
    public SettingPersonalization()
    {
        InitializeComponent();
        MainSettingPage.SettingBreadcrumbBar.SetItems(new List<BreadcrumbItemInfo>()
        {
            new()
            {
                ItemName = "个性化"
            }
        });

#if RELEASE
        SetBackground.IsEnabled = GlobalModel.FunctionOption.IsEnableSettingBackground;
        SetColor.IsEnabled = GlobalModel.FunctionOption.IsEnableSettingColor;
        HomePanel.IsVisible = GlobalModel.FunctionOption.IsEnableSettingPersonalizationHome;
#endif
    }

    private void SetColor_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigationFrame.NavigateTo(new PersonalizationColor());
    }

    private void SetBackground_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigationFrame.NavigateTo(new PersonalizationBackground());
    }

    private void SetHome_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigationFrame.NavigateTo(new PersonalizationHome());
    }
}