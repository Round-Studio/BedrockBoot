using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Interface;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.Pages.MainSubPage;
using Octokit;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.OtherPage;

public partial class AboutContributor : ISettingPage
{
    public AboutContributor()
    {
        InitializeComponent();

        // 面包屑导航国际化
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = i18n["AboutPage.Title"], // "关于我们"
                ItemClickAction = _ => MainSettingPage.NavigateTo(new AboutPage())
            },
            new()
            {
                ItemName = i18n["AboutPage.Contributor.Title"] // "贡献者"
            }
        };

        // 异步获取 GitHub 贡献者
        FetchContributors();
    }

    private static I18nManager i18n => I18nManager.Instance;

    private void FetchContributors()
    {
        Task.Run(async () =>
        {
            try
            {
                // 初始化 GitHub 客户端
                var githubClient = new GitHubClient(new ProductHeaderValue("BedrockBoot"));

                // 获取 Round-Studio/BedrockBoot 仓库的贡献者
                var contributors = await githubClient.Repository.GetAllContributors("Round-Studio", "BedrockBoot");

                Dispatcher.UIThread.Invoke(() =>
                {
                    ContributorBox.Children.Clear();
                    foreach (var item in contributors) ContributorBox.Children.Add(new ContributorItem(item));
                    LoadingRing.IsVisible = false;
                });
            }
            catch (Exception ex)
            {
                // 记录错误并在 UI 上反馈（可选）
                Console.WriteLine($"Failed to fetch contributors: {ex.Message}");

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LoadingRing.IsVisible = false;
                    // 如果加载失败，可以显示一个提示文本
                    // ErrorTextBlock.IsVisible = true;
                    // ErrorTextBlock.Text = i18n["AboutPage.Contributor.LoadFailed"];
                });
            }
        });
    }
}