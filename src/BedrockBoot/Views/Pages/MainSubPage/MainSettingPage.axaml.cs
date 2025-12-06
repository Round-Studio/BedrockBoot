using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.Views.Pages.SettingSubPage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation.Breadcrumb;
using Round.SDK.Helper;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainSettingPage : BedrockBootPage
{
    public static NavigationFrame NavigationFrame;
    public static BreadcrumbBar SettingBreadcrumbBar;
    public MainSettingPage()
    {
        InitializeComponent();
        BuildTime.Text =
            $"Build.2.{((DateTime)(CheckVersion.GetBuildTimestamp(Assembly.GetExecutingAssembly()))).ToString("yy.MMdd.HHmmss")}";
        SettingBreadcrumbBar = this.BreadcrumbBar;
        this.BreadcrumbBar.RootItemClick = () =>
            SettingFrame.NavigateTo(new SettingNavigation());

        NavigationFrame = SettingFrame;
        
        SettingFrame.NavigateTo(new SettingNavigation());
    }
}