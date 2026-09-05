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
using BedrockBoot.Models.Pack.LeviLamina;
using BedrockBoot.Models.Pack.Plugin.Market;
using BedrockBoot.Views.DialogContent.Loader.LeviLamina;
using Octokit;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Service.Download;

public class LeviLaminaDownloadResult : IDownloadResult
{
    public LeviLaminaDownloadResult(SearchResultItemInfo searchResultItemInfo)
    {
        SearchInfo = searchResultItemInfo;
    }

    public SearchResultItemInfo SearchInfo { get; set; }
    public bool IsHasManyFiles { get; } = true;

    public async Task<List<Control>?> DescriptionControls()
    {
        var html = await MarketClient.GetReadmeHtml(SearchInfo.Authors[0], SearchInfo.SourceWebsite.Split('/')[^1]);
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
        var owner = SearchInfo.SourceWebsite.Split('/')[^2];
        var repo = SearchInfo.SourceWebsite.Split('/')[^1];

        // 并行获取数据以提高性能
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

    public Task Install()
    {
        throw new System.NotImplementedException();
    }

    public Task ReInstall()
    {
        throw new System.NotImplementedException();
    }

    public void Delete()
    {
        throw new System.NotImplementedException();
    }

    public async Task<List<ResourceFileInfo>> GetFiles()
    {
        var info = JsonSerializer.Deserialize<KeyValuePair<string, PackageInfo>>(SearchInfo.JsonData);
        var versions = info.Value.Variants["client"].Versions
            .Reverse()
            .ToList();
        return versions.Select(x => new ResourceFileInfo()
        {
            FileName = x.Key,
            Description = $"{SearchInfo.Name} {x.Key}",
            OnDownload = (s) =>
            {
                var chooseModVersion = x.Key;
                var chooseInstanceDialog = new DialogChooseLeviLaminaModInstallInstanceContent(info.Value,
                    chooseModVersion, info.Value.Variants["client"].Versions[chooseModVersion]);
                DialogHost.Show(new()
                {
                    Title = $"安装 {SearchInfo.Name} {chooseModVersion}",
                    Content = chooseInstanceDialog,
                    CloseButtonText = "确定",
                    PrimaryButtonText = "取消",
                    AccountButton = DialogButtons.CloseButton,
                    CloseAction = () =>
                    {
                        var installer = new LeviLaminaModsInstaller(info.Value, info.Key);
                        installer.Install(chooseModVersion, chooseInstanceDialog.SavePath);
                    }
                });
            }
        }).ToList();
    }
}