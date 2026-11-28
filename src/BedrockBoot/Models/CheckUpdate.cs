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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BedrockBoot.Standard.Enum;
using BedrockBoot.Models.Account.Github;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Standard.Enum.Config;
using Octokit;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;

namespace BedrockBoot.Models;

public enum UpdateSource
{
    Release,
    Action
}

public class UpdateCheckResult
{
    public UpdateSource Source { get; init; }
    public Release? Release { get; init; }
    public WorkflowRun? WorkflowRun { get; init; }

    /// <summary>
    ///     API 不可用时从 <c>version</c> 分支读到的版本号（带 v，与 Release Tag 同形）。
    /// 此时 <see cref="Release" /> 为 null，但下载、展示都不需要它。
    /// </summary>
    public string? FallbackTag { get; init; }

    /// <summary>API 不可用时从 <c>version</c> 分支读到的更新日志（Markdown）</summary>
    public string? FallbackBody { get; init; }

    /// <summary>API 不可用时的资产直链：不依赖 Release 对象也能下到同一个构建</summary>
    public string? DirectDownloadUrl { get; init; }

    public string DisplayName => Source == UpdateSource.Release
        ? Release?.TagName ?? FallbackTag ?? ""
        : $"{WorkflowRun?.Name} #{WorkflowRun?.RunNumber}";

    public string CommitSha => Source == UpdateSource.Release
        ? Release?.TargetCommitish ?? ""
        : WorkflowRun?.HeadSha ?? "";
}

public class CheckUpdate
{
    private const string Owner = "Round-Studio";
    private const string Repo = "BedrockBoot";

    /// <summary>
    ///     API 检查的时限。Octokit 默认超时过长，api.github.com 无声卡住时用户要干等；
    ///     超时抛 TimeoutException，正式版/预览版会进 version 分支的回退流程。
    /// </summary>
    private static readonly TimeSpan ApiCheckTimeout = TimeSpan.FromSeconds(30);

    /// <summary>给 API 调用加上限时，超时抛 <see cref="TimeoutException" /> 而不是无限等待</summary>
    private static async Task<T> WithTimeout<T>(Task<T> task, string what)
    {
        var completed = await Task.WhenAny(task, Task.Delay(ApiCheckTimeout)).ConfigureAwait(false);
        if (completed != task)
            throw new TimeoutException($@"{what}超过 {ApiCheckTimeout.TotalSeconds} 秒未响应");
        return await task.ConfigureAwait(false);
    }

    public static async Task<Release?> Update()
    {
        // 统一入口：令牌快过期会先续期，401 会刷新后重试一次
        var releases = await WithTimeout(
            GitHubApiClient.ExecuteAsync(c => c.Repository.Release.GetAll(Owner, Repo)), @"Release 检查");

        var latest =
            releases.FirstOrDefault(x => x.Prerelease == (GlobalModel.Config.Data.UpdateType == UpdateType.Preview));
        Console.WriteLine($@"预览版：{GlobalModel.Config.Data.UpdateType == UpdateType.Preview}");

        if (latest == null)
        {
            Console.WriteLine(@"未找到匹配的 Release，跳过更新检查。");
            return null;
        }

        Console.WriteLine($@"最新版本: {latest.TagName}");

        if (!IsCurrentVersion(latest.TagName))
            return latest;

        Console.WriteLine($@"当前为最新版本 {latest.TagName}，无需启动更新。");

        return null;
    }

