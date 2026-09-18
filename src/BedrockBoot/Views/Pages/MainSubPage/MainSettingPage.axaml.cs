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

using BedrockBoot.Base.Entry;
using BedrockBoot.Interface;
using BedrockBoot.Views.Pages.SettingSubPage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation.Breadcrumb;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainSettingPage : BedrockBootPage
{
    private static NavigationFrame NavigationFrame;
    private static BreadcrumbBar SettingBreadcrumbBar;

    public MainSettingPage()
    {
        InitializeComponent();
        BreadcrumbBar.RootItem = I18nManager.Instance["MainPage.Nav.Setting"];
        SettingBreadcrumbBar = BreadcrumbBar;
        BreadcrumbBar.RootItemClick = () =>
            SettingFrame.NavigateTo(new SettingNavigation());

        NavigationFrame = SettingFrame;

        SettingFrame.NavigateTo(new SettingNavigation());
    }

    public static void NavigateTo(ISettingPage page)
    {
        NavigationFrame.NavigateTo(page);
        SettingBreadcrumbBar.SetItems(page.BreadcrumbItem);
        SettingBreadcrumbBar.RootItem = I18nManager.Instance["MainPage.Nav.Setting"];
    }
}