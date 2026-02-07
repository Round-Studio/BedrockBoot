using System.Collections.Generic;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Pages.OtherPage;

public partial class AboutPage : UserControl
{
    public AboutPage()
    {
        InitializeComponent();
        MainSettingPage.SettingBreadcrumbBar.SetItems(new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "关于我们"
            }
        });
        VersionCard.Description = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        PowerByTextBlock.Text = $"Power By: Avalonia {typeof(AppBuilder).Assembly.GetName().Version}";
    }

    private async void CheckUpdateBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        CheckUpdateBtn.IsEnabled = false;
        CheckUpdateBtn.Content = new ProgressRing
        {
            Width = 24,
            Height = 24,
            Background = Brushes.Transparent
        };
        await MainPage.Update(true);
        CheckUpdateBtn.IsEnabled = true;
        CheckUpdateBtn.Content = "检查更新";
    }

    private void OpenSourceBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigationFrame.NavigateTo(new AboutOpenSource());
    }
}