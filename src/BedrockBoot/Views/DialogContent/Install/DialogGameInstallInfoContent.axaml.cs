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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Core.Global;
using BedrockBoot.Services;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.View;

namespace BedrockBoot.Views.DialogContent.Install;

public partial class DialogGameInstallInfoContent : UserControl
{
    public DialogGameInstallInfoContent()
    {
        InitializeComponent();
    }

    public DialogGameInstallInfoContent(BuildInfo info) : this()
    {
        BuildInfo = info;
        UpdateUI();
    }

    private static I18nManager i18n => I18nManager.Instance;
    public List<GameDownloadUrlInfo>? Sources { get; set; }
    public BuildInfo BuildInfo { get; set; } = null!;

    private bool _sourcesReady;

    public string InstanceName => InstallName.Text;

    public int SelectedFolderIndex => InstallFolder.SelectedIndex;

    public int SelectedSourceIndex => SourceSelBox.SelectedIndex;

    public bool UsePack => IsUsePackIns.IsChecked ?? false;

    public bool SourcesReady => _sourcesReady;

    public void UpdateUI()
    {
        InstallFolder.Items.Clear();
        var folders = GlobalModel.Config.Data.GameFolders;
        if (folders is { Count: > 0 })
        {
            foreach (var folder in folders)
                InstallFolder.Items.Add($"[{folder.GameFolderName}] {folder.GameFolderPath}");
            InstallFolder.SelectedIndex = Math.Clamp(GlobalModel.Config.Data.GameFolderSelIndex, 0, folders.Count - 1);
        }

        InstallName.Text = BuildInfo.Key;
        SourceSelBox.Items.Clear();
        LoadRing.IsVisible = true;

        CheckPack();

        Task.Run(async () =>
        {
            try
            {
                Sources = await EasyDownload.GetPackageUrls(BuildInfo);

                if (Sources == null || Sources.Count == 0)
                {
                    await ShowErrorDialog(i18n["Download.Draw.Error.NoUrl"]);
                    return;
                }

                var hasBestSourceSet = false;
                var itemList = new List<GameDownloadSourceItem>();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    for (var i = 0; i < Sources.Count; i++)
                    {
                        var urlInfo = Sources[i];
                        var item = new GameDownloadSourceItem(urlInfo)
                        {
                            Height = 64,
                            Width = 220
                        };
                        var currentIndex = i;

                        item.Pinged = index =>
                        {
                            if (!hasBestSourceSet)
                            {
                                hasBestSourceSet = true;
                                Dispatcher.UIThread.Invoke(() =>
                                {
                                    _sourcesReady = true;
                                    LoadRing.IsVisible = false;
                                    SourceSelBox.SelectedIndex = index;
                                });
                            }
                        };

                        itemList.Add(item);
                        SourceSelBox.Items.Add(new ItemViewItem()
                        {
                            Content = item,
                            Padding = new Thickness(8)
                        });
                    }
                });

                for (var i = 0; i < itemList.Count; i++) itemList[i].OnPing(i);
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"{i18n["MainWindow.Dialog.Error.Title"]}: {ex.Message}");
            }
        });
    }

    private async Task ShowErrorDialog(string message)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            DialogHost.Show(new DialogInfo
            {
                Title = i18n["MainWindow.Dialog.Error.Title"],
                Content = message,
                CloseButtonText = i18n["MainWindow.Common.Confirm"],
                CloseAction = () => Models.Global.GlobalModel.MainWindow.CloseDraw()
            });
        });
    }

    private void InstallBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (InstallFolder.Items.Count <= 0)
        {
            var dialog = new DialogAddGameFolderContent();
            DialogHost.Show(new DialogInfo
            {
                Title = i18n["Download.Draw.AddFolder.Title"],
                Content = dialog,
                CloseButtonText = i18n["MainWindow.Common.Add"],
                SecondaryButtonText = i18n["MainWindow.Common.Cancel"],
                AccountButton = DialogButtons.CloseButton,
                CloseAction = () =>
                {
                    if (Directory.Exists(dialog.FolderPath))
                    {
                        var name = string.IsNullOrEmpty(dialog.FolderName)
                            ? Path.GetFileName(Path.GetDirectoryName(dialog.FolderPath))
                            : dialog.FolderName;

                        GlobalModel.Config.Data.GameFolders ??= new List<GameFolderInfo>();
                        GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo
                        {
                            GameFolderPath = dialog.FolderPath,
                            GameFolderName = name ?? "Minecraft"
                        });
                        GlobalModel.Config.Save();

                        UpdateUI();
                        ExecuteInstallTask();
                    }
                }
            });
        }
        else
        {
            ExecuteInstallTask();
        }
    }

    public void ExecuteInstallTask()
    {
        var usePack = UsePack;
        var hasLocalPack = usePack && HasAnyCachedPack();

        if (!hasLocalPack)
        {
            if (Sources == null || Sources.Count == 0)
            {
                ShowErrorDialogAsync(i18n["Download.Draw.Error.NoUrl"]);
                return;
            }

            if (SourceSelBox.SelectedIndex < 0 || SourceSelBox.SelectedIndex >= Sources.Count)
            {
                ShowErrorDialogAsync("请选择一个有效的下载源");
                return;
            }

            var selectedSource = Sources[SourceSelBox.SelectedIndex];
            if (selectedSource == null || string.IsNullOrEmpty(selectedSource.Url))
            {
                ShowErrorDialogAsync("选中的下载源无效");
                return;
            }
        }

        if (InstallFolder.SelectedIndex < 0 || InstallFolder.SelectedIndex >= InstallFolder.Items.Count)
        {
            ShowErrorDialogAsync("请选择一个有效的安装目录");
            return;
        }

        var gameFolders = GlobalModel.Config.Data.GameFolders;
        if (gameFolders == null || InstallFolder.SelectedIndex >= gameFolders.Count)
        {
            ShowErrorDialogAsync("游戏目录配置无效");
            return;
        }

        var targetFolder = gameFolders[InstallFolder.SelectedIndex];
        if (targetFolder == null || string.IsNullOrEmpty(targetFolder.GameFolderPath))
        {
            ShowErrorDialogAsync("安装目录路径无效");
            return;
        }

        var targetPath = targetFolder.GameFolderPath;

        if (!Directory.Exists(targetPath))
        {
            try
            {
                Directory.CreateDirectory(targetPath);
            }
            catch (Exception ex)
            {
                ShowErrorDialogAsync($"无法创建安装目录: {ex.Message}");
                return;
            }
        }

        string selectedUrl = null;
        if (Sources != null &&
            SourceSelBox.SelectedIndex >= 0 &&
            SourceSelBox.SelectedIndex < Sources.Count)
        {
            selectedUrl = Sources[SourceSelBox.SelectedIndex].Url;
        }

        TaskDownloadGameItem.Install(
            BuildInfo,
            selectedUrl,
            usePack,
            targetPath,
            InstallName.Text
        );
    }

    private async void ShowErrorDialogAsync(string message)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            DialogHost.Show(new DialogInfo
            {
                Title = i18n["MainWindow.Dialog.Error.Title"],
                Content = message,
                CloseButtonText = i18n["MainWindow.Common.Confirm"],
                CloseAction = () => Models.Global.GlobalModel.MainWindow?.CloseDraw()
            });
        });
    }

    private void IsUsePackIns_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        SourceGrid.IsVisible = IsUsePackIns.IsChecked != true;
    }

    private void InstallFolder_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (InstallFolder.SelectedIndex < 0) return;

        try
        {
            CheckPack();
        }
        catch
        {
        }
    }

    private bool HasAnyCachedPack()
    {
        var selectedFolder = GlobalModel.Config.Data.GameFolders
            .ElementAtOrDefault(InstallFolder.SelectedIndex);
        if (selectedFolder == null) return false;

        var packagePath = Path.Combine(selectedFolder.GameFolderPath, "version_save", $"{BuildInfo.ID}.insPack");
        if (File.Exists(packagePath)) return true;

        return Core.Models.Helper.GamePackageCacheIndex.Find(
            BuildInfo.ID, BuildInfo.BuildType.ToString()) != null;
    }

    private bool CheckPack()
    {
        var selectedFolder = GlobalModel.Config.Data.GameFolders
            .ElementAtOrDefault(InstallFolder.SelectedIndex);

        if (selectedFolder == null)
        {
            IsUsePackIns.IsChecked = false;
            IsUsePackIns.IsVisible = false;
            return false;
        }

        var hasPack = HasAnyCachedPack();
        IsUsePackIns.IsChecked = hasPack;
        IsUsePackIns.IsVisible = hasPack;

        return hasPack;
    }
}