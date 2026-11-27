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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Account.Github;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
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
///
///     <para>
///     “查版本”和“下文件”是两条独立链路，不再绑在同一条通道上：
///     <list type="bullet">
///         <item>版本号：GitHub API（顺带拿到资产对象，可写版本记录）→ GitHub main 分支的
///               version 文件（普通网页请求，不消耗 API 配额）。</item>
///         <item>下载：CNB 镜像（国内直连，不经过 GitHub）→ GitHub（API 资产或 latest 直链）。</item>
///     </list>
///     因此 GitHub 被限流时仍能拿到版本号并照常从 CNB 下载；CNB 不可用时也能退回 GitHub 下载。
///     </para>
///
///     <para>
///     启动器自身更新落地前还有一道强制检查（<see cref="CheckBeforeLauncherUpdateAsync"/>）：
///     该环节没有开关、不读配置，组件更新失败会直接中止启动器更新。
///     版本号、镜像与更新内容的来源统一走 <see cref="PreLoadReleaseSource"/>。
///     </para>
/// </summary>
public static class PreLoadDependencyHelper
{
    /// <summary>PreLoad.NET 发布仓库（独立仓库，由 PreLoad.NET 的手动 workflow 发布）</summary>
    public const string Owner = PreLoadReleaseSource.Owner;
    public const string Repo = PreLoadReleaseSource.Repo;

    /// <summary>Release 中唯一的资产名，与发布 workflow 的产物命名保持一致</summary>
    private const string AssetName = PreLoadReleaseSource.AssetName;

    /// <summary>
    ///     Release 页的 latest 直链：由 GitHub 服务端 302 到当前最新 Tag 的资产地址。
    ///     与 api.github.com 无关，因此不受 Rate Limit 影响，也不需要登录。
    /// </summary>
    public static string LatestDownloadUrl => PreLoadReleaseSource.LatestDownloadUrl;

    /// <summary>守卫是阻塞启动的关键路径，API 通道最多等这么久就转直链</summary>
    private static readonly TimeSpan GuardApiTimeout = TimeSpan.FromSeconds(8);

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
            var remote = await GetRemoteVersionAsync();
            if (remote == null)
            {
                Console.WriteLine(@"PreLoad.NET 更新检查：所有通道都没拿到版本信息，跳过");
                return;
            }

            var localVersion = GetLocalVersion();
            if (IsUpToDate(localVersion, remote.Tag))
            {
                Console.WriteLine($@"PreLoad.NET 已是最新版本：{localVersion}");
                return;
            }

            Console.WriteLine(
                $@"PreLoad.NET 发现新版本 {remote.Tag}（本地：{localVersion ?? "无记录"}，版本号来源：{remote.Source}），加入下载任务");

            // 任务列表 UI 依赖主窗口，等它就绪再派发（超时则放弃，不影响启动）
            WaitForMainWindowOrTimeout();

