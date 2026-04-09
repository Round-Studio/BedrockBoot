using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Import;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.TaskItem;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainManager : BedrockBootPage
{
    private static I18nManager i18n => I18nManager.Instance;
    private FileSystemWatcher? _configWatcher;
    private string GameType = "";
    private string SearchKey = "";
    
    // 用于 FileSystemWatcher 的防抖
    private CancellationTokenSource? _watcherDebounceCts;

    public MainManager()
    {
        Instance = this;
        InitializeComponent();

        UpdateUI();

#if RELEASE
        ImportGameBtn.IsVisible = GlobalModel.FunctionOption.IsEnableImportGamePack;
#endif
    }

    public bool IsEditMode { get; set; }
    public static MainManager Instance { get; private set; }

    private void InitializeConfigWatcher()
    {
        CleanupConfigWatcher();

        try
        {
            var folders = BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolders;
            if (folders.Count == 0) return;

            var currentFolder = folders[BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolderSelIndex];
            var gameFolderPath = currentFolder.GameFolderPath;

            if (!Directory.Exists(gameFolderPath)) return;

            _configWatcher = new FileSystemWatcher
            {
                Path = gameFolderPath,
                Filter = "config.json",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true 
            };

            _configWatcher.Changed += OnConfigFileChanged;
            _configWatcher.Deleted += OnConfigFileChanged;
            _configWatcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Config Watcher Init Failed: {ex.Message}");
        }
    }

    private void CleanupConfigWatcher()
    {
        if (_configWatcher != null)
        {
            _configWatcher.EnableRaisingEvents = false;
            _configWatcher.Changed -= OnConfigFileChanged;
            _configWatcher.Deleted -= OnConfigFileChanged;
            _configWatcher.Dispose();
            _configWatcher = null;
        }
        
        _watcherDebounceCts?.Cancel();
        _watcherDebounceCts?.Dispose();
        _watcherDebounceCts = null;
    }

    private async void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!e.FullPath.Contains("bedrock_versions", StringComparison.OrdinalIgnoreCase)) 
            return;

        // 防抖逻辑：避免单次保存触发多次事件导致重复刷新
        _watcherDebounceCts?.Cancel();
        _watcherDebounceCts = new CancellationTokenSource();
        var token = _watcherDebounceCts.Token;

        try
        {
            await Task.Delay(300, token); // 等待 300ms
            if (!token.IsCancellationRequested)
            {
                await Dispatcher.UIThread.InvokeAsync(UpdateGameList);
            }
        }
        catch (TaskCanceledException) { /* 任务被取消，正常忽略 */ }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        CleanupConfigWatcher();
    }

    public void UpdateUI()
    {
        IsEditMode = false;
        try
        {
            var folders = BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolders;
            bool hasFolders = folders.Count > 0;
            
            FolderList.IsVisible = hasFolders;
            FolderNull.IsVisible = !hasFolders;

            FolderList.SelectedIndex = -1;
            FolderList.Items.Clear();

            // 批量生成 Folder 项
            var folderItems = new List<ListBoxItem>(folders.Count);
            foreach (var folder in folders)
            {
                folderItems.Add(new ListBoxItem
                {
                    Content = new GameFolderItem(folder),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            // 一次性分配数据，减少 UI 重绘
            if (folderItems.Count > 0)
            {
                // 如果是 Avalonia 11，推荐使用 ItemsSource = folderItems，若兼容旧版可用  或循环 Add
                foreach (var item in folderItems) FolderList.Items.Add(item);
                
                FolderList.SelectedIndex = folders.Count == 1 ? 0 : BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolderSelIndex;
            }

            InitializeConfigWatcher();
            UpdateGameList();
        }
        finally
        {
            IsEditMode = true;
        }
    }

    public void UpdateGameList()
    {
        IsEditMode = false;
        try
        {
            var configData = BedrockBoot.Core.Global.GlobalModel.Config.Data;
            
            if (configData.GameFolders.Count == 0)
            {
                ShowNoGamesUI(true);
                return;
            }

            if (configData.GameFolderSelIndex < 0 || configData.GameFolderSelIndex >= configData.GameFolders.Count)
            {
                configData.GameFolderSelIndex = 0;
                BedrockBoot.Core.Global.GlobalModel.Config.Save();
            }

            var currentFolder = configData.GameFolders[configData.GameFolderSelIndex];
            var versionsPath = Path.Combine(currentFolder.GameFolderPath, "bedrock_versions");

            if (!Directory.Exists(versionsPath))
            {
                ShowNoGamesUI(false);
                return;
            }

            GamesLoad.IsVisible = true;
            GamesNull.IsVisible = false;
            GameScro.IsVisible = false;

            var gameItems = new List<Avalonia.Controls.Control>();
            bool hasSearchKey = !string.IsNullOrEmpty(SearchKey);
            bool hasGameType = !string.IsNullOrEmpty(GameType);

            foreach (var info in GameInfoHelper.GetVersionConfigs(currentFolder.GameFolderPath))
            {
                var vInfo = info?.Info;
                if (string.IsNullOrEmpty(vInfo?.VersionName) || string.IsNullOrEmpty(vInfo?.Version))
                    continue;

                if (hasSearchKey &&
                    !vInfo.VersionName.Contains(SearchKey, StringComparison.OrdinalIgnoreCase) &&
                    !vInfo.Version.Contains(SearchKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (hasGameType)
                {
                    var type = vInfo.VersionType == MinecraftGameTypeVersion.Release ? "Release" : "Preview";
                    if (GameType != type) continue;
                }

                gameItems.Add(new GameItem(info!));
            }

            GameList.Children.Clear();

            if (gameItems.Count > 0)
            {
                // 使用 AddRange 批量添加 UI 元素以优化渲染性能
                GameList.Children.AddRange(gameItems);
                GamesLoad.IsVisible = false;
                GameScro.IsVisible = true;
                GamesNull.IsVisible = false;
            }
            else
            {
                GamesLoad.IsVisible = false;
                GameScro.IsVisible = false;
                GamesNull.IsVisible = true;
            }
        }
        finally
        {
            IsEditMode = true;
        }
    }

    private void ShowNoGamesUI(bool isNullBecauseNoFolder)
    {
        GamesLoad.IsVisible = false;
        GameScro.IsVisible = false;
        GamesNull.IsVisible = true;
        
        // 此处可通过绑定或 FindControl 获取 TextBlock 并设置多语言
        // GamesNullText.Text = isNullBecauseNoFolder ? i18n["MainPage.Status.NoFolder"] : i18n["MainPage.Status.NoInstance"];
    }

    private void AddFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogAddGameFolderContent();

        DialogHost.Show(new DialogInfo
        {
            Title = i18n["Setting.Game.Folders.Dialog.Add.Title"],
            Content = dialog,
            CloseButtonText = i18n["Setting.Game.Folders.Dialog.Add.Action"],
            SecondaryButtonText = i18n["MainWindow.Common.Cancel"],
            PrimaryButtonText = i18n["Setting.Game.Folders.Dialog.Add.ImportOther"],
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                if (!Directory.Exists(dialog.FolderPath)) return;

                var name = string.IsNullOrEmpty(dialog.FolderName)
                    ? Path.GetFileName(Path.GetDirectoryName(dialog.FolderPath))
                    : dialog.FolderName;

                BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo
                {
                    GameFolderPath = dialog.FolderPath,
                    GameFolderName = name ?? "Unknown Folder"
                });
                BedrockBoot.Core.Global.GlobalModel.Config.Save();
                UpdateUI();
            },
            PrimaryAction = () =>
            {
                GlobalModel.MainWindow.OpenDraw(new DrawImportOtherLauncherContent(), i18n["Setting.Game.Folders.Draw.Import.Title"]);
            }
        });
    }

    private void FolderList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolderSelIndex = FolderList.SelectedIndex;
            BedrockBoot.Core.Global.GlobalModel.Config.Save();
            InitializeConfigWatcher();
            UpdateGameList();
            JumpListManager.ConfigureJumpList();
        }
    }

    private void ImportGameBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogImportGameContent();

        DialogHost.Show(new DialogInfo
        {
            Title = i18n["MainPage.Manager.Import.Title"],
            Content = dialog,
            CloseButtonText = i18n["MainPage.Manager.Import.Action"],
            SecondaryButtonText = i18n["MainWindow.Common.Cancel"],
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                var packPath = dialog.PackFile;
                var installFolder = dialog.PackInstallFolder;
                var installName = dialog.PackInstallName;

                if (string.IsNullOrEmpty(packPath) || !File.Exists(packPath))
                {
                    ShowErrorNotice(string.Format(i18n["MainPage.Manager.Import.Error.Pack"], packPath));
                    return;
                }

                if (string.IsNullOrEmpty(installFolder) || !Directory.Exists(installFolder))
                {
                    ShowErrorNotice(string.Format(i18n["MainPage.Manager.Import.Error.Folder"], installFolder));
                    return;
                }

                if (string.IsNullOrEmpty(installName))
                {
                    ShowErrorNotice(i18n["MainPage.Manager.Import.Error.Name"]);
                    return;
                }

                TaskImportGamePackItem.Install(packPath, installFolder, installName, dialog.GameType, dialog.DontKnowGameType);
            }
        });
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        SearchKey = SearchBox.Text?.Trim() ?? "";
        UpdateGameList();
    }

    private void GameTypeSel_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            GameType = GameTypeSel?.SelectedItem is ComboBoxItem { Tag: { } tag } 
                       ? tag.ToString() ?? "" 
                       : "";
            UpdateGameList();
        }
    }

    private async void ImportIntegrationPackBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = i18n["MainPage.Manager.Integration.Picker.Title"],
            AllowMultiple = false,
            FileTypeFilter = new []
            {
                new FilePickerFileType(i18n["MainPage.Manager.Integration.Picker.Type"])
                {
                    Patterns = new[] { "*.mcpint" }
                }
            }
        });

        if (files is { Count: >= 1 })
        {
            var filePath = files[0].Path.LocalPath;
            if (File.Exists(filePath))
            {
                var body = new DialogAddGameInstanceConfigContent(filePath);
                DialogHost.Show(new DialogInfo
                {
                    Title = i18n["MainPage.Manager.Integration.Dialog.Title"],
                    Content = body,
                    CloseButtonText = i18n["MainPage.Manager.Integration.Dialog.Action"],
                    SecondaryButtonText = i18n["MainWindow.Common.Cancel"],
                    CloseAction = () =>
                    {
                        if(string.IsNullOrEmpty(body.GameInstallFolder) || string.IsNullOrEmpty(body.GameInstallName))
                            return;

                        TaskImportIntegrationPackItem.Install(filePath, body.GameInstallFolder, body.GameInstallName);
                    }
                });
            }
        }
    }

    // 提取公共弹窗逻辑减少代码冗余
    private void ShowErrorNotice(string message)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo 
        {
            Title = i18n["MainWindow.Dialog.Error.Title"],
            Message = message,
            NoticeType = NoticeType.Info
        });
    }
}