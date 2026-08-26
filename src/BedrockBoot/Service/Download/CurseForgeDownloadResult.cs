using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Interface.Download;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;

namespace BedrockBoot.Service.Download;

public class CurseForgeDownloadResult : IDownloadResult
{
    public CurseForgeDownloadResult(SearchResultItemInfo searchResultItemInfo)
    {
        SearchInfo = searchResultItemInfo;
    }

    public SearchResultItemInfo SearchInfo { get; set; }

    public async Task<List<Control>?> DescriptionControls()
    {
        var apiClient = new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey);
        var descriptionHtml = await apiClient.GetModDescriptionAsync(SearchInfo.Id);

        if (!string.IsNullOrEmpty(descriptionHtml))
        {
            var controls = HtmlToControlConverter.ConvertHtmlToControls(descriptionHtml);
            return controls;
        }

        return null;
    }

    public async Task<uint> GetDownloadCount()
    {
        return SearchInfo.DownloadCount;
    }
}