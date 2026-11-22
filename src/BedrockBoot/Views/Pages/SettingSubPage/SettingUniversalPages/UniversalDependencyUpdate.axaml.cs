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
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.I18n;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.Helper;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;

/// <summary>
/// 设置 > 通用 > 软件更新 > 依赖更新。
/// 展示联机组件（EasyTier / GravityCone）已安装版本，检查并安装更新。
/// 仅 Windows 提供，Linux 下入口被隐藏。
/// </summary>
public partial class UniversalDependencyUpdate : ISettingPage
{
    public UniversalDependencyUpdate()
    {
        InitializeComponent();

        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Universal.Breadcrumb.Root"],
                ItemClickAction = info => MainSettingPage.NavigateTo(new SettingUniversal())
            },
            new()
            {
                ItemName = I18nManager.Instance["Setting.Universal.SoftwareUpdate.Title"],
                ItemClickAction = info => MainSettingPage.NavigateTo(new UniversalSoftwareUpdate())
            },
            new()
            {
                ItemName = I18nManager.Instance["Settings.Universal.Update.Dependency.Title"]
            }
        };

        RefreshLocalVersions();
    }

    /// <summary>展示本地已安装版本（不访问网络）</summary>
    private void RefreshLocalVersions()
    {
        EasyTierCard.Description = FormatLocal(
            MultiplayerDependencyHelper.IsEasyTierInstalled(),
            MultiplayerDependencyHelper.GetLocalVersion(MultiplayerDependencyHelper.EasyTierVersionFile));

        GravityConeCard.Description = FormatLocal(
            MultiplayerDependencyHelper.IsGravityConeInstalled(),
            MultiplayerDependencyHelper.GetLocalVersion(MultiplayerDependencyHelper.GravityConeVersionFile));

        EasyTierLatestText.Text = string.Empty;
        GravityConeLatestText.Text = string.Empty;
    }

    private static string FormatLocal(bool installed, string? version)
    {
        if (!installed) return I18nManager.Instance["Settings.Universal.Dependency.Status.NotInstalled"];
        return string.IsNullOrEmpty(version)
            ? I18nManager.Instance["Settings.Universal.Dependency.Status.MissingVersion"]
            : string.Format(I18nManager.Instance["Settings.Universal.Dependency.Status.InstalledVersion"], version);
    }

    private async void CheckUpdateBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        CheckUpdateBtn.IsEnabled = false;
        var oldContent = CheckUpdateBtn.Content;
        CheckUpdateBtn.Content = new ProgressRing
        {
            Width = 24,
            Height = 24,
            Background = Brushes.Transparent
        };

        try
        {
            var (easyTier, gravityCone) = await Task.Run(MultiplayerDependencyHelper.CheckUpdatesAsync);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                EasyTierLatestText.Text = string.Format(I18nManager.Instance["Settings.Universal.Dependency.Status.LatestVersion"], easyTier.LatestVersion);
                GravityConeLatestText.Text = string.Format(I18nManager.Instance["Settings.Universal.Dependency.Status.LatestVersion"], gravityCone.LatestVersion);

                var anyNotInstalled = !easyTier.IsInstalled || !gravityCone.IsInstalled;
                var anyUpdate = easyTier.HasUpdate || gravityCone.HasUpdate;

                if (anyNotInstalled)
                {
                    UpdateCard.IsVisible = true;
                    UpdateCard.Header = I18nManager.Instance["Settings.Universal.Dependency.Install.Title"];
                    UpdateCard.Description = I18nManager.Instance["Settings.Universal.Dependency.Install.Desc"];
                }
                else if (anyUpdate)
                {
                    UpdateCard.IsVisible = true;
                    UpdateCard.Header = I18nManager.Instance["Settings.Universal.Dependency.Update.Title"];
                    UpdateCard.Description = I18nManager.Instance["Settings.Universal.Dependency.Update.AvailableDesc"];
                }
                else
                {
                    UpdateCard.IsVisible = false;
                    Models.Global.GlobalModel.MainWindow?.Notice?.AddNotice(new NoticeInfo
                    {
                        Title = I18nManager.Instance["Settings.Universal.Dependency.Notice.Title"],
                        Message = I18nManager.Instance["Settings.Universal.Dependency.Notice.Latest"],
                        NoticeType = NoticeType.Success
                    });
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"检查依赖更新失败: {ex}");
            await GitHubErrorDialog.ShowAsync(ex, I18nManager.Instance["GitHub.Error.CheckUpdate"]);
        }
        finally
        {
            CheckUpdateBtn.IsEnabled = true;
            CheckUpdateBtn.Content = oldContent;
        }
    }

    private void UpdateBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // navigateAfterComplete: false — 从设置页更新完成后不去重新初始化联机页
        var dialog = new DialogDownloadMultiPlayerDependenceContent(false)
        {
            Completed = () =>
            {
                RefreshLocalVersions();
                UpdateCard.IsVisible = false;
                Models.Global.GlobalModel.MainWindow?.Notice?.AddNotice(new NoticeInfo
                {
                    Title = I18nManager.Instance["Settings.Universal.Dependency.Notice.Title"],
                    Message = I18nManager.Instance["Settings.Universal.Dependency.Notice.Completed"],
                    NoticeType = NoticeType.Success
                });
            }
        };

        DialogHost.Show(new DialogInfo
        {
            Title = I18nManager.Instance["Settings.Universal.Dependency.Update.DialogTitle"],
            Content = dialog
        });
    }
}
