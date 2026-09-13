using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Entry.Info.Download;
using BedrockBoot.Helpers;
using BedrockBoot.Interface.Download;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Plugin.Market;
using BedrockBoot.Models.Pack.Search;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.TaskItem;
using Octokit;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Service.Download;

public class DllModDownloadResult : IDownloadResult
{
    public DllModDownloadResult(SearchResultItemInfo searchResultItemInfo)
    {
        SearchInfo = searchResultItemInfo;
    }

    public SearchResultItemInfo SearchInfo { get; set; }
    public bool IsHasManyFiles { get; } = true;

    public async Task<List<Control>?> DescriptionControls()
    {
        var html = await MarketClient.GetReadmeHtml(SearchInfo.SourceWebsite.Split("/")[^2],
            SearchInfo.SourceWebsite.Split("/")[^1]);
        return HtmlToControlConverter.ConvertHtmlToControls(html);
    }

    public async Task<uint> GetDownloadCount()
    {
        var releases = await GetReleases();
        var totalDownloads = releases.Sum(r => r.Assets.Sum(a => a.DownloadCount));
        return (uint)totalDownloads;
    }

    private async Task<IReadOnlyList<Release>> GetReleases()
    {
        var github = new GitHubClient(new ProductHeaderValue("BedrockBoot"));
        var owner = SearchInfo.SourceWebsite.Split("/")[^2];
        var repo = SearchInfo.SourceWebsite.Split("/")[^1];

        var releasesTask = github.Repository.Release.GetAll(owner, repo);

        try
        {
            return (await releasesTask);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"获取插件仓库信息失败：{ex}");
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

    public async Task<bool> IsInstalled() => false;

    public Task Install() => throw new System.NotImplementedException();
    public Task ReInstall() => throw new System.NotImplementedException();
    public void Delete() => throw new System.NotImplementedException();

    public async Task<List<ResourceFileInfo>> GetFiles()
    {
        var result = new List<ResourceFileInfo>();
        var info = JsonSerializer.Deserialize<DllPackage>(SearchInfo.JsonData);
        var releases = await GetReleases();
        releases.ToList().ForEach(release =>
        {
            release.Assets.ToList().ForEach(asset =>
            {
                if ((bool)info?.Files.Keys.Contains(asset.Name))
                {
                    result.Add(new()
                    {
                        FileName = asset.Name,
                        FileSize = (uint)asset.Size,
                        Version = release.TagName,
                        VersionGroup = release.TagName,
                        OnDownload = (s) =>
                        {
                            var chooseInstanceDialog = new DialogChooseGameContent();
                            DialogHost.Show(new()
                            {
                                Content = chooseInstanceDialog,
                                Title = "选择安装实例",
                                CloseButtonText = "下载",
                                SecondaryButtonText = "取消",
                                CloseAction = () =>
                                {
                                    var conf = chooseInstanceDialog.VersionConfig;
                                    var fileInfo = info?.Files[asset.Name];
                                    fileInfo?.Url = fileInfo.Url.Replace("{{tag}}", release.TagName);

                                    TaskDownloadDllModItem.Install(conf, fileInfo!);
                                }
                            });
                        }
                    });
                }
            });
        });

        return result;
    }
}