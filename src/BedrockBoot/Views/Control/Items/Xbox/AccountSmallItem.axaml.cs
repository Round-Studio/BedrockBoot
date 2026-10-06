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
using Avalonia.Media;
using BedrockBoot.Models.Helper;
using BedrockBoot.Standard.Entity.Account.Microsoft;

namespace BedrockBoot.Views.Control.Items.Xbox;

public partial class AccountSmallItem : UserControl
{
    private readonly MsUserConfig _user;
    private ImageLoader _imageLoader = new ImageLoader();

    public AccountSmallItem()
    {
        InitializeComponent();
    }
    public AccountSmallItem(MsUserConfig user):this()
    {
        _user = user;
        Task.Run(() =>
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                AccountName.Text = user.UserName;
                AccountIcon.Background = new ImageBrush()
                {
                    Source = await _imageLoader.LoadIconAsync(user.UserIconUrl)
                };
            });
        });
    }
}