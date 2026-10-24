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

namespace BedrockBoot.Views.DialogContent.Multiplayer;

public partial class DialogMultiplayerLinkRoomContent : UserControl
{
    public DialogMultiplayerLinkRoomContent()
    {
        InitializeComponent();
    }

    /// <summary>联机码。Trim 处理粘贴时携带的空白字符；输入框为空时返回空字符串而非 null。</summary>
    public string RoomCode => RoomCodeInput.Text?.Trim() ?? string.Empty;
}