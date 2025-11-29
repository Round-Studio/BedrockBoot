using System.Collections.Generic;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.OtherPage;

public partial class AboutPage : UserControl
{
    public AboutPage()
    {
        InitializeComponent();
        MainSettingPage.SettingBreadcrumbBar.SetItems(new List<BreadcrumbItemInfo>()
        {
            new ()
            {
                ItemName = "关于我们",
                ItemClickAction = (info) => MainSettingPage.NavigationFrame.NavigateTo(new AboutPage())
            }
        });
        VersionCard.Description = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
}