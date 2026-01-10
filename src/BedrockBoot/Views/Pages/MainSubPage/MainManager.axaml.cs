using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.Control;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.TaskItem;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainManager : BedrockBootPage
{
    public bool IsEditMode { get; set; } = false;
    private FileSystemWatcher _configWatcher;
    public static MainManager Instance { get; private set; }
    private string SearchKey = "";
    private string GameType = "";

    public MainManager()
    {
        Instance = this;
        InitializeComponent();

        UpdateUI();

#if RELEASE
        ImportGameBtn.IsVisible = GlobalModel.FunctionOption.IsEnableImportGamePack;
#endif
    }

    private void InitializeConfigWatcher()
    {
        // 先清理现有的监听器
        CleanupConfigWatcher();

        try
        {
            if (GlobalModel.Config.Data.GameFolders.Count == 0)
                return;

            // 获取当前选中的游戏文件夹路径
            var currentFolder = GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex];
            string gameFolderPath = currentFolder.GameFolderPath;

            if (!Directory.Exists(gameFolderPath))
                return;

            // 创建 FileSystemWatcher
            _configWatcher = new FileSystemWatcher
            {
                Path = gameFolderPath,
                Filter = "config.json",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true // 监听子目录
            };

            // 注册事件处理
            _configWatcher.Changed += OnConfigFileChanged;
            _configWatcher.Deleted += OnConfigFileChanged;

            // 开始监听
            _configWatcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"初始化配置文件监听失败: {ex.Message}");
        }
    }

    private void CleanupConfigWatcher()
    {
        if (_configWatcher != null)
        {
            _configWatcher.EnableRaisingEvents = false;
            _configWatcher.Changed -= OnConfigFileChanged;
            _configWatcher.Dispose();
            _configWatcher = null;
        }
    }

    private async void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            // 只在文件在 bedrock_versions 目录或其子目录中时刷新
            if (e.FullPath.Contains("bedrock_versions", StringComparison.OrdinalIgnoreCase))
            {
                // 在主线程中更新UI
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Console.WriteLine($"检测到文件变化: {e.ChangeType} - {e.FullPath}");
                    UpdateGameList();
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"配置文件变化处理失败: {ex.Message}");
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        CleanupConfigWatcher();
    }

    public void UpdateUI()
    {
        IsEditMode = false;

        if (GlobalModel.Config.Data.GameFolders.Count <= 0)
        {
            FolderList.IsVisible = false;
            FolderNull.IsVisible = true;
        }
        else
        {
            FolderList.IsVisible = true;
            FolderNull.IsVisible = false;
        }

        FolderList.SelectedIndex = -1;
        FolderList.Items.Clear();

        GlobalModel.Config.Data.GameFolders.ForEach(folder =>
        {
            FolderList.Items.Add(new ListBoxItem()
            {
                Content = new GameFolderItem(folder),
                VerticalAlignment = VerticalAlignment.Center
            });
        });

        if (GlobalModel.Config.Data.GameFolders.Count == 1)
            FolderList.SelectedIndex = 0;
        else
            FolderList.SelectedIndex = GlobalModel.Config.Data.GameFolderSelIndex;

        // 初始化或重新初始化文件监听
        InitializeConfigWatcher();

        UpdateGameList();

        IsEditMode = true;
    }

    public void UpdateGameList()
    {
        IsEditMode = false;

        // 安全校验索引
        if (GlobalModel.Config.Data.GameFolders.Count == 0)
        {
            ShowNoGamesUI(isNullBecauseNoFolder: true);
            IsEditMode = true;
            return;
        }

        if (GlobalModel.Config.Data.GameFolderSelIndex < 0 ||
            GlobalModel.Config.Data.GameFolderSelIndex >= GlobalModel.Config.Data.GameFolders.Count)
        {
            GlobalModel.Config.Data.GameFolderSelIndex = 0;
            GlobalModel.Config.Save();
        }

        var currentFolder = GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex];
        var versionsPath = Path.Combine(currentFolder.GameFolderPath, "bedrock_versions");

        // 判断 bedrock_versions 目录是否存在
        if (!Directory.Exists(versionsPath))
        {
            // 目录不存在 → 显示“无实例”，但可提示用户
            ShowNoGamesUI(isNullBecauseNoFolder: false);
            IsEditMode = true;
            return;
        }

        // 开始加载
        GamesLoad.IsVisible = true;
        GamesNull.IsVisible = false;
        GameScro.IsVisible = false;

        var lstDir = Directory.GetDirectories(versionsPath);
        var lst = new List<VersionConfig>();

        foreach (var dir in lstDir)
        {
            try
            {
                var info = GameInfoHelper.GetVersionConfig(dir);

                if (string.IsNullOrEmpty(info?.Info?.VersionName) ||
                    string.IsNullOrEmpty(info?.Info?.Version))
                    continue;

                // 搜索过滤
                if (!string.IsNullOrEmpty(SearchKey) &&
                    !info.Info.VersionName.Contains(SearchKey, StringComparison.OrdinalIgnoreCase) &&
                    !info.Info.Version.Contains(SearchKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                // 类型过滤
                var type = info.Info.VersionType == MinecraftGameTypeVersion.Release ? "Release" : "Preview";
                if (!string.IsNullOrEmpty(GameType) && GameType != type)
                    continue;

                lst.Add(info);
            }
            catch (FileNotFoundException ex)
            {
                DialogHost.Show(new DialogInfo()
                {
                    Title = "发生错误",
                    Content = ex.Message,
                    CloseButtonText = "好的"
                });
            }
            catch (Exception ex)
            {
                // 可选：记录日志，但不要中断
                Console.WriteLine($"加载版本目录失败: {dir}, 错误: {ex.Message}");
            }
        }

        // 更新 UI
        GameList.Children.Clear();

        if (lst.Count > 0)
        {
            foreach (var item in lst)
            {
                GameList.Children.Add(new GameItem(item));
            }

            GamesLoad.IsVisible = false;
            GameScro.IsVisible = true;
            GamesNull.IsVisible = false;
        }
        else
        {
            GamesLoad.IsVisible = false;
            GameScro.IsVisible = false;
            GamesNull.IsVisible = true; // 确实没有有效实例
        }

        IsEditMode = true;
    }

// 提取 UI 显示逻辑，便于维护
    private void ShowNoGamesUI(bool isNullBecauseNoFolder)
    {
        GamesLoad.IsVisible = false;
        GameScro.IsVisible = false;
        GamesNull.IsVisible = true;

        // 可选：根据 isNullBecauseNoFolder 改变提示文本
        // 例如：GamesNullText.Text = isNullBecauseNoFolder ? "请先添加游戏文件夹" : "该文件夹下没有 Bedrock 实例";
    }

    private void AddFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogAddGameFolderContent();

        DialogHost.Show(new DialogInfo()
        {
            Title = "添加游戏根目录",
            Content = dialog,
            CloseButtonText = "添加",
            SecondaryButtonText = "取消",
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                if (Directory.Exists(dialog.FolderPath))
                {
                    var name = string.IsNullOrEmpty(dialog.FolderName)
                        ? Path.GetFileName(Path.GetDirectoryName(dialog.FolderPath))
                        : dialog.FolderName;

                    GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo()
                    {
                        GameFolderPath = dialog.FolderPath,
                        GameFolderName = name
                    });
                    GlobalModel.Config.Save();

                    UpdateUI();
                }
            }
        });
    }

    private void FolderList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            GlobalModel.Config.Data.GameFolderSelIndex = FolderList.SelectedIndex;
            GlobalModel.Config.Save();

            // 当切换文件夹时重新初始化监听器
            InitializeConfigWatcher();

            UpdateGameList();
        }
    }

    private void ImportGameBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogImportGameContent();

        DialogHost.Show(new DialogInfo()
        {
            Title = "导入游戏安装包",
            Content = dialog,
            CloseButtonText = "开始导入",
            SecondaryButtonText = "取消",
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                var packPath = dialog.PackFile;
                var isGDK = dialog.IsGDK;
                var knowGameTypeCheckBox = dialog.DontKnowGameType;
                var installFolder = dialog.PackInstallFolder;
                var installName = dialog.PackInstallName;

                if (string.IsNullOrEmpty(packPath) || !File.Exists(packPath))
                {
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo()
                    {
                        Title = "错误",
                        Message = $"游戏包 {packPath} 无效",
                        NoticeType = NoticeType.Info
                    });
                    return;
                }

                if (string.IsNullOrEmpty(installFolder) || !Directory.Exists(installFolder))
                {
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo()
                    {
                        Title = "错误",
                        Message = $"文件夹 {installFolder} 无效",
                        NoticeType = NoticeType.Info
                    });
                    return;
                }

                if (string.IsNullOrEmpty(installName))
                {
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo()
                    {
                        Title = "错误",
                        Message = $"请输入有效的实例名称",
                        NoticeType = NoticeType.Info
                    });
                    return;
                }

                TaskImportGamePackItem.Install(packPath, installFolder, installName, dialog.GameType,
                    knowGameTypeCheckBox);
            }
        });
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        SearchKey = SearchBox.Text;

        UpdateGameList();
    }

    private void GameTypeSel_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            var tag = "";
            if (GameTypeSel != null)
                if (GameTypeSel.SelectedItem != null)
                {
                    var item = (ComboBoxItem)GameTypeSel.SelectedItem;
                    tag = item.Tag.ToString();
                }

            GameType = tag;
            try
            {
                UpdateGameList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"刷新实例失败：{ex}");
            }
        }
    }
}