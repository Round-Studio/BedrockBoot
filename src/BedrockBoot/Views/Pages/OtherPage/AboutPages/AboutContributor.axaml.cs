/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Account.Github;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Helper;
using Octokit;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.OtherPage.AboutPages;

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
        Task.Run((Func<Task?>)(async () =>
        {
            try
            {
                // 初始化 GitHub 客户端
                var githubClient = GitHubApiClient.CreateClient();

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
                Console.WriteLine($@"Failed to fetch contributors: {ex.Message}");

                await Dispatcher.UIThread.InvokeAsync(() => LoadingRing.IsVisible = false);
                await GitHubErrorDialog.ShowAsync(ex, "网络错误", i18n["Shared.Action.Confirm"]);
            }
        }));
    }
}