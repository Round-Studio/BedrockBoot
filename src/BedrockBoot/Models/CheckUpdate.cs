using System;
using System.Reflection;
using Octokit;

namespace BedrockBoot.Models;

public class CheckUpdate
{
    public static async void Update()
    {
        // 创建客户端
        var github = new GitHubClient(new ProductHeaderValue("BedrockBoot"));
        // 设置Token（可选，用于私有仓库或提高限额）

        // 获取指定仓库的所有发布
        var owner = "Round-Studio";
        var repo = "BedrockBoot";
        var releases = await github.Repository.Release.GetAll(owner, repo);
        
        var latest = releases[0];
        Console.WriteLine($"最新版本: {latest.TagName}");

        if (!latest.TagName.EndsWith(Assembly.GetExecutingAssembly().GetName().Version.ToString()))
        {
            var bodyUrl = latest.Assets[0].BrowserDownloadUrl;
            Console.WriteLine($"下载地址：: {bodyUrl}");
        }
        else
        {
            Console.WriteLine($"当前为最新版本 {latest.TagName}，无需启动更新。");
        }
    }
}