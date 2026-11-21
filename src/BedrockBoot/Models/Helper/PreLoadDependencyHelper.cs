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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Account.Github;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using Round.SDK.Entity;
using Octokit;

namespace BedrockBoot.Models.Helper;

/// <summary>
///     PreLoad.NET 组件的启动时更新检查。
///
///     <para>
///     组件不随启动器内嵌分发。每次启动强制检查独立仓库
///     Round-Studio/PreLoad.NET-Release 的最新 Release（不受 IsAutoCheckUpdate 控制），
///     版本不一致（含首次无记录的自动引导）时创建后台下载任务项进任务列表，
///     由 <see cref="TaskDownloadPreLoadItem"/> 下载落地到
///     <see cref="PathsList.PreLoadDllPath"/>，下次启动游戏生效。
///     任何失败只写 Console，不影响启动流程。
///     </para>
/// </summary>
public static class PreLoadDependencyHelper
{
    /// <summary>PreLoad.NET 发布仓库（独立仓库，由 PreLoad.NET 的手动 workflow 发布）</summary>
    public const string Owner = "Round-Studio";
    public const string Repo = "PreLoad.NET-Release";

    /// <summary>Release 中唯一的资产名，与发布 workflow 的产物命名保持一致</summary>
    private const string AssetName = "PreLoad.NET.dll";

    /// <summary>读取本地已安装的 PreLoad.NET 版本（version.json 记录，即 Release Tag）</summary>
    public static string? GetLocalVersion() =>
        MultiplayerDependencyHelper.GetLocalVersion(PathsList.PreLoadVersionFile);

    /// <summary>
    ///     查询远端最新版本与资产；无资产或网络失败时返回 null。
    ///     私有仓库需在 Github 账号页登录后才有读权限。
    /// </summary>
    public static async Task<(string Tag, ReleaseAsset Asset)?> GetLatestAssetAsync()
    {
        var github = GitHubApiClient.CreateClient();
        var release = await github.Repository.Release.GetLatest(Owner, Repo);
        var asset = release.Assets.FirstOrDefault(x => x.Name == AssetName);
        return asset == null ? null : (release.TagName, asset);
    }

    public static async Task CheckAndUpdateAsync()
    {
        try
        {
            var latest = await GetLatestAssetAsync();
            if (latest == null)
            {
                Console.WriteLine(@"PreLoad.NET 更新检查：最新 Release 中未找到 PreLoad.NET.dll，跳过");
                return;
            }

            var localVersion = GetLocalVersion();
            if (string.Equals(MultiplayerDependencyHelper.NormalizeVersion(localVersion),
                    MultiplayerDependencyHelper.NormalizeVersion(latest.Value.Tag),
                    StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($@"PreLoad.NET 已是最新版本：{localVersion}");
                return;
            }

            Console.WriteLine($@"PreLoad.NET 发现新版本 {latest.Value.Tag}（本地：{localVersion ?? "无记录"}），加入下载任务");

            // 任务列表 UI 依赖主窗口，等它就绪再派发（超时则放弃，不影响启动）
            WaitForMainWindowOrTimeout();

            TaskDownloadPreLoadItem.StartDownload(latest.Value.Asset, latest.Value.Tag);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"PreLoad.NET 更新检查失败（不影响启动）：{ex.Message}");
        }
    }

    /// <summary>
    ///     启动游戏前的强制就绪守卫：组件缺失时阻塞等待下载完成。
    ///     严格模式——下载不可用（未登录/断网/私有仓库无权限）也返回 false 拦截启动。
    /// </summary>
    public static async Task<bool> EnsureComponentReadyAsync()
    {
        try
        {
            if (PreLoadDllStore.HasUpdatedDll()) return true;

            Console.WriteLine(@"PreLoad.NET 组件缺失，启动前强制下载");
            WaitForMainWindowOrTimeout();

            var latest = await GetLatestAssetAsync();
            if (latest == null)
            {
                ShowNotice(I18nManager.Instance["Task.PreLoad.Guard.FetchFailed"]);
                return false;
            }

            var success = await TaskDownloadPreLoadItem.StartDownloadAndWait(latest.Value.Asset, latest.Value.Tag);
            if (!success)
                ShowNotice(I18nManager.Instance["Task.PreLoad.Guard.DownloadFailed"]);

            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"PreLoad.NET 组件就绪检查失败: {ex}");
            ShowNotice(I18nManager.Instance["Task.PreLoad.Guard.DownloadFailed"] + $"\n{ex.Message}");
            return false;
        }
    }

    private static void ShowNotice(string message)
    {
        GlobalModel.MainWindow?.Notice.AddNotice(new NoticeInfo
        {
            Title = I18nManager.Instance["Task.PreLoad.Update.Title"],
            Message = message,
            NoticeType = NoticeType.Warning
        });
    }

    private static void WaitForMainWindowOrTimeout()
    {
        for (var waited = 0; waited < 15000 && GlobalModel.MainWindow == null; waited += 200)
            Thread.Sleep(200);
    }
}
