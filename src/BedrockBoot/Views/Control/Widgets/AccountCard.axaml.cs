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

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Models.Helper;
using BedrockBoot.Standard.Entity.Account.Microsoft;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingAccountPages;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Control.Widgets;

public partial class AccountCard : UserControl
{
    private readonly MsUserConfig _user;

    public AccountCard()
    {
        InitializeComponent();
    }

    public AccountCard(MsUserConfig user) : this()
    {
        _user = user;
        UpdateUi();
    }

    private async void UpdateUi()
    {
        Expander.Header = _user.UserName;
        Expander.Description = _user.IsDefault ? "Xbox 账户 · 默认" : "Xbox 账户";
        Expander.ImageIcon = await new ImageLoader().LoadIconAsync(_user.UserIconUrl, 128);
    }

    private void ManageBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new AccountXbox(_user));
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
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
                    MsAccountManager.Accounts!.Accounts.FindIndex(x => x.BUID == _user.BUID));
                MsAccountManager.AccountConfigEntity!.Save();
                MainSettingPage.NavigateTo(new SettingAccount());
                Models.Global.GlobalModel.MainWindow.UpdateAccount();
            },
            AccountButton = DialogButtons.CloseButton
        });
    }
}