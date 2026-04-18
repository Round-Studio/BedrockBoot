using System.Collections.Generic;
using Avalonia.Interactivity;
using BedrockBoot.Interface;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingPluginPages;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using Round.SDK.Plugin.BedrockBoot.Register;

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

        PluginSetting.IsVisible = RegisterService.API.SettingItems.Count > 0;
        RegisterService.API.SettingItems.ForEach(it =>
        {
            var item = new SettingCard
            {
                Header = it.Header,
                Description = it.Description,
                Glyph = it.IconSource,
                IsClickable = true,
                IsFontIcon = it.IsUseFontIcon,
                ImageIcon = !it.IsUseFontIcon ? null : ImageLoader.LoadIconAsync(it.IconSource).Result
            };
            item.Click += (sender, args) => MainSettingPage.NavigateTo((it.Page as ISettingPage)!);
            PluginSetting.Children.Add(item);
        });
    }

    private void PluginManager_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new PluginManager());
    }
}