using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockLauncher.Core;
using BedrockLauncher.Core.VersionJsons;

namespace BedrockBoot.Views.Pages.DownloadPage.SearchSubPage;

public partial class SearchDefault : UserControl
{
    public SearchDefault()
    {
        InitializeComponent();

        DownloadSearch.SearchDetailed = null;

        Task.Run(() =>
        {
            var lst = VersionHelper.GetVersions();
            
            var rele = lst.Find(x => x.Type == MinecraftGameTypeVersion.Release);
            var prev = lst.Find(x => x.Type == MinecraftGameTypeVersion.Preview);

            Dispatcher.UIThread.Invoke(() =>
            {
                RecommendationPanel.IsVisible = true;
                LoadRing.IsVisible = false;

                ReleaseVersion.Text = rele.ID;
                PreviewVersion.Text = prev.ID;

                ReleaseDescription.Text = $"{rele.Date}，{rele.BuildType}";
                PreviewDescription.Text = $"{prev.Date}，{prev.BuildType}";
            });
        });
    }
    

    private void GameListBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DownloadSearch.SearchFrame.NavigateTo(new SearchDetailed(new SearchInfo()
        {
            Type = SearchResourceType.Minecraft
        }));
    }
}