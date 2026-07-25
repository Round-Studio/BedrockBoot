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
	private ImageLoader _imageLoader = new ImageLoader();
	protected override void OnUnloaded(RoutedEventArgs e)
	{
		base.OnUnloaded(e);
		_imageLoader.Dispose();
	}

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
                IsFontIcon = it.IsUseFontIcon,
                ImageIcon = !it.IsUseFontIcon ? null : _imageLoader.LoadIconAsync(it.IconSource).Result
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