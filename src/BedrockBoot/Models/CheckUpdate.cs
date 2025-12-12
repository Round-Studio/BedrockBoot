using System;
using System.Reflection;
using System.Threading.Tasks;
using Octokit;

namespace BedrockBoot.Models;

public class CheckUpdate
{
    public static async Task<Release> Update()
    {
        // 创建客户端
        var github = new GitHubClient(new ProductHeaderValue("BedrockBoot"));
        // 设置Token（可选，用于私有仓库或提高限额）

        // 获取指定仓库的所有发布
        var owner = "Round-Studio";
        var repo = "BedrockBoot";
        var releases = await github.Repository.Release.GetLatest(owner, repo);
        
        var latest = releases;
        Console.WriteLine($"最新版本: {latest.TagName}");

        if (!latest.TagName.EndsWith(Assembly.GetExecutingAssembly().GetName().Version.ToString()))
        {
            return latest;
        }
        else
        {
            Console.WriteLine($"当前为最新版本 {latest.TagName}，无需启动更新。");
        }

        return null;
    }
}