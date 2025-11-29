using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.OtherPage;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingNavigation : UserControl
{
    public SettingNavigation()
    {
        InitializeComponent();
    }

    private void AboutUs_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigationFrame.NavigateTo(new AboutPage());
    }
}