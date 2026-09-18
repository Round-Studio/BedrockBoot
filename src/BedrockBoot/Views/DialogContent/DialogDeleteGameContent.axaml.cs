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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogDeleteGameContent : UserControl
{
    public DialogDeleteGameContent()
    {
        InitializeComponent();
    }

    public DialogDeleteGameContent(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;

        Delete();
    }

    public VersionConfig VersionInfo { get; set; }

    public void Delete()
    {
        Task.Run(() =>
        {
            var path = VersionInfo.VersionPath;
            Console.WriteLine($@"即将删除文件夹：{path}");

            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            Console.WriteLine($@"总数目：{files.Length}");
            Dispatcher.UIThread.Invoke(() => DeleteProgressBar.Maximum = files.Length);

            Dispatcher.UIThread.Invoke(() => DeleteProgressBar.IsIndeterminate = false);

            var jd = 0;
            files.ToList().ForEach(file =>
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                jd++;
                if (jd % 200 == 0)
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        DeleteProgressText.Text = $"进度：{jd * 100.0 / files.Length:F2} %";
                        DeleteProgressBar.Value = jd;
                    });
            });

            try
            {
                Directory.Delete(path, true);
            }
            catch
            {
            }

            Dispatcher.UIThread.Invoke(DialogHost.Close);
            Dispatcher.UIThread.Invoke(GlobalModel.MainWindow.CloseDraw);
        });
    }
}