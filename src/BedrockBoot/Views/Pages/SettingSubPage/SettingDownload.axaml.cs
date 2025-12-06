using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingDownload : UserControl
{
    public SettingDownload()
    {
        InitializeComponent();
        MainSettingPage.SettingBreadcrumbBar.SetItems(new List<BreadcrumbItemInfo>()
        {
            new ()
            {
                ItemName = "下载"
            }
        });
    }
}