using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.OtherPage;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingNavigation : UserControl
{
    public SettingNavigation()
    {
        InitializeComponent();

#if RELEASE
        SetPersonalization.IsEnabled = GlobalModel.FunctionOption.IsEnableSettingPersonalization;
#endif
    }

    private void AboutUs_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigationFrame.NavigateTo(new AboutPage());
    }

    private void SetDownload_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigationFrame.NavigateTo(new SettingDownload());
    }

    private void Universal_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigationFrame.NavigateTo(new SettingUniversal());
    }

    private void SetPersonalization_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigationFrame.NavigateTo(new SettingPersonalization());
    }

    private void SetGame_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigationFrame.NavigateTo(new SettingGame());
    }
}