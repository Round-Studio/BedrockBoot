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
                ItemName = "账户与档案"
            }
        };

#if LINUX
        IsUseMSALAccountCard.IsVisible = false;
        IsUseMultipleUsersCard.IsVisible = false;
#endif

        UpdateUi();
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
        
        var githubConf = new ConfigEntity<GithubAccountInfo>(PathsList.GithubAccount, false);
        var githubToken = githubConf.Data?.UserToken;
        var githubLoggedIn = !string.IsNullOrEmpty(githubToken);

        LoginInGithub.IsVisible = !githubLoggedIn;
        GithubLogoutBtn.IsVisible = githubLoggedIn;

        if (githubLoggedIn)
        {
            GithubHeaderText.Text = string.IsNullOrEmpty(githubConf.Data.UserName)
                ? "Github 账户"
                : githubConf.Data.UserName;

            GithubExpander.IsFontIcon = false;
            GithubExpander.ImageIcon = await (new ImageLoader()).LoadIconAsync(githubConf.Data.UserImage);
        }
        else
        {
            GithubHeaderText.Text = "Github 账户";
            GithubExpander.IsFontIcon = true;
        }

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
                Title = "发生错误",
                Content = "登录过程中发生错误，请检查网络连接并重试。",
                CloseButtonText = "确定"
            });
        }
        UpdateUi();
        Models.Global.GlobalModel.MainWindow.UpdateAccount();
    }

    private async void LoginInGithub_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var githubClient = new GitHubOAuthManager();
            await githubClient.LoginAsync();
        }
        catch (Exception exception)
        {
            Console.WriteLine($@"Github 登录发生错误 {exception}");
            DialogHost.Show(new DialogInfo
            {
                Title = "登录失败",
                Content = "Github 登录过程中发生错误，请检查网络连接后重试。\n若长时间未在浏览器中完成授权，请重新发起登录。",
                CloseButtonText = "确定"
            });
        }

        UpdateUi();
    }

    private void GithubLogoutBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = "退出 Github 登录",
            Content = "确定要退出当前登录的 Github 账户吗？\n退出后 Commit 更新通道等需要登录的功能将无法使用。",
            CloseButtonText = "退出登录",
            PrimaryButtonText = "取消",
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                try
                {
                    if (File.Exists(PathsList.GithubAccount))
                        File.Delete(PathsList.GithubAccount);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($@"删除 Github 授权信息失败: {exception.Message}");
                }

                // Commit 更新通道依赖 Github 登录，退出后回退到正式版通道
                if (GlobalModel.Config.Data.UpdateType == UpdateType.Commit)
                {
                    GlobalModel.Config.Data.UpdateType = UpdateType.Release;
                    GlobalModel.Config.Save();
                }

                _ = UpdateUi();
            }
        });
    }
}