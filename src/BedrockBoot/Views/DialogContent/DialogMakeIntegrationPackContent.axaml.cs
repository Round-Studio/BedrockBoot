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
using BedrockBoot.Base.Entry.Game.Pack.Integration;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Integration;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogMakeIntegrationPackContent : UserControl
{
    public DialogMakeIntegrationPackContent()
    {
        InitializeComponent();
    }

    public DialogMakeIntegrationPackContent(PackInfo packInfo) : this()
    {
        PackInfo = packInfo;
        // 确保在渲染后开始执行
        StartPackaging();
    }

    private static I18nManager i18n => I18nManager.Instance;
    public PackInfo? PackInfo { get; set; }

    /// <summary>
    ///     开始打包整合包
    /// </summary>
    public void StartPackaging()
    {
        if (PackInfo == null) return;

        Task.Run(() =>
        {
            try
            {
                var packer = new IntegrationPackager(PackInfo.VersionConfig);

                // 进度回调逻辑
                packer.IntegrationProgress = new Progress<IntegrationProgress>(progress =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        // 首次接收到具体进度时关闭不确定状态
                        if (ProgressBar.IsIndeterminate)
                            ProgressBar.IsIndeterminate = false;

                        ProgressBar.Value = progress.Progress;

                        // 格式化输出：例如 (85.50 %) 正在压缩资源...
                        ProgressText.Text = $"({progress.Progress:F2} %) {progress.Message}";
                    });
                });

                // 完成回调逻辑
                packer.CompleteCallBack = () => Dispatcher.UIThread.Invoke(() =>
                {
                    DialogHost.Close();

                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                    {
                        NoticeType = NoticeType.Info,
                        Title = i18n["Instance.Title"], // "实例"
                        Message = i18n["Instance.Pack.Export.Success"] // "整合包导出完毕"
                    });
                });

                // 开始执行打包流程
                packer.BeginPack(PackInfo);
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    DialogHost.Close();
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                    {
                        NoticeType = NoticeType.Error,
                        Title = i18n["MainWindow.Dialog.Error.Title"],
                        Message = ex.Message
                    });
                });
            }
        });
    }
}