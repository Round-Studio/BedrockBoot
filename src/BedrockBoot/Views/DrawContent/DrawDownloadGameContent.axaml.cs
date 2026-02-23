using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Models.Global;
using BedrockBoot.Services;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawDownloadGameContent : UserControl
{
    private static I18nManager i18n => I18nManager.Instance;
    public List<GameDownloadUrlInfo>? Sources { get; set; }
    public BuildInfo BuildInfo { get; set; } = null!;

    public DrawDownloadGameContent()
    {
        InitializeComponent();
    }

    public DrawDownloadGameContent(BuildInfo info) : this()
    {
        BuildInfo = info;
        UpdateUI();
    }

    /// <summary>
    /// 初始化 UI 与下载源列表
    /// </summary>
    public void UpdateUI()
    {
        // 1. 初始化安装目录下拉框
        InstallFolder.Items.Clear();
        var folders = GlobalModel.Config.Data.GameFolders;
        if (folders is { Count: > 0 })
        {
            foreach (var folder in folders)
            {
                InstallFolder.Items.Add($"[{folder.GameFolderName}] {folder.GameFolderPath}");
            }
            InstallFolder.SelectedIndex = Math.Clamp(GlobalModel.Config.Data.GameFolderSelIndex, 0, folders.Count - 1);
        }

        InstallName.Text = BuildInfo.ID;
        SourceSelBox.Items.Clear();
        LoadRing.IsVisible = true;
        InstallBtn.IsEnabled = false;

        CheckPack();

        // 2. 异步获取下载地址
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

                bool hasBestSourceSet = false;
                var itemList = new List<GameDownloadSourceItem>();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    for (int i = 0; i < Sources.Count; i++)
                    {
                        var urlInfo = Sources[i];
                        var item = new GameDownloadSourceItem(urlInfo);
                        int currentIndex = i;

                        // 当某个源 Ping 通后的回调
                        item.Pinged = index =>
                        {
                            if (!hasBestSourceSet)
                            {
                                hasBestSourceSet = true;
                                Dispatcher.UIThread.Invoke(() =>
                                {
                                    LoadRing.IsVisible = false;
                                    InstallBtn.IsEnabled = true;
                                    SourceSelBox.SelectedIndex = index;
                                });
                            }
                        };

                        itemList.Add(item);
                        SourceSelBox.Items.Add(new ListBoxItem { Content = item });
                    }
                });

                // 启动所有源的 Ping 测试
                for (int i = 0; i < itemList.Count; i++)
                {
                    itemList[i].OnPing(i);
                }
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
                CloseAction = () => GlobalModel.MainWindow.CloseDraw()
            });
        });
    }

    /// <summary>
    /// 安装按钮逻辑
    /// </summary>
    private void InstallBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // 如果没有配置游戏目录，弹出添加对话框
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

    private void ExecuteInstallTask()
    {
        if ((Sources == null || SourceSelBox.SelectedIndex < 0) && !CheckPack()) return;

        var selectedUrl = Sources[SourceSelBox.SelectedIndex].Url;
        var targetPath = GlobalModel.Config.Data.GameFolders[InstallFolder.SelectedIndex].GameFolderPath;

        TaskDownloadGameItem.Install(
            BuildInfo, 
            selectedUrl, 
            IsUsePackIns.IsChecked ?? false,
            targetPath, 
            InstallName.Text
        );

        GlobalModel.MainWindow.CloseDraw();
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
        catch { /* 路径无效忽略 */ }
    }

    private bool CheckPack()
    {
        var selectedFolder = GlobalModel.Config.Data.GameFolders
            .ElementAtOrDefault(InstallFolder.SelectedIndex);
    
        if (selectedFolder == null)
        {
            IsUsePackIns.IsChecked = false;
            IsUsePackIns.IsVisible = false;
            InstallBtn.IsEnabled = false;
            return false;
        }

        var folderPath = selectedFolder.GameFolderPath;
        var packagePath = Path.Combine(folderPath, "version_save", $"{BuildInfo.ID}.insPack");

        var hasPack = File.Exists(packagePath);
        IsUsePackIns.IsChecked = hasPack;
        IsUsePackIns.IsVisible = hasPack;
        InstallBtn.IsEnabled = hasPack;

        return hasPack;
    }
}