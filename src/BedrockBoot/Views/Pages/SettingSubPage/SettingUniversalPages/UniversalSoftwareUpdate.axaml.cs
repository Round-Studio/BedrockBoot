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
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using BedrockBoot.Standard.Enum;
using BedrockBoot.Core.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Standard.Enum.Config;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.OtherPage;
using BedrockBoot.Views.Pages.OtherPage.BuildRelease;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using AboutReleaseNotes = BedrockBoot.Views.Pages.OtherPage.AboutPages.AboutReleaseNotes;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;

public partial class UniversalSoftwareUpdate : ISettingPage
{
    public bool IsEdit;

    public UniversalSoftwareUpdate()
    {
        InitializeComponent();
        IsAutoCheckUpdate.IsChecked = GlobalModel.Config.Data.IsAutoCheckUpdate;
        UpdateTypeBox.SelectedIndex = (int)GlobalModel.Config.Data.UpdateType;

        // 版本描述：直接显示版本号，或拼接“当前版本：”
        VersionCard.Description = Models.Global.GlobalModel.BodyVersion;

        // PreLoad.NET 组件本地版本
        PreLoadComponentCard.Description = PreLoadDependencyHelper.GetLocalVersion()
            ?? I18nManager.Instance["Settings.Universal.Update.GameComponent.PreLoad.NotInstalled"];

        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "Setting.Universal.Breadcrumb.Root",
                ItemClickAction = info =>
                    MainSettingPage.NavigateTo(new SettingUniversal())
            },
            new()
            {
                ItemName = "Setting.Universal.SoftwareUpdate.Title"
            }
        };

        // 联机依赖组件仅 Windows 提供，Linux 下隐藏入口
#if LINUX
        DependencyUpdateBtn.IsVisible = false;
#endif

        IsEdit = true;
    }

    private void DependencyUpdateBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new UniversalDependencyUpdate());
    }

    private void IsAutoCheckUpdate_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsAutoCheckUpdate = IsAutoCheckUpdate.IsChecked ?? false;
            GlobalModel.Config.Save();
        }
    }

    private void UpdateTypeBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.UpdateType = (UpdateType)UpdateTypeBox.SelectedIndex;
            GlobalModel.Config.Save();
        }
    }

    private void ReleaseNotesBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new AboutReleaseNotes());
    }

    private async void CheckUpdateBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        CheckUpdateBtn.IsEnabled = false;

        // 替换为等待状态
        CheckUpdateBtn.Content = new ProgressRing
        {
            Width = 24,
            Height = 24,
            Background = Brushes.Transparent
        };

        await MainPage.Update(true);

        CheckUpdateBtn.IsEnabled = true;
        // 恢复按钮文本
        CheckUpdateBtn.Content = I18nManager.Instance["Setting.Universal.SoftwareUpdate.CheckUpdateAction"];
    }

    private void GitCommitsBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new ChangLogPage());
    }

    private async void PreLoadCheckBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        PreLoadCheckBtn.IsEnabled = false;

        // 替换为等待状态
        PreLoadCheckBtn.Content = new ProgressRing
        {
            Width = 24,
            Height = 24,
            Background = Brushes.Transparent
        };

        try
        {
            // 与启动检查、启动器更新前检查共用同一条通道顺序：CNB → GitHub API → GitHub version 文件
            var remote = await Task.Run(() => PreLoadDependencyHelper.GetRemoteVersionAsync());
            var local = PreLoadDependencyHelper.GetLocalVersion();
            var notInstalled = I18nManager.Instance["Settings.Universal.Update.GameComponent.PreLoad.NotInstalled"];

            if (remote == null)
            {
                ShowPreLoadNotice(I18nManager.Instance["Settings.Universal.Update.GameComponent.PreLoad.CheckFailed"]);
                return;
            }

            PreLoadComponentCard.Description = $"{local ?? notInstalled} → {remote.Tag}";

            if (string.Equals(MultiplayerDependencyHelper.NormalizeVersion(local),
                    MultiplayerDependencyHelper.NormalizeVersion(remote.Tag),
                    StringComparison.OrdinalIgnoreCase))
            {
                ShowPreLoadNotice(I18nManager.Instance["Settings.Universal.Update.GameComponent.PreLoad.Latest"]);
            }
            else
            {
                // 有更新直接走后台下载任务项，与启动时自动检查共用同一入口
                PreLoadDependencyHelper.StartDownloadInBackground(remote);
            }
        }
        catch (Exception ex)
        {
            // 私有仓库未登录 GitHub 时会走到这里
            Console.WriteLine($@"PreLoad.NET 检查更新失败: {ex}");
            ShowPreLoadNotice(
                I18nManager.Instance["Settings.Universal.Update.GameComponent.PreLoad.CheckFailed"] + $"\n{ex.Message}");
        }
        finally
        {
            PreLoadCheckBtn.IsEnabled = true;
            PreLoadCheckBtn.Content = I18nManager.Instance["Settings.Universal.Update.GameComponent.PreLoad.Check"];
        }
    }

    private void ShowPreLoadNotice(string message)
    {
        Models.Global.GlobalModel.MainWindow?.Notice.AddNotice(new NoticeInfo
        {
            Title = I18nManager.Instance["Task.PreLoad.Update.Title"],
            Message = message,
            NoticeType = NoticeType.Info
        });
    }
}