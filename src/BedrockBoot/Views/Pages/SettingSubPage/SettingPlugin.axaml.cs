using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
	private ImageLoader _imageLoader = ImageLoader.Shared;

	public SettingPlugin()
    {
        InitializeComponent();

        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Settings.Nav.Plugin.Title"]
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
                IsFontIcon = it.IsUseFontIcon
            };
            item.Click += (sender, args) => MainSettingPage.NavigateTo((it.Page as ISettingPage)!);
            PluginSetting.Children.Add(item);

            // 图标异步加载，避免每个插件都在 UI 线程上阻塞一次解码
            if (it.IsUseFontIcon)
            {
                _ = LoadIconAsync(item, it.IconSource);
            }
        });
    }

    private async Task LoadIconAsync(SettingCard card, string iconSource)
    {
        try
        {
            var icon = await _imageLoader.LoadIconAsync(iconSource);
            if (icon != null) card.ImageIcon = icon;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"加载插件设置项图标失败: {ex.Message}");
        }
    }

    private void PluginManager_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new PluginManager());
    }
}