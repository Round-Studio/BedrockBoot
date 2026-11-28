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

using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Models.Global;
using BedrockBoot.Standard.Entity.Account.Microsoft;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingAccountPages;

public partial class AccountXbox : ISettingPage
{
    private readonly MsUserConfig _msUserConfig;

    public AccountXbox()
    {
        InitializeComponent();
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "Settings.Nav.Account.Title",
                ItemClickAction = (i) =>
                    MainSettingPage.NavigateTo(new SettingAccount())
            },
            new()
            {
                ItemName = "Settings.Account.Xbox.Title"
            }
        };
    }

    public AccountXbox(MsUserConfig msUserConfig)
    {
        _msUserConfig = msUserConfig;
        InitializeComponent();
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "Settings.Nav.Account.Title",
                ItemClickAction = (i) =>
                    MainSettingPage.NavigateTo(new SettingAccount())
            },
            new()
            {
                ItemName = _msUserConfig.UserName
            }
        };

        AccountName.Text = _msUserConfig.UserName;
        AccountImage.Update(_msUserConfig.UserIconUrl);
    }

    private void DeleteAccountBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new()
        {
            Title = I18nManager.Instance["Account.Delete.Confirm.Title"],
            Content = I18nManager.Instance["Account.Delete.Confirm.Content"],
            CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
            PrimaryButtonText = I18nManager.Instance["Shared.Action.Cancel"],
            CloseAction = () =>
            {
                MsAccountManager.AccountConfigEntity!.Data.Accounts.RemoveAt(
                    MsAccountManager.Accounts!.Accounts.FindIndex(x => x.BUID == _msUserConfig.BUID));
                MsAccountManager.SaveAccounts();
                MainSettingPage.NavigateTo(new SettingAccount());
                Models.Global.GlobalModel.MainWindow.UpdateAccount();
            },
            AccountButton = DialogButtons.CloseButton
        });
    }

    private void UserBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        GlobalModel.MainWindow.OpenDraw(new DrawXboxLiveContent(_msUserConfig), string.Format(I18nManager.Instance["Settings.Account.Xbox.LiveAccount"], _msUserConfig.UserName));
    }
}