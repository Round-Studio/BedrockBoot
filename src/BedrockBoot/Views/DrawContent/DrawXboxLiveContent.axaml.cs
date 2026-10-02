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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Standard.Entity.Account.Microsoft;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Models.Pack.Xbox.Cape;
using BedrockBoot.Views.Pages.XboxSubPage.DrawContent;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation.LeftSelectBar;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawXboxLiveContent : UserControl
{
    private MsUserConfig _xbl;
    private string _authHeader = string.Empty;

    public DrawXboxLiveContent()
    {
        InitializeComponent();
    }

    public DrawXboxLiveContent(MsUserConfig xbl) : this()
    {
        _xbl = xbl;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            Console.WriteLine(@"正在刷新账户凭证...");
            _xbl = await MsAccountManager.RefreshAccountAsync(_xbl) ?? _xbl;
            Console.WriteLine(@"刷新完毕。");

            Console.WriteLine(@"开始获取 Xbox 用户凭证");

            var authClient = new XboxMcAuthClient();

            string? xblToken = await authClient.GetXboxUserTokenAsync(_xbl.AuthResult.AccessToken);
            if (xblToken == null)
            {
                Console.WriteLine(@"获取 XBL Token 失败，流程终止");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LoadCard.IsVisible = false;
                    MainContent.IsVisible = false;
                });
                return;
            }

            var (xstsToken, userHash, xuid) = await authClient.GetXstsTokenAsync(
                xblToken,
                XboxMcAuthClient.PlayFabRelyingParty);

            if (xstsToken == null || userHash == null)
            {
                Console.WriteLine(@"获取 XSTS Token 失败，流程终止");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LoadCard.IsVisible = false;
                    MainContent.IsVisible = false;
                });
                return;
            }

            _authHeader = $"XBL3.0 x={userHash};{xstsToken}";
            Console.WriteLine(@"XBL3.0 Token 获取成功");

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                XboxFrame.NavigateTo(new XboxCapes(_authHeader));
                LoadCard.IsVisible = false;
                MainContent.IsVisible = true;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"初始化异常: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LoadCard.IsVisible = false;
                MainContent.IsVisible = false;
            });
        }
    }

    public bool IsEditMode { get; set; }

    private void TabControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsEditMode) return;

        var tag = ((LeftSelectBarItem)TabControl.SelectedItem!).Tag!.ToString();

        switch (tag)
        {
            case "Info":
                XboxFrame.NavigateTo(new XboxCapes(_authHeader));
                break;
        }
    }
}