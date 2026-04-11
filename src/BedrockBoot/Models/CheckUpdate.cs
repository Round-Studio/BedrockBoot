using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Global;
using Octokit;

namespace BedrockBoot.Models;

public class CheckUpdate
{
    public static async Task<Release> Update()
    {
        // 创建客户端
        var github = new GitHubClient(new ProductHeaderValue("BedrockBoot"));

        // 获取指定仓库的所有发布
        var owner = "Round-Studio";
        var repo = "BedrockBoot";
        var releases = await github.Repository.Release.GetAll(owner, repo);
        
        var latest = releases.FirstOrDefault(x => x.Prerelease == (BedrockBoot.Core.Global.GlobalModel.Config.Data.UpdateType == UpdateType.Preview));
        Console.WriteLine($"预览版：{(BedrockBoot.Core.Global.GlobalModel.Config.Data.UpdateType == UpdateType.Preview)}");

        Console.WriteLine($@"最新版本: {latest.TagName}");

        if (!latest.TagName.Replace("0","").EndsWith(Assembly.GetExecutingAssembly().GetName().Version!.ToString().Replace("0",""))) return latest;

        Console.WriteLine($@"当前为最新版本 {latest.TagName}，无需启动更新。");

        return null;
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

        // 严格正则: yyyy.mm.dd.hhmm 格式
        // 年: 2000-2099, 月: 01-12 或 1-12, 日: 01-31 或 1-31, 时间: 0000-2359 或 0-235959
        var pattern = @"^(20\d{2})\.(0?[1-9]|1[0-2])\.(0?[1-9]|[12]\d|3[01])\.(\d{1,6})$";
        
        if (!Regex.IsMatch(version, pattern))
            return false;

        // 进一步验证时间部分
        var parts = version.Split('.');
        var timePart = parts[3];
        
        if (!int.TryParse(timePart, out int time))
            return false;

        // 验证时间合理性
        if (timePart.Length == 1 || timePart.Length == 2) // 小时
        {
            return time <= 23;
        }
        else if (timePart.Length == 3 || timePart.Length == 4) // 时分
        {
            int hour = time / 100;
            int minute = time % 100;
            return hour <= 23 && minute <= 59;
        }
        else if (timePart.Length >= 5) // 时分秒
        {
            int hour = time / 10000;
            int minute = (time / 100) % 100;
            int second = time % 100;
            return hour <= 23 && minute <= 59 && second <= 59;
        }

        return true;
    }
}