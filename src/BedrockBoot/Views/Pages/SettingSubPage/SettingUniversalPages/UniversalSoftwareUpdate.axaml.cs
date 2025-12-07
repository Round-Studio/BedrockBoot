using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;

public partial class UniversalSoftwareUpdate : UserControl
{
    public UniversalSoftwareUpdate()
    {
        InitializeComponent();
        MainSettingPage.SettingBreadcrumbBar.SetItems(new List<BreadcrumbItemInfo>()
        {
            new()
            {
                ItemName = "通用",
                ItemClickAction = (info) =>
                    MainSettingPage.NavigationFrame.NavigateTo(new SettingUniversal())
            },
            new()
            {
                ItemName = "软件更新"
            }
        });
    }
}