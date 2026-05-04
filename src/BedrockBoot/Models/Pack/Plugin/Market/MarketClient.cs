using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Pack.Market;
using BedrockBoot.Models.Global;

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
            // 请求数据并反序列化为 MarketResponse 对象
            var response = await _httpClient.GetFromJsonAsync<MarketResponse>(_apiUrl);
            
            // 返回插件列表，如果为 null 则返回空列表
            return response?.Plugins ?? new List<MarketResponse.PluginInfo>();
        }
        catch (Exception ex)
        {
            // 可以在这里记录日志，例如: Logger.Error(ex);
            Console.WriteLine($@"[MarketClient] 获取插件列表失败: {ex.Message}");
            return new List<MarketResponse.PluginInfo>();
        }
    }
}