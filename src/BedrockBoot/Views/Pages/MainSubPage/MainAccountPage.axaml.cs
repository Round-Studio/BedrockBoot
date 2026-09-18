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
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Account.Microsoft;
using BedrockBoot.Core.Global;
using BedrockBoot.Models;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Views.Control.Items;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.View;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainAccountPage : BedrockBootPage
{
    public MainAccountPage()
    {
        InitializeComponent();
        
        UpdateUi();
    }

    public void UpdateUi()
    {
        IsEdit = false;
        IsChooseAccountBeforeLaunch.IsChecked = GlobalModel.Config.Data.IsChooseAccountBeforeLaunch;
        var users = MsAccountManager.Accounts?.Accounts;
        UsersList.IsVisible = false;
        UsersList.Items.Clear();
        NoneCard.IsVisible = true;
        
        if (users != null)
        {
            NoneCard.IsVisible = users.Count == 0;
            if(users.Count == 0) return;
            users.ForEach(user =>
            {
                UsersList.Items.Add(new ItemViewItem()
                {
                    Content = new XboxUserItem(user)
                    {
                        OnDelete = (buid) =>
                        {
                            DialogHost.Show(new()
                            {
                                Title = "您确定要删除账户吗",
                                Content = "删除此账户后，将会丢失该账户的验证凭证，将无法进行第三方服务器的游玩。",
                                CloseButtonText = "确定",
                                PrimaryButtonText = "取消",
                                CloseAction = () =>
                                {
                                    MsAccountManager.AccountConfigEntity!.Data.Accounts.RemoveAt(
                                        MsAccountManager.Accounts!.Accounts.FindIndex(x => x.BUID == buid));
                                    MsAccountManager.AccountConfigEntity!.Save();
                                    UpdateUi();
                                },
                                AccountButton = DialogButtons.CloseButton
                            });
                        }
                    }
                });
            });
            if (string.IsNullOrEmpty(MsAccountManager.Accounts?.SelectUserBUID))
            {
                MsAccountManager.AccountConfigEntity?.Data.SelectUserBUID = users[0].BUID;
                MsAccountManager.AccountConfigEntity?.Save();
            }
            var selIndex = users.FindLastIndex(user => user.BUID == MsAccountManager.Accounts?.SelectUserBUID);
            UsersList.SelectedIndex = selIndex;
            UsersList.IsVisible = true;
        }

        IsEdit = true;
    }

    private async void LoginBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await MsAccountManager.LoginAccount();
        }
        catch(Exception exception)
        {
            Console.WriteLine($@"登录发生错误 {exception}");
            _ = DialogHost.Close();
            DialogHost.Show(new()
            {
                Title = "发生错误",
                Content = "登录过程中发生错误，请检查网络连接并重试。",
                CloseButtonText = "确定"
            });
        }
        UpdateUi();
    }

    private void UsersList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            MsAccountManager.AccountConfigEntity?.Data.SelectUserBUID =
                MsAccountManager.Accounts?.Accounts[UsersList.SelectedIndex].BUID;
            MsAccountManager.AccountConfigEntity?.Save();
        }
    }

    private void IsChooseAccountBeforeLaunch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsChooseAccountBeforeLaunch != null)
        {
            GlobalModel.Config.Data.IsChooseAccountBeforeLaunch = (bool)IsChooseAccountBeforeLaunch.IsChecked!;
            GlobalModel.Config.Save();
        }
    }
}