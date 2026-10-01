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
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Pack.Xbox.Cape;
using BedrockBoot.Views.Control.Items.Xbox.Capes;

namespace BedrockBoot.Views.Pages.XboxSubPage.DrawContent;

public partial class XboxCapes : UserControl
{
    private readonly string _xbl;

    public XboxCapes()
    {
        InitializeComponent();
    }

    public XboxCapes(string xbl) : this()
    {
        _xbl = xbl;
        MainContent.IsVisible = false;
        Task.Run(async () =>
        {
            var capes = await CapeApiClient.GetPlayerCapesAsync(_xbl);
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                LoadingCard.IsVisible = false;
                MainContent.IsVisible = true;

                ListPanel.Children.Clear();
                capes.Capes.ForEach(cape => { ListPanel.Children.Add(new CapeItem(cape)); });
            });
        });
    }
}