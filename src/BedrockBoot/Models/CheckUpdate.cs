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
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BedrockBoot.Standard.Enum;
using BedrockBoot.Models.Account.Github;
using BedrockBoot.Models.Global;
using BedrockBoot.Standard.Enum.Config;
using Octokit;
using Round.SDK.Entity;
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

    public string DisplayName => Source == UpdateSource.Release
        ? Release?.TagName ?? ""
        : $"{WorkflowRun?.Name} #{WorkflowRun?.RunNumber}";

    public string CommitSha => Source == UpdateSource.Release
        ? Release?.TargetCommitish ?? ""
        : WorkflowRun?.HeadSha ?? "";
}

public class CheckUpdate
{
    private const string Owner = "Round-Studio";
    private const string Repo = "BedrockBoot";

    public static async Task<Release?> Update()
    {
        var github = new GitHubClient(new ProductHeaderValue("BedrockBoot"));

        var releases = await github.Repository.Release.GetAll(Owner, Repo);

        var latest =
            releases.FirstOrDefault(x => x.Prerelease == (GlobalModel.Config.Data.UpdateType == UpdateType.Preview));
        Console.WriteLine($@"预览版：{GlobalModel.Config.Data.UpdateType == UpdateType.Preview}");

        if (latest == null)
        {
            Console.WriteLine(@"未找到匹配的 Release，跳过更新检查。");
            return null;
        }

        Console.WriteLine($@"最新版本: {latest.TagName}");

        if (!latest.TagName.Replace("0", "")
                .EndsWith(Assembly.GetExecutingAssembly().GetName().Version!.ToString().Replace("0", "")))
            return latest;

        Console.WriteLine($@"当前为最新版本 {latest.TagName}，无需启动更新。");

        return null;
    }

    public static async Task<WorkflowRun?> GetLatestActionRunAsync(string dataUserToken)
    {
        var github = new GitHubClient(new ProductHeaderValue("BedrockBoot"));

        if (!string.IsNullOrEmpty(dataUserToken))
            github.Credentials = new Credentials(dataUserToken);

        var runs = await github.Actions.Workflows.Runs.List(Owner, Repo);

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
            var conf = new ConfigEntity<GithubAccountInfo>(PathsList.GithubAccount);
            if (conf.Data == null)
                return null;
            if (string.IsNullOrEmpty(conf.Data.UserToken))
                return null;
            
            var run = await GetLatestActionRunAsync(conf.Data.UserToken);
            if (run == null)
                return null;

            var currentSha = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

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

        var release = await Update();
        if (release == null)
            return null;

        return new UpdateCheckResult
        {
            Source = UpdateSource.Release,
            Release = release
        };
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