            StartDownloadInBackground(remote);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"PreLoad.NET 更新检查失败（不影响启动）：{ex.Message}");
        }
    }

    /// <summary>
    ///     启动游戏前的强制就绪守卫：组件缺失时阻塞等待下载完成。
    ///     严格模式——所有通道都拿不到文件时返回 false 拦截启动。
    ///     <para>
    ///     先解析最新版本（GitHub API → GitHub version 文件），再按下载顺序落地（CNB → GitHub）；
    ///     连版本号都拿不到时直接打 latest 直链（CNB → GitHub），尽量不让用户因为网络卡在启动这一步。
    ///     </para>
    /// </summary>
    public static async Task<bool> EnsureComponentReadyAsync()
    {
        try
        {
            if (PreLoadDllStore.HasUpdatedDll()) return true;

            Console.WriteLine(@"PreLoad.NET 组件缺失，启动前强制下载");
            WaitForMainWindowOrTimeout();

            var remote = await GetRemoteVersionAsync();
            if (remote != null && await DownloadRemoteAsync(remote)) return true;

            // 版本号拿不到，或那条通道下载失败：按 latest 直链再试一轮（CNB → GitHub）
            if (await DownloadLatestDirectAsync()) return true;

            ShowNotice(I18nManager.Instance["Task.PreLoad.Guard.DownloadFailed"]);
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"PreLoad.NET 组件就绪检查失败: {ex}");
            ShowNotice(I18nManager.Instance["Task.PreLoad.Guard.DownloadFailed"] + $"\n{ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     启动器更新落地前的组件强制检查。
    ///     <para>
    ///     PreLoad.NET 是启动器依赖的组件，新版本启动器跑在旧组件上容易出异常，
    ///     因此这一环是强制的：没有开关，也不读任何配置项。返回 false 表示组件没能更新，
    ///     调用方必须中止启动器更新。
    ///     </para>
    ///     <para>
    ///     先解析版本号（GitHub API → GitHub version 文件）；落后就按下载顺序更新（CNB → GitHub）。
    ///     版本号全部拿不到、或拿到的通道都下载失败时，再按 latest 直链强下（CNB → GitHub）；
    ///     仍失败即中止启动器更新。
    ///     </para>
    /// </summary>
    public static async Task<bool> CheckBeforeLauncherUpdateAsync()
    {
        try
        {
            var localVersion = GetLocalVersion();

            var remote = await GetRemoteVersionAsync();
            if (remote != null)
            {
                if (IsUpToDate(localVersion, remote.Tag))
                {
                    Console.WriteLine(
                        $@"更新启动器前检查：PreLoad.NET 已是最新版本 {localVersion}（版本号来源：{remote.Source}）");
                    return true;
                }

                Console.WriteLine(
                    $@"更新启动器前检查：PreLoad.NET 需要更新 {localVersion ?? "无记录"} → {remote.Tag}（版本号来源：{remote.Source}）");

                if (await DownloadRemoteAsync(remote)) return true;
            }

            // 所有通道都拿不到版本号，或拿到的通道都下载失败：按 latest 直链强下（CNB → GitHub）
            Console.WriteLine(@"更新启动器前检查：无法从版本号通道完成更新，改走 latest 直链强制下载");

            if (await DownloadLatestDirectAsync()) return true;

            ShowNotice(I18nManager.Instance["Task.PreLoad.Guard.DownloadFailed"]);
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"更新启动器前检查 PreLoad.NET 失败（将中止启动器更新）：{ex}");
            ShowNotice(I18nManager.Instance["Task.PreLoad.Guard.DownloadFailed"] + $"\n{ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     组件更新失败、需要阻断启动器更新时的弹窗。
    ///     只负责把弹窗投递到 UI 线程，不等待用户关闭——调用方随后自己中止流程。
    /// </summary>
    public static void ShowBlockedDialog()
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                DialogHost.Show(new DialogInfo
                {
                    Title = I18nManager.Instance["Task.PreLoad.Update.Title"],
                    Content = I18nManager.Instance["Task.PreLoad.BeforeUpdate.Blocked"] + "\n\n" +
                              I18nManager.Instance["Task.PreLoad.BeforeUpdate.DependencyWarning"],
                    CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"]
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"PreLoad.NET 阻断提示弹窗显示失败：{ex.Message}");
            }
        });
    }

    /// <summary>本地版本与远端 Tag 是否一致；本地无记录视为不一致（需要下载）</summary>
    private static bool IsUpToDate(string? localVersion, string? remoteTag)
    {
        if (string.IsNullOrWhiteSpace(localVersion) || string.IsNullOrWhiteSpace(remoteTag)) return false;

        return string.Equals(MultiplayerDependencyHelper.NormalizeVersion(localVersion),
            MultiplayerDependencyHelper.NormalizeVersion(remoteTag), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>PreLoad.NET 版本号的来源通道，与下载通道无关</summary>
    public enum PreLoadVersionSource
    {
        /// <summary>GitHub API：能拿到资产对象，私有仓库可带授权头</summary>
        GitHubRelease = 0,

        /// <summary>GitHub main 分支的 version 文件：普通网页请求，不消耗 API 配额</summary>
        GitHubRaw = 1
    }

    /// <summary>远端最新版本，以及该版本能用的下载方式</summary>
    /// <param name="Tag">远端版本号（可能是 <c>26.11.27.1741</c> 或 <c>v26.11.27.1741</c>）</param>
    /// <param name="Source">版本号从哪读到的</param>
    /// <param name="Asset">仅 GitHub API 通道有；其余通道为 null，下载时走 latest 直链</param>
    public sealed record PreLoadRemoteVersion(string Tag, PreLoadVersionSource Source, ReleaseAsset? Asset);

    /// <summary>
    ///     解析远端最新版本：GitHub API → GitHub 的 version 文件，取第一个可用的。
    ///     全部失败返回 null。
    /// </summary>
    public static async Task<PreLoadRemoteVersion?> GetRemoteVersionAsync()
    {
        // 1) GitHub API（限流/断网/无权限时返回 null）
        var latest = await TryGetLatestAssetAsync();
        if (latest != null)
            return new PreLoadRemoteVersion(latest.Value.Tag, PreLoadVersionSource.GitHubRelease,
                latest.Value.Asset);

        // 2) GitHub main 分支的 version 文件
        var remote = await PreLoadReleaseSource.TryGetLatestAsync(PreLoadReleaseChannel.Stable);
        return remote == null
            ? null
            : new PreLoadRemoteVersion(remote.Tag, PreLoadVersionSource.GitHubRaw, null);
    }

    /// <summary>
    ///     按解析到的版本下载并落地组件：下载优先 CNB，失败再走 GitHub。
    ///     不抛异常，失败返回 false。
    /// </summary>
    public static async Task<bool> DownloadRemoteAsync(PreLoadRemoteVersion remote)
    {
        try
        {
            // 1) CNB 优先：国内直连，不经过 GitHub，也不吃限流
            if (await DownloadFromCnbAsync(remote.Tag)) return true;

            // 2) GitHub：API 通道拿得到资产对象就按资产下载（可带授权头），否则走 latest 直链
            Console.WriteLine($@"CNB 通道下载失败，改用 GitHub 通道（{remote.Source}）");

            return remote.Asset != null
                ? await TaskDownloadPreLoadItem.StartDownloadAndWait(remote.Asset, remote.Tag)
                : await DownloadByDirectLinkAsync(remote.Tag);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"PreLoad.NET 组件下载失败（版本号来源：{remote.Source}）：{ex}");
            return false;
        }
    }

    /// <summary>
    ///     版本号完全拿不到时的强制下载：同样先 CNB、再 GitHub。
    ///     两条都是 latest 直链，不依赖版本号，因此限流或断网时仍可能成功。
    /// </summary>
    private static async Task<bool> DownloadLatestDirectAsync()
    {
        if (await DownloadFromCnbAsync(null)) return true;

        Console.WriteLine($@"改走 GitHub latest 直链：{PreLoadReleaseSource.LatestDownloadUrl}");
        return await DownloadByDirectLinkAsync(null);
    }

    /// <summary>
    ///     从 CNB 的 latest 直链下载。该地址固定、国内可直连，不需要走 GitHub 加速源探测。
    ///     <paramref name="tag" /> 只用于写版本记录（取的是 GitHub 判定出的版本号，
    ///     与实际落地的 CNB 产物可能有几个构建的偏差，下次版本号追上来自动校正）。
    /// </summary>
    private static async Task<bool> DownloadFromCnbAsync(string? tag)
    {
        Console.WriteLine($@"使用 CNB 通道下载 PreLoad.NET：{PreLoadReleaseSource.CnbLatestDownloadUrl}");

        var success = await TaskDownloadPreLoadItem.StartDirectDownloadAndWait(
            PreLoadReleaseSource.CnbLatestDownloadUrl, tag);

        if (!success) Console.WriteLine(@"CNB 通道下载 PreLoad.NET 失败");
        return success;
    }

    /// <summary>
    ///     后台下载：CNB 失败会自动退回 GitHub，异常在这里吞掉，不让未观察任务冒到上层。
    ///     设置页的手动检查也用它。
    /// </summary>
    public static void StartDownloadInBackground(PreLoadRemoteVersion remote)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await DownloadRemoteAsync(remote);
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"PreLoad.NET 后台下载失败：{ex.Message}");
            }
        });
    }

    /// <summary>
    ///     按直链下载并落地组件。<paramref name="knownTag" /> 已知时直接用它写版本记录，
    ///     未知时从 302 的 Location 里解析；解析不到只影响版本记录，不影响下载结果。
    ///     具体取哪个地址由 <see cref="PreLoadReleaseSource.GetDownloadUrl" /> 决定
    ///     （版本文件里的号不一定等于 Release Tag，会退回 latest 直链）。
    /// </summary>
    private static async Task<bool> DownloadByDirectLinkAsync(string? knownTag)
    {
        // 解析 Tag 不参与下载，因此与下载并行发起，避免 github.com 不通时白等一轮超时
        Task<string?> tagTask = knownTag != null ? Task.FromResult<string?>(knownTag) : TryResolveLatestTagAsync();

        var url = PreLoadReleaseSource.GetDownloadUrl(knownTag);
        var success = await TaskDownloadPreLoadItem.StartDirectDownloadAndWait(url);
        if (!success) return false;

        // 能拿到 Tag 就补写版本记录，避免下次启动/更新重复下载；拿不到只影响设置页的版本显示
        if (await tagTask is { } tag)
        {
            Console.WriteLine($@"PreLoad.NET 直链下载完成，版本记录补写为 {tag}");
            MultiplayerDependencyHelper.WriteLocalVersion(PathsList.PreLoadVersionFile, "PreLoad.NET", tag);
        }
        else
        {
            Console.WriteLine(@"PreLoad.NET 直链下载完成，但未解析到版本号，跳过版本记录");
        }

        return true;
    }

    /// <summary>
    ///     API 查询的容错包装：限流、断网、私有仓库未登录或超时都返回 null 而不是抛异常，
    ///     由调用方决定是否走直链兜底。
    ///     <para>
    ///     Octokit 默认连接超时长达 100 秒，而守卫处在阻塞启动的路径上，
    ///     所以这里限时等待，超时即放弃 API 通道。
    ///     </para>
    /// </summary>
    private static async Task<(string Tag, ReleaseAsset Asset)?> TryGetLatestAssetAsync()
    {
        var apiTask = GetLatestAssetAsync();
        try
        {
            if (await Task.WhenAny(apiTask, Task.Delay(GuardApiTimeout)) != apiTask)
            {
                // 放弃等待，但仍挂一个续延观察异常，避免未处理的任务异常
                _ = apiTask.ContinueWith(t => { _ = t.Exception; },
                    CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default);

                Console.WriteLine($@"PreLoad.NET 版本信息查询超时（{GuardApiTimeout.TotalSeconds:F0} 秒），转用直链");
                return null;
            }

            return await apiTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"PreLoad.NET 版本信息获取失败：{ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     通过 latest 直链的 302 跳转解析最新 Release Tag，用于补写版本记录。
    ///     纯网页请求，不消耗 API 配额；解析不到返回 null。
    /// </summary>
    public static async Task<string?> TryResolveLatestTagAsync()
    {
        try
        {
            // 关闭自动跳转：只看第一跳的 Location，不真正去拉资产
            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "BedrockBoot");

            using var response = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Head, LatestDownloadUrl),
                HttpCompletionOption.ResponseHeadersRead);

            var location = response.Headers.Location?.ToString();
            if (string.IsNullOrEmpty(location)) return null;

            // https://github.com/{owner}/{repo}/releases/download/{tag}/{asset}
            const string marker = "/releases/download/";
            var index = location.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return null;

            var rest = location[(index + marker.Length)..];
            var slash = rest.IndexOf('/');
            if (slash <= 0) return null;

            return Uri.UnescapeDataString(rest[..slash]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"PreLoad.NET 版本号解析失败（不影响直链下载）：{ex.Message}");
            return null;
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
