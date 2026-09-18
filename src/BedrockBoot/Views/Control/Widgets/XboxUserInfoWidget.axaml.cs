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
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Core.Models.Xbox;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Views.Control.Widgets;

public partial class XboxUserInfoWidget : UserControl
{
    public XboxUserInfoWidget()
    {
        InitializeComponent();

        Task.Run(async () =>
        {
            if (GlobalModel.XboxUserInfo != null)
                Dispatcher.UIThread.Invoke(() =>
                {
                    BoxContent.IsVisible = true;
                    PlayerName.Text = GlobalModel.XboxUserInfo.Gamertag;
                });
            else
                try
                {
                    var checker = new XboxLoginStatusChecker();
                    var status = await checker.GetDetailedXboxStatus();
                    GlobalModel.XboxUserInfo = status.XboxUserInfo;

                    Dispatcher.UIThread.Invoke(() =>
                    {
                        BoxContent.IsVisible = true;
                        PlayerName.Text = GlobalModel.XboxUserInfo.Gamertag;
                    });
                }
                catch
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        BoxContent.IsVisible = true;
                        PlayerName.Text = "无法获取";
                    });
                }

            Dispatcher.UIThread.Invoke(() => LoadRing.IsVisible = false);
        });
    }
}