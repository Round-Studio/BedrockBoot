using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.OtherPage;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingNavigation : UserControl
{
    public SettingNavigation()
    {
        InitializeComponent();

#if RELEASE
        SetPersonalization.IsEnabled = BedrockBoot.Models.Global.GlobalModel.FunctionOption.IsEnableSettingPersonalization;
#endif
    }

    private void AboutUs_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new AboutPage());
    }

    private void SetDownload_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new SettingDownload());
    }

    private void Universal_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new SettingUniversal());
    }

    private void SetPersonalization_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new SettingPersonalization());
    }

    private void SetGame_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new SettingGame());
    }

    private void Plugin_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new SettingPlugin());
    }
}