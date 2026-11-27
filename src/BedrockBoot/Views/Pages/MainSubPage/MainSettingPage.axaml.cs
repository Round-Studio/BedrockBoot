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
using System.Linq;
using Avalonia.Threading;
using BedrockBoot.Localization;
using BedrockBoot.Standard.Entity;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Views.Pages.SettingSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation.Breadcrumb;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainSettingPage : BedrockBootPage
{
    /// <summary>面包屑根项（"设置"）的本地化键。</summary>
    private const string RootItemKey = "MainPage.Nav.Setting";

    private static NavigationFrame NavigationFrame;
    public static BreadcrumbBar SettingBreadcrumbBar;

    /// <summary>当前显示的子页面；切语言时靠它重放面包屑。</summary>
    private static ISettingPage? CurrentPage;

    public MainSettingPage()
    {
        InitializeComponent();

        SettingBreadcrumbBar = BreadcrumbBar;
        BreadcrumbBar.RootItemClick = () =>
        {
            // 回分类列表不是 NavigateTo：这里要一并清掉当前子页面，
            // 否则之后切语言重放面包屑，会把已经离开的子页面条目又画回来。
            CurrentPage = null;
            SettingFrame.NavigateTo(new SettingNavigation());
            RefreshBreadcrumb();
        };

        NavigationFrame = SettingFrame;

        // 面包屑条目存的是键而不是文案，因此键表一变（切语言、退出/进入调试预览、
        // 缺翻译策略变更）就要重新解析一次：否则界面上其它文案（{DynamicResource}）
        // 都换了、面包屑还停在上一门语言。
        // 订阅的是 LocalizationContext.Changed 而不是 I18nManager.PropertyChanged，
        // 因为前者覆盖调试预览这类"表变了但语言没变"的情形。
        Loaded += (_, _) =>
        {
            LocalizationContext.Current.Changed -= OnTableChanged;
            LocalizationContext.Current.Changed += OnTableChanged;
        };
        Unloaded += (_, _) => LocalizationContext.Current.Changed -= OnTableChanged;

        // 顶部导航每次切到设置页都会新建本页实例，但 CurrentPage 是静态的、
        // 跨实例存活：不清掉的话，初始 RefreshBreadcrumb 会把上一个实例
        // 留在二级页时的条目又画回来（内容已是分类列表、面包屑却还是旧子页）。
        CurrentPage = null;
        SettingFrame.NavigateTo(new SettingNavigation());
        RefreshBreadcrumb();
    }

    public static void NavigateTo(ISettingPage page)
    {
        NavigationFrame.NavigateTo(page);
        CurrentPage = page;
        RefreshBreadcrumb();
    }

    private static void OnTableChanged(object? sender, LocalizationChangedEventArgs e)
    {
        // 键表重建一般发生在 UI 线程（切语言、热重载都已 marshalled），
        // 但来源注册可能在别的线程，统一兜一手。
        if (Dispatcher.UIThread.CheckAccess())
            RefreshBreadcrumb();
        else
            Dispatcher.UIThread.Post(RefreshBreadcrumb);
    }

    /// <summary>按当前语言重建面包屑：根项 + 当前子页面的条目。</summary>
    private static void RefreshBreadcrumb()
    {
        if (SettingBreadcrumbBar is null)
            return;

        SettingBreadcrumbBar.RootItem = I18nManager.Instance[RootItemKey];
        SettingBreadcrumbBar.SetItems(Localize(CurrentPage?.BreadcrumbItem));
    }

    /// <summary>把条目里的本地化键解析成当前语言的文案。</summary>
    /// <remarks>
    ///     键表里没有的键（用户名、产品名这类原样字符串）查串会原样返回，无需区分。
    ///     每次派生新实例：条目本身保留键，重复渲染才不会把已解析过的文案当键再解析一次。
    /// </remarks>
    private static List<BreadcrumbItemInfo> Localize(List<BreadcrumbItemInfo>? items)
    {
        if (items is null || items.Count == 0)
            return new List<BreadcrumbItemInfo>();

        return items.Select(item => new BreadcrumbItemInfo
        {
            ItemName = I18nManager.Instance[item.ItemName],
            ItemClickAction = item.ItemClickAction
        }).ToList();
    }
}
