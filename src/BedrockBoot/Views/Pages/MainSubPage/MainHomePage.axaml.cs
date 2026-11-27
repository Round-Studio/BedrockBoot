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

using System;
using System.ComponentModel;
using System.IO;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Localization;
using BedrockBoot.Models.Account.Github;
using BedrockBoot.Standard.Entity;
using BedrockBoot.Standard.Entity.Config;
using BedrockBoot.Standard.Entity.Info;
using BedrockBoot.Standard.Entity.Manifest;
using BedrockBoot.Standard.Enum;
using BedrockBoot.Standard.Enum.Type;
using BedrockBoot.Models.Global;
using BedrockBoot.Standard.Enum.Config;
using BedrockBoot.Style.Widgets;
using BedrockBoot.Views.Control.Widgets;
using BedrockBoot.Views.Control.Widgets.DesktopWidgets;
using BedrockBoot.Views.DrawContent;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainHomePage : BedrockBootPage
{
    public DesktopWorkspace DesktopWorkspace;

    /// <summary>清单里代表"随机欢迎语"的令牌。</summary>
    private const string RandomToken = "{{random}}";

    /// <summary>
    ///     旧版清单里写死的默认主页标题。
    /// </summary>
    /// <remarks>
    ///     这些值是"默认欢迎语"的历史形态，不是用户的自定义文案，
    ///     因此要按默认处理，否则老清单会永远卡在中文。
    /// </remarks>
    private static readonly string[] LegacyDefaultTitles = ["您好，欢迎回来", "你好，欢迎回来"];

    public MainHomePage()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            UpdateHome();
            ApplyHomeTitle();

            // 标题是随语言切换的随机文案，{DynamicResource} 承载不了随机，
            // 因此自己盯着语言变化重算一次。
            I18nManager.Instance.PropertyChanged -= OnI18nPropertyChanged;
            I18nManager.Instance.PropertyChanged += OnI18nPropertyChanged;
        };

        Unloaded += (_, _) => I18nManager.Instance.PropertyChanged -= OnI18nPropertyChanged;
    }

    private void OnI18nPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // I18nManager 只在切语言/重建键表时发 "Item[]"，其余属性变更与标题无关。
        if (e.PropertyName is null or "Item[]")
            ApplyHomeTitle();
    }

    /// <summary>
    ///     决定主页标题显示什么。
    /// </summary>
    /// <remarks>
    ///     三种情形：
    ///     <list type="number">
    ///         <item>清单里的 <c>pageHome</c> 含 <c>{{random}}</c> 且用户写了自定义欢迎语 → 原样显示用户的文案（不翻译）。</item>
    ///         <item>含 <c>{{random}}</c> 但用户没写 → 用当前语言的内置欢迎语（<see cref="WelcomeMessages" />，带频率）。</item>
    ///         <item>不含令牌 → 视为用户自定义的整句标题，原样显示。</item>
    ///     </list>
    ///     空白与<b>历史默认标题</b>（旧版清单里写死的中文默认值）都按情形 2 处理。
    ///     两条路都拿不到欢迎语时回落到 <c>Home.Title</c> 键。
    /// </remarks>
    private void ApplyHomeTitle()
    {
        var manifest = Models.Global.GlobalModel.CustomManifest;
        var pageHome = manifest?.PageTitles?.PageHome;

        // 旧清单把默认欢迎语写成了中文字面量；不识别它就会被当成"用户自定义标题"，
        // 于是无论切到哪门语言都一直显示中文。
        // 旧清单的 randomStr 同样是一整串中文，因此这里连自定义列表一起忽略，
        // 否则识别了 pageHome 还是会把中文随机回来。
        var isLegacyDefault = IsLegacyDefaultTitle(pageHome);
        if (isLegacyDefault)
            pageHome = RandomToken;

        var title = string.IsNullOrWhiteSpace(pageHome)
            ? null
            : pageHome.Contains(RandomToken, StringComparison.Ordinal)
                ? ResolveRandomTitle(manifest, pageHome, isLegacyDefault)
                : pageHome;

        // 没有可用标题时必须显式回落到 Home.Title：
        // Text 一旦被赋过本地值，XAML 上那个 {DynamicResource} 就已经被替换，
        // 清掉本地值也回不来（只会变成空标题），所以这里主动取当前语言的 Home.Title。
        // 切语言时本方法会重算，不会停在旧语言上。
        LineTextBlock.Text = title ?? I18nManager.Instance["Home.Title"];
    }

    /// <summary>旧版清单里写死的默认主页标题。</summary>
    private static bool IsLegacyDefaultTitle(string? pageHome) =>
        pageHome is not null && Array.IndexOf(LegacyDefaultTitles, pageHome) >= 0;

    /// <summary>把 <c>{{random}}</c> 替换成欢迎语；拿不到欢迎语时返回 <c>null</c>。</summary>
    /// <param name="manifest">用户清单，可能为 <c>null</c>。</param>
    /// <param name="pageHome">含 <c>{{random}}</c> 的模板。</param>
    /// <param name="ignoreCustom">为 <c>true</c> 时不使用清单里的自定义欢迎语（旧版默认清单）。</param>
    private static string? ResolveRandomTitle(CustomManifest? manifest, string pageHome, bool ignoreCustom)
    {
        // 用户自定义的欢迎语原样使用：那是用户自己的文案，不该被翻译。
        var custom = manifest?.RandomStr;
        if (!ignoreCustom && custom is { Count: > 0 })
            return pageHome.Replace(RandomToken, custom[Random.Shared.Next(custom.Count)]);

        // 没有专属欢迎语集合的语言（ja_JP / ru_RU / zh_TW …）交给 Home.Title 键：
        // 那条键各语言本来就有译文，硬塞英文欢迎语反而更差。
        var welcome = WelcomeMessages.Pick(Localizer.Culture);
        return welcome is null ? null : pageHome.Replace(RandomToken, welcome);
    }

    public void UpdateHome()
    {
        MainGrid.Children.Clear();

        switch (GlobalModel.Config.Data.HomeConfig.HomeType)
        {
            case HomeType.None:
                PendingOpenAddWidgetDrawer = false;
                break;
            case HomeType.News:
                PendingOpenAddWidgetDrawer = false;
                MainGrid.Children.Add(new GameUpdateNewsWidget());
                break;
            case HomeType.Widgets:
                // 局部变量固定住这一份工作区：切页后字段会指向新实例，
                // 事件回调里再用字段就可能把别的页面的布局写进配置文件。
                var workspace = new DesktopWorkspace();
                DesktopWorkspace = workspace;

                if (File.Exists(PathsList.WidgetsConfigPath))
                    workspace.ImportLayout(File.ReadAllText(PathsList.WidgetsConfigPath));

                workspace.AddWidgetCallOn += (sender, args) => OpenAddWidgetDrawer();
                workspace.LayoutChanged += (sender, args) =>
                {
                    var json = workspace.ExportLayout();
                    File.WriteAllText(PathsList.WidgetsConfigPath, json);
                };
                MainGrid.Children.Add(workspace);

                // 从"设置 - 个性化 - 主页"跳过来时，把添加面板一并打开
                if (PendingOpenAddWidgetDrawer)
                {
                    PendingOpenAddWidgetDrawer = false;
                    Dispatcher.UIThread.Post(OpenAddWidgetDrawer, DispatcherPriority.Background);
                }

                break;
        }
    }

    /// <summary>
    ///     设置页要求"跳回主页并打开添加面板"时置位；主页加载后消费掉。
    /// </summary>
    public static bool PendingOpenAddWidgetDrawer { get; set; }

    /// <summary>打开"添加小部件"抽屉。</summary>
    public static void OpenAddWidgetDrawer()
    {
        Models.Global.GlobalModel.MainWindow.OpenDraw(
            new DrawAddWidgetContent(),
            I18nManager.Instance["Widget.Previewer.AddWidget"]);
    }
}