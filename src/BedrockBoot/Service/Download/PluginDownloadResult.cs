using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Entry.Pack.Market;
using BedrockBoot.Interface.Download;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Plugin.Market;

namespace BedrockBoot.Service.Download;

public class PluginDownloadResult : IDownloadResult
{
    public PluginDownloadResult(SearchResultItemInfo searchResultItemInfo)
    {
        SearchInfo = searchResultItemInfo;
    }

    public SearchResultItemInfo SearchInfo { get; set; }

    public async Task<List<Control>?> DescriptionControls()
    {
        var pluginInfo = JsonSerializer.Deserialize<MarketResponse.PluginInfo>(SearchInfo.JsonData);
        var html = await MarketClient.GetReadmeHtml(pluginInfo.RepositoryOwner, pluginInfo.RepositoryName);

        var controls = HtmlToControlConverter.ConvertHtmlToControls(html);
        return controls;
    }

    public async Task<uint> GetDownloadCount()
    {
        throw new System.NotImplementedException();
    }
}