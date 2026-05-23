using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Interface;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.OtherPage;

public partial class AboutReleaseNotes : ISettingPage
{
    public AboutReleaseNotes()
    {
        InitializeComponent();
        
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = i18n["AboutPage.Title"], // "关于我们"
                ItemClickAction = _ => MainSettingPage.NavigateTo(new AboutPage())
            },
            new()
            {
                ItemName = "发行说明"
            }
        };
    }
}