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
using System.IO;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using BedrockBoot.Models.Account.Github;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Standard.Enum.Config;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Views.Control.Widgets;
using BedrockBoot.Views.DialogContent;
using Octokit;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Entity;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingAccount : ISettingPage
{
    public SettingAccount()
    {
        InitializeComponent();
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "Account.Panel.Title"
            }
        };

#if LINUX
        IsUseMSALAccountCard.IsVisible = false;
        IsUseMultipleUsersCard.IsVisible = false;
#endif

        UpdateUi();

        // 页面被缓存复用或从别处返回时，登录态可能已经变了（令牌过期、授权范围被改），
        // 每次挂到可视树都按当前状态重新判断一遍
        AttachedToVisualTree += (_, _) => UpdateGitHubState();
    }

    /// <summary>
    ///     按当前登录态刷新 GitHub 区块的文案与可见性。
    ///     纯同步、可重复调用，不包含头像加载。
    /// </summary>
    private void UpdateGitHubState()
    {
        var githubAccount = GitHubTokenManager.ReadAccount();
        var githubLoggedIn = GitHubTokenManager.IsLoggedIn;
        var githubExpired = !githubLoggedIn && GitHubTokenManager.HasCredential(githubAccount);

        LoginInGithub.IsVisible = !githubLoggedIn;
        GithubLogoutBtn.IsVisible = githubLoggedIn;

        // 已登录时说明文字改成"管理登录的 GitHub 账号"，并展示当前授权范围
        GithubExpander.Description = I18nManager.Instance[githubLoggedIn
            ? "Settings.Account.GitHub.Description.Manage"
            : "Settings.Account.GitHub.Description"];

        GithubScopeText.IsVisible = githubLoggedIn;
        GithubScopeActionBtn.IsVisible = githubLoggedIn;
        GithubExpander.IsFontIcon = !githubLoggedIn;

        if (githubLoggedIn)
        {
            // 已登录时标题带上用户名，比只有一个昵称更容易看出这是 GitHub 账户
            GithubHeaderText.Text = string.IsNullOrEmpty(githubAccount?.UserName)
                ? I18nManager.Instance["Settings.Account.Github"]
                : string.Format(I18nManager.Instance["Settings.Account.Github.LoggedInAs"],
                    githubAccount.UserName);

            var hasRepoScope = githubAccount?.HasRepoScope;
            GithubScopeText.Text = I18nManager.Instance[hasRepoScope switch
            {
                true => "Settings.Account.GitHub.Scope.Status.Full",
                false => "Settings.Account.GitHub.Scope.Status.LoginOnly",
                _ => "Settings.Account.GitHub.Scope.Status.Unknown"
            }];

            // 没有 repo 权限就给"授予"，已有则给"撤销"（撤销会重新授权为仅登录）
            var canGrant = hasRepoScope != true;
            GithubScopeActionBtn.Header = I18nManager.Instance[canGrant
                ? "Settings.Account.GitHub.GrantFullAccess.Title"
                : "Settings.Account.GitHub.RevokeFullAccess.Title"];
            GithubScopeActionBtn.Description = I18nManager.Instance[canGrant
                ? "Settings.Account.GitHub.GrantFullAccess.Desc"
                : "Settings.Account.GitHub.RevokeFullAccess.Desc"];
        }
        else
        {
            GithubHeaderText.Text = githubExpired
                ? I18nManager.Instance["Settings.Account.GitHub.Expired"]
                : I18nManager.Instance["Settings.Account.Github"];
        }
    }

    private async Task UpdateUi()
    {
        IsEdit = false;
        AccountPanel.Children.Clear();
        var users = MsAccountManager.Accounts.Accounts;
        NullCard.IsVisible = users.Count == 0;
        users.ForEach(user => AccountPanel.Children.Add(new AccountCard(user)));
        IsUseMultipleUsers.IsChecked = GlobalModel.Config.Data.IsUseMultipleUsers;
        IsUseMSALAccount.IsChecked = GlobalModel.Config.Data.IsUseMSALAccount;
        IsAskMeBeforeLaunch.IsChecked = GlobalModel.Config.Data.IsChooseAccountBeforeLaunch;
        
        // 登录态以令牌管理器为准：短效令牌过期但还能自动续期时仍算已登录，
        // 续期失败（refresh_token 也被撤销/过期）则回到未登录状态并提示重新登录
        UpdateGitHubState();

        // 头像要异步加载，只能放在同步状态刷新之后
        if (GitHubTokenManager.IsLoggedIn)
            GithubExpander.ImageIcon = await (new ImageLoader()).LoadIconAsync(
                GitHubTokenManager.ReadAccount()?.UserImage);

        IsEdit = true;
    }

    private void IsUseMultipleUsers_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsUseMultipleUsers = (bool)IsUseMultipleUsers.IsChecked!;
            GlobalModel.Config.Save();

            Models.Global.GlobalModel.MainWindow.SetReboot();
        }
    }

    private void IsUseMSALAccount_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsUseMSALAccount = (bool)IsUseMSALAccount.IsChecked!;
            GlobalModel.Config.Save();
        }
    }

    private void IsAskMeBeforeLaunch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsChooseAccountBeforeLaunch = (bool)IsAskMeBeforeLaunch.IsChecked!;
            GlobalModel.Config.Save();
        }
    }

    private async void AddAccountBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await MsAccountManager.LoginAccount();
        }
        catch (Exception exception)
        {
            _ = DialogHost.Close();
            Console.WriteLine($@"登录发生错误 {exception}");
            DialogHost.Show(new()
            {
                Title = I18nManager.Instance["Shared.Status.Error.Title"],
                Content = I18nManager.Instance["Account.Login.Error"],
                CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"]
            });
        }
        UpdateUi();
        Models.Global.GlobalModel.MainWindow.UpdateAccount();
    }

    private void LoginInGithub_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowScopeDialog();
    }

    /// <summary>弹出授权范围选择：完整授权（repo）或仅登录（user:email）</summary>
    private void ShowScopeDialog()
    {
        // 先让用户选授权范围：repo 权限是下载 Commit 产物的前提，不该在浏览器授权页才第一次看到
        var content = new DialogGitHubLoginScopeContent();
        content.Confirmed += fullAccess => _ = LoginWithScopeAsync(fullAccess);

        DialogHost.Show(new DialogInfo
        {
            Title = I18nManager.Instance["Settings.Account.GitHub.Scope.Title"],
            Content = content
        });
    }

    private void GithubScopeActionBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var hasRepoScope = GitHubTokenManager.ReadAccount()?.HasRepoScope;

        if (hasRepoScope != true)
        {
            // 授予：复用同一个授权范围弹窗，勾选承诺后走完整授权
            ShowScopeDialog();
            return;
        }

        DialogHost.Show(new DialogInfo
        {
            Title = I18nManager.Instance["Settings.Account.GitHub.RevokeFullAccess.Title"],
            Content = I18nManager.Instance["Settings.Account.GitHub.RevokeFullAccess.Confirm"],
            CloseButtonText = I18nManager.Instance["Settings.Account.GitHub.RevokeFullAccess.Action"],
            PrimaryButtonText = I18nManager.Instance["Shared.Action.Cancel"],
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () => _ = RevokeFullAccessAsync()
        });
    }

    /// <summary>
    ///     撤销 repo 权限：GitHub 不支持把已签发的令牌降级，只能作废旧授权后重新走一次
    ///     仅登录（user:email）授权，否则旧令牌里的 repo 权限会一直留到过期。
    /// </summary>
    private async Task RevokeFullAccessAsync()
    {
        var token = GitHubTokenManager.ReadAccount()?.UserToken;

        await GitHubOAuthManager.RevokeAsync(token);

        try
        {
            if (File.Exists(PathsList.GithubAccount))
                File.Delete(PathsList.GithubAccount);
        }
        catch (Exception exception)
        {
            Console.WriteLine($@"删除 Github 授权信息失败: {exception.Message}");
        }

        await LoginWithScopeAsync(false);
    }

    private async Task LoginWithScopeAsync(bool fullAccess)
    {
        try
        {
            var githubClient = new GitHubOAuthManager();
            await githubClient.LoginAsync(fullAccess);
        }
        catch (Exception exception)
        {
            Console.WriteLine($@"Github 登录发生错误 {exception}");
            DialogHost.Show(new DialogInfo
            {
                Title = I18nManager.Instance["Shared.Status.Error.Title"],
                Content = I18nManager.Instance["Settings.Account.Github.LoginError"],
                CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"]
            });
        }

        UpdateUi();
    }

    private void GithubLogoutBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = I18nManager.Instance["Settings.Account.Github.Logout"],
            Content = I18nManager.Instance["Settings.Account.Github.LogoutConfirm"],
            CloseButtonText = I18nManager.Instance["Settings.Account.Github.LogoutAction"],
            PrimaryButtonText = I18nManager.Instance["Shared.Action.Cancel"],
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                // 删除本地文件前先记下令牌，交给后台远程吊销（refresh_token 有 6 个月有效期，
                // 只删本地等于凭证还活着）
                var token = GitHubTokenManager.ReadAccount()?.UserToken;

                try
                {
                    if (File.Exists(PathsList.GithubAccount))
                        File.Delete(PathsList.GithubAccount);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($@"删除 Github 授权信息失败: {exception.Message}");
                }

                if (!string.IsNullOrEmpty(token))
                    _ = GitHubOAuthManager.RevokeAsync(token);

                // 退出登录不改更新通道：Commit 通道的更新检查是匿名的，照样能查；
                // 只有下载产物时才需要授权，届时会明确提示重新登录
                _ = UpdateUi();
            }
        });
    }
}