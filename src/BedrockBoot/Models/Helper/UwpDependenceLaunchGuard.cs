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
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Views.DialogContent;
using CoreGlobal = BedrockBoot.Core.Global.GlobalModel;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
#if WINDOWS
using BedrockBoot.Windows.Models.Helper.Uwp;
#endif

namespace BedrockBoot.Models.Helper;

/// <summary>
/// 游戏启动前的 UWP 依赖检测守卫。
/// 启动器启动时不再检测 UWP 依赖，仅在启动游戏前检测一次，
/// 缺失依赖时由用户选择：自动补全后启动 / 仍要启动 / 取消启动。
/// </summary>
public static class UwpDependenceLaunchGuard
{
    /// <summary>
    /// 在启动游戏前调用。返回 true 表示继续启动，false 表示用户取消启动。
    /// </summary>
    public static async Task<bool> CheckBeforeLaunchAsync()
    {
#if WINDOWS
        var depList = await GetMissingDepsIfEnabledAsync();
        if (depList == null)
            return true;

        Console.WriteLine($@"启动游戏前检测到 {depList.Count} 个 UWP 依赖未安装。");
        return await ShowChoiceDialogAsync(depList);
#else
        return true;
#endif
    }

    /// <summary>
    /// 若开关开启且存在缺失的 UWP 依赖，返回缺失列表；否则返回 null（无需处理）。
    /// </summary>
    public static async Task<List<(string Name, string Version)>?> GetMissingDepsIfEnabledAsync()
    {
#if WINDOWS
        if (!CoreGlobal.Config.Data.IsCheckUwpDependenceAtLaunch)
            return null;

        var depList = await Task.Run(UwpDependencyChecker.GetMissingDependencies);
        if (depList.Count == 0)
            return null;

        return depList;
#else
        return null;
#endif
    }

#if WINDOWS
    private static Task<bool> ShowChoiceDialogAsync(List<(string Name, string Version)> depList)
    {
        var tcs = new TaskCompletionSource<bool>();

        Dispatcher.UIThread.Invoke(() =>
        {
            try
            {
                DialogHost.Show(new DialogInfo
                {
                    Title = I18nManager.Instance["LaunchWindow.Status.UWP.Missing.Title"],
                    Content = string.Format(
                        I18nManager.Instance["LaunchWindow.Status.UWP.Missing.Content"],
                        string.Join("\n", depList.Select(d => d.Name))),
                    PrimaryButtonText = I18nManager.Instance["LaunchWindow.Status.UWP.Fix"],
                    SecondaryButtonText = I18nManager.Instance["LaunchWindow.Status.UWP.LaunchAnyway"],
                    CloseButtonText = I18nManager.Instance["Shared.Action.Cancel"],
                    AccountButton = DialogButtons.PrimaryButton,
                    PrimaryAction = () =>
                    {
                        var content = new DialogDownloadUwpDependenceContent(depList);
                        content.AllCompleted = () => tcs.TrySetResult(true);
                        DialogHost.Show(new DialogInfo
                        {
                            Title = I18nManager.Instance["LaunchWindow.Status.UWP.Download.Title"],
                            Content = content
                        });
                    },
                    SecondaryAction = () => tcs.TrySetResult(true),
                    CloseAction = () => tcs.TrySetResult(false)
                });
            }
            catch (Exception ex)
            {
                // 对话框宿主不可用时（如无主窗口场景）不阻断启动
                Console.WriteLine($@"UWP 依赖检测对话框显示失败：{ex.Message}");
                tcs.TrySetResult(true);
            }
        });

        return tcs.Task;
    }
#endif
}
