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
using Avalonia.Interactivity;
using BedrockBoot.Core.Global;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Views.Control.Widgets;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingAccountPages;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

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

        UpdateUi();
    }

    private void UpdateUi()
    {
        IsEdit = false;
        AccountPanel.Children.Clear();
        var users = MsAccountManager.Accounts.Accounts;
        users.ForEach(user =>
        {
            var item = new AccountCard(user);
            item.Click += (s, e) =>
            {
                MainSettingPage.NavigateTo(new AccountXbox(user));
            };
            AccountPanel.Children.Add(item);
        });
        IsUseMultipleUsers.IsChecked = GlobalModel.Config.Data.IsUseMultipleUsers;
        IsUseMSALAccount.IsChecked = GlobalModel.Config.Data.IsUseMSALAccount;
        IsAskMeBeforeLaunch.IsChecked = GlobalModel.Config.Data.IsChooseAccountBeforeLaunch;

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
    }
}