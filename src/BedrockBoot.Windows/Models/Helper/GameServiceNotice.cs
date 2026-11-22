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
using Avalonia.Threading;
using BedrockBoot.Localization;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Windows.Models.Helper;

public class GameServiceNotice
{
    public static void UnInstallGameService()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            DialogHost.Show(new DialogInfo()
            {
                Title = Localizer.Get("Windows.GameService.Notice.Title"),
                Content = new StackPanel()
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock()
                        {
                            Text = Localizer.Get("Windows.GameService.Notice.Content")
                        },
                        new HyperlinkButton()
                        {
                            Content = Localizer.Get("Windows.GameService.Notice.InstallStore"),
                            NavigateUri = new Uri("ms-windows-store://pdp/?productid=9MWPM2CQNLHN")
                        },
                        new HyperlinkButton()
                        {
                            Content = Localizer.Get("Windows.GameService.Notice.InstallWeb"),
                            NavigateUri =
                                new Uri("https://apps.microsoft.com/detail/9mwpm2cqnlhn?hl=zh-cn&gl=CN&ocid=pdpshare")
                        },
                        new TextBlock()
                        {
                            Text = Localizer.Get("Windows.GameService.Notice.Detail")
                        },
                        new HyperlinkButton()
                        {
                            Content = Localizer.Get("Windows.GameService.Notice.DocLink"),
                            NavigateUri = new Uri("https://docs.roundstudio.top/docs/product/bb/commonQuestion")
                        }
                    }
                },
                AccountButton = DialogButtons.CloseButton,
                CloseButtonText = Localizer.Get("Shared.Action.Confirm")
            });
        });
    }
}