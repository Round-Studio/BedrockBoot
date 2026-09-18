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
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Account.Microsoft;
using BedrockBoot.Models.Account.Microsoft;

namespace BedrockBoot.Views.Control.Items;

public partial class XboxUserItem : UserControl
{
    private readonly MsUserConfig _user;
    public Action<string>? OnDelete { get; set; }
    public XboxUserItem()
    {
        InitializeComponent();
    }

    public XboxUserItem(MsUserConfig user) : this()
    {
        _user = user;
        UserName.Text = user.UserName;
        UserLoginTime.Text = user.AuthResult.SavedAt.ToString();
        _ = UserIcon.Update(user.UserIconUrl);
    }

    private void InputElement_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        PART_ControlPanel.Opacity = 1;
    }

    private void InputElement_OnPointerExited(object? sender, PointerEventArgs e)
    {
        PART_ControlPanel.Opacity = 0;
    }

    private void DelBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        OnDelete?.Invoke(_user.BUID);
    }
}