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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Standard.Entity.Progress;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Models.Global;
using Octokit;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogDownloadAppSdkContent : UserControl
{
    public DialogDownloadAppSdkContent()
    {
        InitializeComponent();
        _ = Update();
    }

    public async Task Update()
    {
        string owner = "BE-Community-Dev";
        string repo = "AppSDKArchive";
        string tag = "1.8";

        try
        {
            DialogHost.Show(new DialogInfo()
            {
                Content = I18nManager.Instance["Sdk.Dialog.SelectMethod"],
                Title = I18nManager.Instance["Sdk.Dialog.Title"],
                PrimaryButtonText = I18nManager.Instance["Sdk.Dialog.Method1"],
                CloseButtonText = I18nManager.Instance["Sdk.Dialog.Method2"],
                AccountButton = DialogButtons.CloseButton,
                PrimaryAction = new(async () =>
                {
                    var client = new GitHubClient(new ProductHeaderValue("BedrockBoot.Desktop"));
                    var release = await client.Repository.Release.Get(owner, repo, tag);

                    if (release == null)
                    {
                        return;
                    }

                    if (release.Assets != null && release.Assets.Count > 0)
                    {
                        var downloadUrl = release.Assets[0].BrowserDownloadUrl;
                        var downloader = new GithubFilesDownloader();
                        var savePath = Path.Combine(PathsList.TempPath, $"sdkinstaller_{Guid.NewGuid()}.exe");

                        await downloader.DownloadAsync(downloadUrl, savePath, new Progress<DownloadProgress>(p =>
                        {
                            Dispatcher.UIThread.Invoke(() =>
                            {
                                if (ProgressBar.IsIndeterminate)
                                    ProgressBar.IsIndeterminate = false;

                                ProgressBar.Value = p.ProgressPercentage;
                                ProgressText.Text = string.Format(I18nManager.Instance["Sdk.Dialog.DownloadDependency"], p.ProgressPercentage);
                            });
                        }));

                        if (File.Exists(savePath))
                        {
                            await RunAsAdministratorAndWaitAsync(savePath);
                            DialogHost.Close();
                        }
                    }
                }),
                CloseAction = async () =>
                {
                    var downloader = new MultiThreadDownloader();
                    var downloadUrl = "https://aka.ms/windowsappsdk/1.8/1.8.260710003/windowsappruntimeinstall-x64.exe";

                    var savePath = Path.Combine(PathsList.TempPath, $"sdkinstaller_{Guid.NewGuid()}.exe");
                    await downloader.DownloadAsync(downloadUrl, savePath, new Progress<DownloadProgress>(p =>
                    {
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            if (ProgressBar.IsIndeterminate)
                                ProgressBar.IsIndeterminate = false;

                            ProgressBar.Value = p.ProgressPercentage;
                            ProgressText.Text = string.Format(I18nManager.Instance["Sdk.Dialog.DownloadDependency"], p.ProgressPercentage);
                        });
                    }));

                    if (File.Exists(savePath))
                    {
                        await RunAsAdministratorAndWaitAsync(savePath);
                        DialogHost.Close();
                    }
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            DialogHost.Close();
            DialogHost.Show(new()
            {
                Title = I18nManager.Instance["Download.Mgr.DownloadFailed"],
                Content = ex.Message,
                CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"]
            });
        }
    }

    private async Task RunAsAdministratorAndWaitAsync(string filePath)
    {
        Process process = null;
        while (process == null)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                process = Process.Start(processInfo);
            }
            catch
            {
                await Task.Delay(500);
            }
        }

        await process.WaitForExitAsync();
    }
}