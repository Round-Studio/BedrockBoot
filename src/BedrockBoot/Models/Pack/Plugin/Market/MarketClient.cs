using BedrockBoot.Base.Entry.Pack.Market;
using BedrockBoot.Helpers;
using BedrockBoot.Models.Global;
using Octokit;
using Octokit.Internal;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Pack.Plugin.Market;

public class MarketClient
{
    private readonly HttpClient _httpClient;
    private static string _apiUrl = SourceList.PluginApi;

    public MarketClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <summary>
    /// 从市场获取所有插件列表
    /// </summary>
    public async Task<List<MarketResponse.PluginInfo>> GetPluginsAsync()
    {
        Console.WriteLine(@"获取可下载的插件列表...");
        try
        {
            var response = await _httpClient.GetFromJsonAsync<MarketResponse>(_apiUrl);
            
            return response?.Plugins ?? new List<MarketResponse.PluginInfo>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"获取插件列表失败: {ex.Message}");
            return new List<MarketResponse.PluginInfo>();
        }
    }

    public static async Task<(Repository Repository, IReadOnlyList<Release> Releases)> 
        GetPluginRepositoryFullInfo(MarketResponse.PluginInfo info)
    {
        Console.WriteLine($@"获取目标插件仓库信息：{info.PluginName}");
        var github = new GitHubClient(new ProductHeaderValue("BedrockBoot"));
        var owner = info.RepositoryOwner;
        var repo = info.RepositoryName;
    
        // 并行获取数据以提高性能
        var repositoryTask = github.Repository.Get(owner, repo);
        var releasesTask = github.Repository.Release.GetAll(owner, repo);

        try
        {
            await Task.WhenAll(repositoryTask, releasesTask);

            return (await repositoryTask, await releasesTask);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取插件仓库信息失败：{ex}");
            var error = GitHubHelper.HandleException(ex);
            DialogHost.Show(new DialogInfo
            {
                Title = "网络错误",
                Content = $"无法从 GitHub 获取信息，请检查网络后重试。\n{error.GetLocalizedMessage()}",
                CloseButtonText = "确定"
            });
            throw;
        }
    }
    
    public static async Task<string> GetReadmeHtml(string owner, string repo)
    {
        Console.WriteLine($@"获取仓库 README: {owner}/{repo}");
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.html");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "BedrockBoot");
        
            var url = $"https://api.github.com/repos/{owner}/{repo}/readme";
            var response = await httpClient.GetAsync(url);
        
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
        
            return null;
        }
    }
}