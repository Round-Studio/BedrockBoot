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
    ///     情形 2 拿不到欢迎语时<b>不设值</b>，让 <c>{DynamicResource Home.Title}</c> 生效——
    ///     这样"默认欢迎语"始终只有一条本地化来源，不再靠字面判等区分默认与自定义。
    /// </remarks>
    private void ApplyHomeTitle()
    {
        var manifest = Models.Global.GlobalModel.CustomManifest;
        var pageHome = manifest?.PageTitles?.PageHome;
        if (string.IsNullOrWhiteSpace(pageHome))
            return;

        var title = pageHome.Contains("{{random}}", StringComparison.Ordinal)
            ? ResolveRandomTitle(manifest!, pageHome)
            : pageHome;

        if (title is null)
            return;

        LineTextBlock.Text = title;
    }

    /// <summary>把 <c>{{random}}</c> 替换成欢迎语；拿不到欢迎语时返回 <c>null</c>。</summary>
    private static string? ResolveRandomTitle(CustomManifest manifest, string pageHome)
    {
        // 用户自定义的欢迎语原样使用：那是用户自己的文案，不该被翻译。
        var custom = manifest.RandomStr;
        if (custom is { Count: > 0 })
            return pageHome.Replace("{{random}}", custom[Random.Shared.Next(custom.Count)]);

        // 没有专属欢迎语集合的语言（ja_JP / ru_RU / zh_TW …）交给 Home.Title 键：
        // 那条键各语言本来就有译文，硬塞英文欢迎语反而更差。
        var welcome = WelcomeMessages.Pick(Localizer.Culture);
        return welcome is null ? null : pageHome.Replace("{{random}}", welcome);
    }

    public void UpdateHome()
    {
        MainGrid.Children.Clear();

        switch (GlobalModel.Config.Data.HomeConfig.HomeType)
        {
            case HomeType.None:
                break;
            case HomeType.News:
                MainGrid.Children.Add(new GameUpdateNewsWidget());
                break;
            case HomeType.Widgets:
                DesktopWorkspace = new DesktopWorkspace();
                if (File.Exists(PathsList.WidgetsConfigPath))
                    DesktopWorkspace.ImportLayout(File.ReadAllText(PathsList.WidgetsConfigPath));
                DesktopWorkspace.AddWidgetCallOn += (sender, args) =>
                {
                    Models.Global.GlobalModel.MainWindow.OpenDraw(new DrawAddWidgetContent(), I18nManager.Instance["Widget.Previewer.AddWidget"]);
                };
                DesktopWorkspace.LayoutChanged += (sender, args) =>
                {
                    var json = DesktopWorkspace.ExportLayout();
                    File.WriteAllText(PathsList.WidgetsConfigPath, json);
                };
                MainGrid.Children.Add(DesktopWorkspace);
                break;
        }
    }
}