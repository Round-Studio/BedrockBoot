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
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Models.Translate;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using ResourcePackConvert.Core.Models;
using ResourcePackConvert.Core.Services;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogJeToBeResourcePackContent : UserControl
{
    public DialogJeToBeResourcePackContent()
    {
        InitializeComponent();
    }

    public DialogJeToBeResourcePackContent(string input, string save) : this()
    {
        var resCon =
            new ResourcePackConvertConverter(
                progress: new Progress<ConversionProgress>(p =>
                {
                    Console.WriteLine($@"进度: {p.Percentage:F2}% - {p.Message}");
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ProgressBar.Value = p.Percentage;
                        ProgressText.Text = $"{p.Stage} ({p.Percentage:F2} %)";
                    });

                    if (p.Percentage == 100) Dispatcher.UIThread.InvokeAsync(DialogHost.Close);
                }));
        
        Task.Run(() =>
        {
            resCon.ConvertResourcePack(input, save);
        });
    }
}