    /// <summary>
    ///     远端 Tag 是否就是当前运行的版本。CI 生成的版本号会省略前导 0
    ///     （<c>v2.1.11.0308</c> 与 <c>2.1.11.308</c>），逐段按数字比较；
    ///     Tag 段数比程序集版本少时按尾部对齐（例如 <c>v2.1.11</c> 对 <c>2.1.11.0</c>）。
    ///     <para>
    ///     不能像以前那样整体去掉字符 <c>0</c> 再 EndsWith：<c>102</c> 与 <c>120</c>
    ///     都会被削成 <c>12</c>，把「落后一个构建」误判成「已是最新」。
    ///     </para>
    /// </summary>
    private static bool IsCurrentVersion(string? tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName)) return false;

        var current = SplitVersion(Assembly.GetExecutingAssembly().GetName().Version!.ToString());
        var remote = SplitVersion(tagName);
        if (remote.Length == 0 || remote.Length > current.Length) return false;

        return current[(current.Length - remote.Length)..].SequenceEqual(remote);
    }

    /// <summary>把 <c>v2.1.11.0308</c> 拆成去前导 0 的段；非数字段原样保留</summary>
    private static string[] SplitVersion(string version) =>
        version.Trim().TrimStart('v', 'V')
            .Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => int.TryParse(part.Trim(), out var number) ? number.ToString() : part.Trim())
            .ToArray();

    /// <summary>
    ///     API 不可用时的兜底：从仓库 <c>version</c> 分支读版本号与更新日志
    ///     （<c>info/release</c> / <c>info/preview</c>、<c>changelog/&lt;版本号&gt;.md</c>）。
    ///     <para>
    ///     <c>Available = false</c> 表示这个通道也读不到，调用方应继续抛错；
    ///     <c>Available = true</c> 且 <c>Result = null</c> 表示已经是最新版本。
    ///     </para>
    /// </summary>
    private static async Task<(bool Available, UpdateCheckResult? Result)> TryGetFallbackAsync(
        UpdateType updateType)
    {
        var version = await LauncherReleaseSource.TryGetVersionAsync(updateType);
        if (version == null)
        {
            Console.WriteLine(@"启动器版本号兜底通道也不可用");
            return (false, null);
        }

        var tag = LauncherReleaseSource.ToTag(version);
        if (IsCurrentVersion(tag))
        {
            Console.WriteLine($@"当前为最新版本 {tag}（兜底通道），无需启动更新。");
            return (true, null);
        }

        var body = await LauncherReleaseSource.TryGetChangelogAsync(version);
        return (true, new UpdateCheckResult
        {
            Source = UpdateSource.Release,
            FallbackTag = tag,
            FallbackBody = body,
            // 只在支持自更新的平台上给直链，避免在其它平台把 Windows 产物当成更新包
            DirectDownloadUrl = LauncherDirectDownloadHelper.IsSupported
                ? LauncherReleaseSource.GetDownloadUrl(version)
                : null
        });
    }

    public static async Task<WorkflowRun?> GetLatestActionRunAsync()
    {
        // 同样加时限：Commit 通道没有 version 分支兜底，卡住时宁可快速报错让用户重试
        var runs = await WithTimeout(
            GitHubApiClient.ExecuteAsync(c => c.Actions.Workflows.Runs.List(Owner, Repo)), @"Commit 检查");

        var latest = runs.WorkflowRuns
            .Where(r => r.Status == WorkflowRunStatus.Completed
                        && r.Conclusion == WorkflowRunConclusion.Success)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        if (latest == null)
        {
            Console.WriteLine(@"未找到成功的 Action 运行记录。");
            return null;
        }

        Console.WriteLine($@"最新 Action: {latest.Name} #{latest.RunNumber}");
        Console.WriteLine($@"提交: {latest.HeadSha}");
        Console.WriteLine($@"时间: {latest.CreatedAt}");

        return latest;
    }

    public static async Task<UpdateCheckResult?> CheckAsync()
    {
        if (GlobalModel.Config.Data.UpdateType == UpdateType.Commit)
        {
            // 公开仓库的 Actions 接口允许匿名读取，未登录也能拿到最新提交与更新日志；
            // 只有下载产物需要登录并授予 repo 权限，那一步会自己提示
            var run = await GetLatestActionRunAsync();
            if (run == null)
                return null;

            // 已应用的提交以本地记录为准：CI 生成的 InformationalVersion 是日期版本号，
            // 不包含 SHA，直接和 HeadSha 比较永远不相等，会导致同一提交反复提示更新
            var appliedSha = GetAppliedCommitSha();
            if (!string.IsNullOrEmpty(appliedSha) &&
                run.HeadSha.StartsWith(appliedSha, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($@"当前已是最新提交 {run.HeadSha}，无需更新。");
                return null;
            }

            var currentSha = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            // 兼容未来 CI 将 SHA 嵌入 InformationalVersion 的情况
            if (!string.IsNullOrEmpty(currentSha) && run.HeadSha.StartsWith(currentSha))
            {
                Console.WriteLine($@"当前已是最新提交 {run.HeadSha}，无需更新。");
                return null;
            }

            return new UpdateCheckResult
            {
                Source = UpdateSource.Action,
                WorkflowRun = run
            };
        }

        return await CheckReleaseAsync();
    }

    /// <summary>
    ///     正式版/预览版的检查入口：GitHub API 优先，API 不可用时退回 version 分支的
    ///     版本号 + 更新日志（普通网页请求，不消耗 API 配额）。
    ///     <para>
    ///     两条通道都拿不到 version 分支文件时，把 API 的异常继续抛给调用方——
    ///     由界面弹网络错误并提供“强制下载最新版”，而不是误报“已是最新”。
    ///     </para>
    /// </summary>
    private static async Task<UpdateCheckResult?> CheckReleaseAsync()
    {
        var updateType = GlobalModel.Config.Data.UpdateType;

        try
        {
            var release = await Update();
            return release == null
                ? null
                : new UpdateCheckResult { Source = UpdateSource.Release, Release = release };
        }
        catch (Exception ex) when (updateType is UpdateType.Release or UpdateType.Preview)
        {
            Console.WriteLine($@"GitHub API 检查更新失败：{ex.Message}；改从 version 分支读取版本信息");

            var (available, result) = await TryGetFallbackAsync(updateType);
            if (!available)
            {
                Console.WriteLine(@"version 分支同样不可用，按更新检查失败处理");
                throw;
            }

            return result;
        }
    }

    private static string CommitRecordPath =>
        Path.Combine(PathsList.ConfigFolderPath, "update", "last_commit.sha");

    public static string? GetAppliedCommitSha()
    {
        try
        {
            return File.Exists(CommitRecordPath)
                ? File.ReadAllText(CommitRecordPath).Trim()
                : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"读取已应用提交记录失败: {ex.Message}");
            return null;
        }
    }

    public static void RecordAppliedCommitSha(string sha)
    {
        try
        {
            var directory = Path.GetDirectoryName(CommitRecordPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(CommitRecordPath, sha);
            Console.WriteLine($@"已记录应用的提交: {sha}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"记录已应用提交失败: {ex.Message}");
        }
    }

    public static UpdateType GetBodyUpdateType()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        if (IsStrictDateVersion(version))
            return UpdateType.Preview;

#if DEBUG
        return UpdateType.Debug;
#else
        return UpdateType.Release;
#endif
    }

    private static bool IsStrictDateVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return false;

        version = version.TrimStart('v', 'V');

        var pattern = @"^(20\d{2})\.(0?[1-9]|1[0-2])\.(0?[1-9]|[12]\d|3[01])\.(\d{1,6})$";

        if (!Regex.IsMatch(version, pattern))
            return false;

        var parts = version.Split('.');
        var timePart = parts[3];

        if (!int.TryParse(timePart, out var time))
            return false;

        if (timePart.Length == 1 || timePart.Length == 2)
            return time <= 23;

        if (timePart.Length == 3 || timePart.Length == 4)
        {
            var hour = time / 100;
            var minute = time % 100;
            return hour <= 23 && minute <= 59;
        }

        if (timePart.Length >= 5)
        {
            var hour = time / 10000;
            var minute = time / 100 % 100;
            var second = time % 100;
            return hour <= 23 && minute <= 59 && second <= 59;
        }

        return true;
    }
}