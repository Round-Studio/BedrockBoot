using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Pack.Market;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Plugin;
using BedrockBoot.Models.Pack.Plugin.Market;
using BedrockBoot.Plugin;
using BedrockBoot.Views.TaskItem.Plugin;
using Octokit;
using Round.SDK.Entry;

namespace BedrockBoot.Views.Pages.DownloadPage.ResultSubPage.PluginMarket;

public partial class PluginMarketInfo : UserControl
{
    private readonly MarketResponse.PluginInfo _info;
    private PackConfig? _packConfig;
    private Release _release;

    public PluginMarketInfo()
    {
        InitializeComponent();
    }
    
    public PluginMarketInfo(MarketResponse.PluginInfo info) : this()
    {
        _info = info;
        _ = UpdateUi();
    }

    public async Task UpdateUi()
    {
        try
        {
            ContentPanel.IsVisible = false;
            LoadingCard.IsVisible = true;
            BodyPanel.Children.Clear();

            RepoBtn.NavigateUri = new Uri(_info.RepositoryUrl);

            PluginNameText.Text = _info.PluginName;
            ImageRender.Update(_info.IconUrl);

            var (repository, releases) = await MarketClient.GetPluginRepositoryFullInfo(_info);
            
            bool installStatus = releases.FirstOrDefault().Assets.All(x =>
            {
                var file = x.BrowserDownloadUrl;
                var fileName = Path.GetFileName(file);

                var result = PluginLoader.FindInstalledPackageFile(fileName);
                if (File.Exists(result))
                {
                    _packConfig = PluginHelper.ReadPackConfig(result);
                    return true;
                }
                
                return false;
            });

            _release = releases.FirstOrDefault();

            InstallBtn.IsVisible = !installStatus;
            ReInstallBtn.IsVisible = installStatus;
            DeleteBtn.IsVisible = installStatus;

            var totalDownloads = releases.Sum(r => r.Assets.Sum(a => a.DownloadCount));
            DownloadCountText.Text = totalDownloads.ToString();

            UpdataDateText.Text = GetTimeAgo(repository.UpdatedAt);
            AuthorText.Text = $"By {_info.Username}";

            ContentPanel.IsVisible = true;
            LoadingCard.IsVisible = false;

            var html = await MarketClient.GetReadmeHtml(_info.RepositoryOwner, _info.RepositoryName);

            var controls = HtmlToControlConverter.ConvertHtmlToControls(html);
            foreach (var control in controls) BodyPanel.Children.Add(control);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    
    public static string GetTimeAgo(DateTimeOffset publishedAt)
    {
        var timeSince = DateTimeOffset.Now - publishedAt;
    
        if (timeSince.TotalDays >= 1)
            return $"{(int)timeSince.TotalDays} 天前";
        else if (timeSince.TotalHours >= 1)
            return $"{(int)timeSince.TotalHours} 小时前";
        else if (timeSince.TotalMinutes >= 1)
            return $"{(int)timeSince.TotalMinutes} 分钟前";
        else
            return "刚刚";
    }

    private void InstallBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskDownloadPluginItem.Install(_release,_info);
        GlobalModel.MainWindow.CloseDraw();
    }

    private void ReInstallBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DeleteBtn_OnClick(sender, e);
        TaskDownloadPluginItem.Install(_release, _info);
        GlobalModel.MainWindow.CloseDraw();
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        PluginLoader.Delete(_packConfig);
        UpdateUi();
    }
}