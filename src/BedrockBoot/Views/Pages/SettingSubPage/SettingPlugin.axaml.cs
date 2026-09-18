/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

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