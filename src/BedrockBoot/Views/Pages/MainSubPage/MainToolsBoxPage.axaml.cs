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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Standard.Entity;
using BedrockBoot.Chunker.Jvm;
using BedrockBoot.Models;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DialogContent.Chunker;
using BedrockBoot.Views.Windows.SubWindows;
using BedrockBoot.Standard.Core;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainToolsBoxPage : BedrockBootPage
{
    public MainToolsBoxPage()
    {
        InitializeComponent();

#if RELEASE
        TranslateResourcePack.IsVisible = BedrockBoot.Models.Global.GlobalModel.FunctionOption.IsEnableToolsBoxUsingPackTranslate;
#endif
    }

    private static I18nManager i18n => I18nManager.Instance;

    /// <summary>
    ///     修复丢失的游戏文件
    /// </summary>
    private void FoundLoseFilesBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var rfw = new RecoverFilesWindow();
        rfw.ShowDialog(GlobalModel.MainWindow);
    }

    /// <summary>
    ///     卸载所有已安装的 UWP 游戏组件
    /// </summary>
    private async void DeleteMcBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // 显示正在卸载的进度提示
        DialogHost.Show(new DialogInfo
        {
            Title = i18n["MainPage.Tools.Uninstall.Dialog.Title"],
            Content = i18n["MainPage.Tools.Uninstall.Dialog.Content"]
        });

        try
        {
            // 依次移除不同版本的 UWP 实例
#if WINDOWS
            await CoreGlobal.BedrockCore.RemoveUWPGameAsync(MinecraftGameTypeVersion.Release);
            await CoreGlobal.BedrockCore.RemoveUWPGameAsync(MinecraftGameTypeVersion.Preview);
            await CoreGlobal.BedrockCore.RemoveUWPGameAsync(MinecraftGameTypeVersion.Beta);
#endif
        }
        catch (Exception ex)
        {
            // 如果卸载过程中出现异常，可以在此处捕获并记录
            Console.WriteLine($@"Uninstall failed: {ex.Message}");
        }
        finally
        {
            // 无论成功与否，任务结束后关闭对话框
            DialogHost.Close();
        }
    }

    private void WorldShift_OnClick(object? sender, RoutedEventArgs e)
    {
        void OpenDialog()
        {
            Task.Run(async () =>
            {
                if (Chunker.Chunker.DefaultJvmInfo == null)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        DialogHost.Show(new DialogInfo
                        {
                            Title = I18nManager.Instance["Tools.SaveConvert"],
                            Content = I18nManager.Instance["Tools.Jvm.Fetching"]
                        });
                    });

                    var jvms = await JavaUtil.GetJavaListAsync();
                    jvms.ForEach(j => Console.WriteLine($@"Find Jvm: {j.JavaPath}"));
                    var jvm = jvms.Find(j => j.MajorVersion >= 17);

                    Dispatcher.UIThread.Invoke(DialogHost.Close);

                    if (jvm == null)
                    {
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            DialogHost.Show(new DialogInfo
                            {
                                Title = I18nManager.Instance["Tools.Jvm.Error"],
                                Content = I18nManager.Instance["Tools.Jvm.NotFound"],
                                CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"]
                            });
                        });
                        return;
                    }

                    Chunker.Chunker.DefaultJvmInfo = jvm;
                }

                Dispatcher.UIThread.Invoke(() =>
                {
                    DialogHost.Show(new DialogInfo
                    {
                        Title = I18nManager.Instance["Tools.SaveConvert"],
                        Content = new DialogChooseChunkerTypeContent(),
                        CloseButtonText = I18nManager.Instance["Shared.Action.Cancel"]
                    });
                });
            });
        }

        if (!Chunker.Chunker.CheckChunker())
        {
            var dialog = new DialogDownloadChunkerContent();
            DialogHost.Show(new DialogInfo
            {
                Title = I18nManager.Instance["Tools.DownloadDependency"],
                Content = dialog
            });

            dialog.Download(OpenDialog);
        }
        else
        {
            OpenDialog();
        }
    }

    private async void TranslateResourcePack_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = I18nManager.Instance["Tools.Translate.ImportPack"],
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(I18nManager.Instance["Tools.Translate.PackFileType"])
                {
                    Patterns = new[] { "*.mcpack", "*.mcaddon" }
                }
            }
        });

        if (files.Count > 0)
        {
            var selectedFile = files[0].Path.LocalPath;

            var saveFile = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = I18nManager.Instance["Tools.Translate.SaveAs"],
                DefaultExtension = Path.GetExtension(selectedFile),
                FileTypeChoices = new[]
                {
                    new FilePickerFileType(I18nManager.Instance["FileType.MinecraftSupport"])
                    {
                        Patterns = new[] { Path.GetExtension(selectedFile) }
                    }
                }
            });

            if (saveFile != null)
            {
                var saveFilePath = saveFile.Path.LocalPath;
                var inputFile = selectedFile;

                DialogHost.Show(new DialogInfo
                {
                    Title = I18nManager.Instance["Tools.Translate.Title"],
                    Content = new DialogTranslateResourcePackContent(inputFile, saveFilePath)
                });
            }
        }
    }

    private void ResourcePackShift_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo()
        {
            Title = I18nManager.Instance["Shared.Label.Notice"],
            Content = I18nManager.Instance["Tools.Translate.Warn.Line1"] +
                      I18nManager.Instance["Tools.Translate.Warn.Line2"] +
                      "\n" +
                      I18nManager.Instance["Tools.Translate.Warn.Line3"],
            CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
            PrimaryButtonText = I18nManager.Instance["Shared.Action.Cancel"],
            CloseAction = async () =>
            {
                var topLevel = TopLevel.GetTopLevel(GlobalModel.MainWindow);
                if (topLevel == null) return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = I18nManager.Instance["Tools.Convert.ImportPack"],
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType(I18nManager.Instance["Tools.Convert.JavaPackFileType"])
                        {
                            Patterns = new[] { "*.zip" }
                        }
                    }
                });

                if (files.Count > 0)
                {
                    var selectedFile = files[0].Path.LocalPath;

                    var saveFile = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = I18nManager.Instance["Tools.Convert.SaveAs"],
                        DefaultExtension = "mcpack",
                        FileTypeChoices = new[]
                        {
                            new FilePickerFileType(I18nManager.Instance["FileType.MinecraftBedrockResource"])
                            {
                                Patterns = new[] { "*.mcpack" }
                            }
                        }
                    });

                    if (saveFile != null)
                    {
                        var saveFilePath = saveFile.Path.LocalPath;
                        var inputFile = selectedFile;

                        DialogHost.Show(new DialogInfo
                        {
                            Title = I18nManager.Instance["Tools.Convert.Title"],
                            Content = new DialogJeToBeResourcePackContent(inputFile, saveFilePath)
                        });
                    }
                }
            }
        });
    }

    private void CheckSDK_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = CoreInitialize.GetSdkInstalledMode();
    }
}