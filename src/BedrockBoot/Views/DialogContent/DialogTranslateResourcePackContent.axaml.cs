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
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Models.Translate;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogTranslateResourcePackContent : UserControl
{
    public DialogTranslateResourcePackContent()
    {
        InitializeComponent();
    }

    public DialogTranslateResourcePackContent(string input, string save) : this()
    {
        Task.Run(() =>
        {
            var translator = new ResourcePackTranslate(new MicrosoftTranslateService());

            translator.TranslatePackageAsync(
                input,
                "zh_CN",
                save,
                (progress, status) =>
                {
                    Console.WriteLine($@"进度: {progress:F2}% - {status}");
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ProgressBar.Value = progress;
                        ProgressText.Text = $"{status} ({progress:F2} %)";
                    });

                    if (progress == 100) Dispatcher.UIThread.InvokeAsync(DialogHost.Close);
                }
            );
        });
    }
}