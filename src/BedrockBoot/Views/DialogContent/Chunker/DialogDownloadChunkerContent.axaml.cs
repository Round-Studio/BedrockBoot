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
using BedrockBoot.Chunker.Base.Enum;
using BedrockBoot.Chunker.Event;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent.Chunker;

public partial class DialogDownloadChunkerContent : UserControl
{
    public DialogDownloadChunkerContent()
    {
        InitializeComponent();
    }

    public void Download(Action action)
    {
        Task.Run(async () =>
        {
            await BedrockBoot.Chunker.Chunker.DownloadChunker(DownloadType.Github,
                new Progress<DownloadProgressEventArgs>(pro =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        ProgressText.Text = $"下载 Chunker ({pro.Percentage:F2} %)";
                        ProgressBar.Value = pro.Percentage;
                    });
                }));

            Dispatcher.UIThread.Invoke(DialogHost.Close);
            action.Invoke();
        });
    }
}