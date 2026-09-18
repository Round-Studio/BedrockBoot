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
using BedrockBoot.Base.Enum;
using BedrockBoot.Chunker.Base.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Chunker;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent.Chunker;

public partial class DialogChunkerConversionContent : UserControl
{
    public DialogChunkerConversionContent()
    {
        InitializeComponent();
    }

    public DialogChunkerConversionContent(
        ChunkerType type,
        SaveType saveType,
        string gameVersion,
        string archivePath,
        string savePath,
        Action<string>? complete = null) : this()
    {
        GlobalModel.MainWindow.CloseDraw();

        Task.Run(() =>
        {
            var chunkerHelper = new ChunkerHelper(type, gameVersion, archivePath,
                BedrockBoot.Chunker.Chunker.DefaultJvmInfo,
                new Progress<double>(p =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        ProgressText.Text = $"转换中... ({p:F2} %)";
                        ProgressBar.Value = p;
                    });
                }));

            if (saveType == SaveType.File)
                chunkerHelper.ConversionToFile(savePath);
            else
                chunkerHelper.ConversionToFolder(savePath);

            Dispatcher.UIThread.Invoke(DialogHost.Close);
            complete?.Invoke(savePath);
        });
    }
}