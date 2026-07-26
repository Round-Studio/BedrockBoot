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
using BedrockBoot.Core.Global;
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
    public DrawDownloadGameContent()
    {
        InitializeComponent();
    }

    public DrawDownloadGameContent(BuildInfo info) : this()
    {
        BuildInfo = info;
        UpdateUI();
    }

    private static I18nManager i18n => I18nManager.Instance;
    public List<GameDownloadUrlInfo>? Sources { get; set; }
    public BuildInfo BuildInfo { get; set; } = null!;

    /// <summary>下载源是否已就绪（至少一个源 Ping 通）。用于在无缓存时决定安装按钮可用性。</summary>
    private bool _sourcesReady;

    /// <summary>
    ///     初始化 UI 与下载源列表
    /// </summary>
    public void UpdateUI()
    {
        // 1. 初始化安装目录下拉框
        InstallFolder.Items.Clear();
        var folders = GlobalModel.Config.Data.GameFolders;
        if (folders is { Count: > 0 })
        {
            foreach (var folder in folders)
                InstallFolder.Items.Add($"[{folder.GameFolderName}] {folder.GameFolderPath}");
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

                var hasBestSourceSet = false;
                var itemList = new List<GameDownloadSourceItem>();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    for (var i = 0; i < Sources.Count; i++)
                    {
                        var urlInfo = Sources[i];
                        var item = new GameDownloadSourceItem(urlInfo);
                        var currentIndex = i;

                        // 当某个源 Ping 通后的回调
                        item.Pinged = index =>
                        {
                            if (!hasBestSourceSet)
                            {
                                hasBestSourceSet = true;
                                Dispatcher.UIThread.Invoke(() =>
                                {
                                    _sourcesReady = true;
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

    /// <summary>
    ///     安装按钮逻辑
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
        // 使用纯查询，且尊重用户对"使用缓存"勾选框的手动选择。
        // 此前这里调用 CheckPack() 会把勾选框强制重置为 hasPack，
        // 用户取消勾选想强制重新下载时会被无声改回使用缓存。
        var usePack = IsUsePackIns.IsChecked ?? false;
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

            // 检查选中的下载源是否有效
            var selectedSource = Sources[SourceSelBox.SelectedIndex];
            if (selectedSource == null || string.IsNullOrEmpty(selectedSource.Url))
            {
                ShowErrorDialogAsync("选中的下载源无效");
                return;
            }
        }

        // 检查安装目录是否有效
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

        // 即使使用缓存也尽量携带下载地址：
        // 万一缓存文件校验失败，EasyDownload 仍可回退到网络下载
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

        Models.Global.GlobalModel.MainWindow.CloseDraw();
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
            /* 路径无效忽略 */
        }
    }

    /// <summary>
    /// 纯查询：当前选中目录或全局缓存索引中是否存在该版本的缓存包（不修改任何 UI 状态）
    /// </summary>
    private bool HasAnyCachedPack()
    {
        var selectedFolder = GlobalModel.Config.Data.GameFolders
            .ElementAtOrDefault(InstallFolder.SelectedIndex);
        if (selectedFolder == null) return false;

        var packagePath = Path.Combine(selectedFolder.GameFolderPath, "version_save", $"{BuildInfo.ID}.insPack");
        if (File.Exists(packagePath)) return true;

        // 全局缓存索引：其他安装目录缓存的同版本包也可复用（EasyDownload 会自动检测）
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
            InstallBtn.IsEnabled = _sourcesReady;
            return false;
        }

        var hasPack = HasAnyCachedPack();
        IsUsePackIns.IsChecked = hasPack;
        IsUsePackIns.IsVisible = hasPack;

        // 注意：不能无条件用 hasPack 覆盖按钮可用性。
        // 此前切换到没有缓存的目录会把按钮永久禁用（下载源早已 Ping 通，但无人再启用它）。
        InstallBtn.IsEnabled = hasPack || _sourcesReady;

        return hasPack;
    }
}