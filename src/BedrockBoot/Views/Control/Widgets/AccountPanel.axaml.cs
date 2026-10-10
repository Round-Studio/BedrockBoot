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

using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Views.Control.Items.Xbox;
using BedrockBoot.Views.Pages;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.View;

namespace BedrockBoot.Views.Control.Widgets;

public partial class AccountPanel : UserControl
{
    private bool IsEdit = false;
    public AccountPanel()
    {
        InitializeComponent();
        UpdateAccount();
    }

    public async Task UpdateAccount()
    {
        IsEdit = false;
        if (Core.Global.GlobalModel.Config.Data.IsUseMultipleUsers)
        {
            var users = MsAccountManager.Accounts?.Accounts;
            var selIndex = users.FindLastIndex(user => user.BUID == MsAccountManager.Accounts?.SelectUserBUID);

            AccountList.Items.Clear();
            users.ForEach(user =>
            {
                AccountList.Items.Add(new ItemViewItem
                {
                    Width = 208,
                    Content = new AccountSmallItem(user)
                });
            });
            AccountList.SelectedIndex = selIndex;
        }

        IsEdit = true;
    }

    private async void AccountList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            MsAccountManager.AccountConfigEntity?.Data.SelectUserBUID =
                MsAccountManager.Accounts?.Accounts[AccountList.SelectedIndex].BUID;
            MsAccountManager.AccountConfigEntity?.Save();

            await Models.Global.GlobalModel.MainWindow.UpdateAccount(true);
        }
    }

    private void GoToAccountPage_OnClick(object? sender, RoutedEventArgs e)
    {
        MainPage.Instance.SelectTagPage("main:setting");
        MainSettingPage.NavigateTo(new SettingAccount());
    }
}