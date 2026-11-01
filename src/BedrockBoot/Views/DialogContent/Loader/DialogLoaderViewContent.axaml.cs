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
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using BedrockBoot.Standard.Interface.ModLoader;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent.Loader;

public partial class DialogLoaderViewContent : UserControl
{
    private static I18nManager i18n => I18nManager.Instance;

    private readonly IModsLoader _loader;

    public DialogLoaderViewContent()
    {
        InitializeComponent();
    }

    public DialogLoaderViewContent(IModsLoader loader) : this()
    {
        _loader = loader;
        _ = UpdateUi();
    }

    /// <summary>
    ///     使用统一的加载器详细信息模板弹出对话框
    /// </summary>
    public static void Show(IModsLoader loader)
    {
        _ = ShowCore(loader);
    }

    private static async Task ShowCore(IModsLoader loader)
    {
        var info = new DialogInfo
        {
            Title = i18n["Dialog.LoaderView.Title"],
            Content = new DialogLoaderViewContent(loader),
            CloseButtonText = i18n["MainWindow.Common.Confirm"]
        };

        // DialogContent 只在构建时读取一次按钮配置，
        // 因此适用性检查必须在弹出对话框之前完成
        if (!loader.IsInstalled())
        {
            try
            {
                if (await loader.ApplicableInstance())
                {
                    info.PrimaryButtonText = i18n["Dialog.LoaderView.Action.Install"];
                    info.PrimaryAction = () =>
                    {
                        try
                        {
                            loader.Install();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($@"从加载器详情发起安装失败: {ex.Message}");
                        }
                    };
                    info.AccountButton = DialogButtons.PrimaryButton;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"检查加载器适用性失败: {ex.Message}");
            }
        }

        DialogHost.Show(info);
    }

    private async Task UpdateUi()
    {
        LoaderNameText.Text = _loader.LoaderName;
        LoaderDescriptionText.Text = _loader.LoaderDescription;
        IntroText.Text = _loader.LoaderIntro;
        LoaderLogo.ImageUrl = _loader.IconUri;

        StatusTitleText.Text = i18n["Dialog.LoaderView.Status.Title"];
        ModsCountTitleText.Text = i18n["Dialog.LoaderView.Mods.Count"];
        IntroTitleText.Text = i18n["Dialog.LoaderView.Intro.Title"];

        if (string.IsNullOrEmpty(_loader.LoaderWebsite))
        {
            WebsiteBtn.IsVisible = false;
        }
        else
        {
            WebsiteBtn.IsVisible = true;
            WebsiteBtn.Content = i18n["Dialog.LoaderView.Website"];
        }

        var installed = _loader.IsInstalled();
        InstallStatusLabel.Text = installed
            ? i18n["Dialog.LoaderView.Status.Installed"]
            : i18n["Dialog.LoaderView.Status.NotInstalled"];
        if (installed)
            InstallStatusLabel.ClearValue(BackgroundProperty);
        else
            InstallStatusLabel.Background = new SolidColorBrush(Color.FromRgb(139, 0, 0));

        _ = UpdateModsCountAsync();
    }

    private async Task UpdateModsCountAsync()
    {
        ModsCountRing.IsVisible = true;
        ModsCountText.IsVisible = false;

        var count = await Task.Run(() =>
        {
            try
            {
                return _loader.ModsManager?.GetAllMods().Count ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"统计加载器模组数量失败: {ex.Message}");
                return 0;
            }
        });

        ModsCountRing.IsVisible = false;
        ModsCountText.Text = count.ToString();
        ModsCountText.IsVisible = true;
    }

    private void WebsiteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_loader.LoaderWebsite)) return;

        try
        {
            Process.Start(new ProcessStartInfo(_loader.LoaderWebsite) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"打开加载器官网失败: {ex.Message}");
        }
    }
}
