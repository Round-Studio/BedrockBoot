using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Pack.Market;
using BedrockBoot.Models.Global;
using Octokit;

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

    public static async Task<Release> GetPluginRelease(MarketResponse.PluginInfo info)
    {
        var github = new GitHubClient(new ProductHeaderValue("BedrockBoot"));
        var owner = info.RepositoryOwner;
        var repo = info.RepositoryName;
        var release = await github.Repository.Release.GetLatest(owner, repo);
        
        return release;
    }
}