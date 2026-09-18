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
using BedrockBoot.Models.Pack.LeviLamina;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent.Loader.LeviLamina;

public partial class DialogInstallLeviLaminaModContent : UserControl
{
    private readonly string _versionId;
    private readonly string _savePath;
    private readonly string _key;
    private readonly bool _isOnlyDownload;
    private LipClient _client;

    public DialogInstallLeviLaminaModContent()
    {
        InitializeComponent();
    }

    public DialogInstallLeviLaminaModContent(string versionId, string savePath, string key, bool isOnlyDownload = false)
        : this()
    {
        _versionId = versionId;
        _savePath = savePath;
        _key = key;
        _isOnlyDownload = isOnlyDownload;

        Install();
    }

    public async void Install()
    {
        try
        {
            var toothKey = $"{_key}#client@{_versionId}";
            _client = new LipClient(_savePath);

            _client.ProgressChanged += (sender, progress) =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    InstallProgressBar.IsIndeterminate = false;
                    InstallProgressText.Text = $"{progress} %";
                    InstallProgressBar.Value = progress;

                    // 当进度达到100%时关闭对话框
                    if (progress >= 100)
                    {
                        DialogHost.Close();
                    }
                });
            };

            await _client.InstallAsync(toothKey);
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                InstallProgressText.Text = $"安装失败: {ex.Message}";
                InstallProgressBar.IsIndeterminate = false;
            });

            // 延迟关闭对话框，让用户看到错误信息
            await Task.Delay(3000);
            Dispatcher.UIThread.Invoke(DialogHost.Close);
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
        _client = null;
    }
}