using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.OtherPage;

public partial class AboutOpenSource : UserControl
{
    public AboutOpenSource()
    {
        InitializeComponent();
        MainSettingPage.SettingBreadcrumbBar.SetItems(new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "关于我们",
                ItemClickAction = info => MainSettingPage.NavigationFrame.NavigateTo(new AboutPage())
            },
            new()
            {
                ItemName = "第三方组件库"
            }
        });

        var type = typeof(AppBuilder);
        var assembly = type.Assembly;
        var version = assembly.GetName().Version;

        AvaloniaVersion.Text = $"Version {version}";
    }
}