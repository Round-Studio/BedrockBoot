using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.Pages.MainSubPage;
using Octokit;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.OtherPage;

public partial class AboutContributor : UserControl
{
    public AboutContributor()
    {
        InitializeComponent();
        MainSettingPage.SettingBreadcrumbBar.SetItems(new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "关于我们",
                ItemClickAction = info => MainSettingPage.NavigationFrame.NavigateTo(new AboutPage())
            },
            new()
            {
                ItemName = "贡献者"
            }
        });

        Task.Run(async () =>
        {
            try
            {
                var githubClient = new GitHubClient(new ProductHeaderValue("BedrockBoot"));
                var cons = await githubClient.Repository.GetAllContributors("Round-Studio", "BedrockBoot");

                Dispatcher.UIThread.Invoke(() =>
                {
                    cons.ToList().ForEach(con =>
                    {
                        ContributorBox.Children.Add(new ContributorItem(con));
                    });
                    LoadingRing.IsVisible = false;
                });
            }catch{}
        });
    }
}