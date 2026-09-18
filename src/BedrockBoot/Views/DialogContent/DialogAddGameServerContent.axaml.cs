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
using BedrockBoot.Base.Entry.Game.Pack.Server;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogAddGameServerContent : UserControl
{
    public DialogAddGameServerContent()
    {
        InitializeComponent();
    }

    private static I18nManager i18n => I18nManager.Instance;

    /// <summary>
    ///     获取根据用户输入生成的服务器信息对象
    /// </summary>
    public ServerItemInfo ServerItemInfo
    {
        get
        {
            // 尝试解析端口，如果失败或为空则使用基岩版默认端口 19132
            if (!int.TryParse(ServerPortInputBox.Text, out var port)) port = 19132;

            return new ServerItemInfo
            {
                // 如果名称为空，使用国际化后的默认名称
                ServerName = !string.IsNullOrWhiteSpace(ServerNameInputBox.Text)
                    ? ServerNameInputBox.Text
                    : i18n["Dialog.AddServer.DefaultName"],

                // 地址通常是必填项，此处保持原始引用
                ServerAddress = ServerAddressInputBox.Text ?? string.Empty,

                ServerPort = port,
                VersionConfig = null
            };
        }
    }
}