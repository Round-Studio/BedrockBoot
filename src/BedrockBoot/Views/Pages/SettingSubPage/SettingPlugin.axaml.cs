using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Interface;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingPluginPages;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingPlugin : ISettingPage
{
    public SettingPlugin()
    {
        InitializeComponent();
        
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "插件"
            }
        };
    }

    private void PluginManager_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new PluginManager());
    }
}