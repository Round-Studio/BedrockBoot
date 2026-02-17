using System;
using System.Collections.Generic;
using System.IO;
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
            if (GlobalModel.Config.Data.GameFolders.Count == 0)
                return;

            var currentFolder = GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex];
            var gameFolderPath = currentFolder.GameFolderPath;

            if (!Directory.Exists(gameFolderPath))
                return;

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
            _configWatcher.Dispose();
            _configWatcher = null;
        }
    }

    private async void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (e.FullPath.Contains("bedrock_versions", StringComparison.OrdinalIgnoreCase))
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UpdateGameList();
                });
        }
        catch (Exception) { /* Ignored */ }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        CleanupConfigWatcher();
    }

    public void UpdateUI()
    {
        IsEditMode = false;

        bool hasFolders = GlobalModel.Config.Data.GameFolders.Count > 0;
        FolderList.IsVisible = hasFolders;
        FolderNull.IsVisible = !hasFolders;

        FolderList.SelectedIndex = -1;
        FolderList.Items.Clear();

        GlobalModel.Config.Data.GameFolders.ForEach(folder =>
        {
            FolderList.Items.Add(new ListBoxItem
            {
                Content = new GameFolderItem(folder),
                VerticalAlignment = VerticalAlignment.Center
            });
        });

        if (GlobalModel.Config.Data.GameFolders.Count == 1)
            FolderList.SelectedIndex = 0;
        else
            FolderList.SelectedIndex = GlobalModel.Config.Data.GameFolderSelIndex;

        InitializeConfigWatcher();
        UpdateGameList();

        IsEditMode = true;
    }

    public void UpdateGameList()
    {
        IsEditMode = false;

        if (GlobalModel.Config.Data.GameFolders.Count == 0)
        {
            ShowNoGamesUI(true);
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

        if (!Directory.Exists(versionsPath))
        {
            ShowNoGamesUI(false);
            IsEditMode = true;
            return;
        }

        GamesLoad.IsVisible = true;
        GamesNull.IsVisible = false;
        GameScro.IsVisible = false;

        var lst = new List<VersionConfig>();

        foreach (var info in GameInfoHelper.GetVersionConfigs(currentFolder.GameFolderPath))
        {
            if (string.IsNullOrEmpty(info?.Info?.VersionName) || string.IsNullOrEmpty(info?.Info?.Version))
                continue;

            if (!string.IsNullOrEmpty(SearchKey) &&
                !info.Info.VersionName.Contains(SearchKey, StringComparison.OrdinalIgnoreCase) &&
                !info.Info.Version.Contains(SearchKey, StringComparison.OrdinalIgnoreCase))
                continue;

            var type = info.Info.VersionType == MinecraftGameTypeVersion.Release ? "Release" : "Preview";
            if (!string.IsNullOrEmpty(GameType) && GameType != type)
                continue;

            lst.Add(info);
        }

        GameList.Children.Clear();

        if (lst.Count > 0)
        {
            foreach (var item in lst) GameList.Children.Add(new GameItem(item));
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

        IsEditMode = true;
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
                if (Directory.Exists(dialog.FolderPath))
                {
                    var name = string.IsNullOrEmpty(dialog.FolderName)
                        ? Path.GetFileName(Path.GetDirectoryName(dialog.FolderPath))
                        : dialog.FolderName;

                    GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo
                    {
                        GameFolderPath = dialog.FolderPath,
                        GameFolderName = name
                    });
                    GlobalModel.Config.Save();
                    UpdateUI();
                }
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
            GlobalModel.Config.Data.GameFolderSelIndex = FolderList.SelectedIndex;
            GlobalModel.Config.Save();
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
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo {
                        Title = i18n["MainWindow.Dialog.Error.Title"],
                        Message = string.Format(i18n["MainPage.Manager.Import.Error.Pack"], packPath),
                        NoticeType = NoticeType.Info
                    });
                    return;
                }

                if (string.IsNullOrEmpty(installFolder) || !Directory.Exists(installFolder))
                {
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo {
                        Title = i18n["MainWindow.Dialog.Error.Title"],
                        Message = string.Format(i18n["MainPage.Manager.Import.Error.Folder"], installFolder),
                        NoticeType = NoticeType.Info
                    });
                    return;
                }

                if (string.IsNullOrEmpty(installName))
                {
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo {
                        Title = i18n["MainWindow.Dialog.Error.Title"],
                        Message = i18n["MainPage.Manager.Import.Error.Name"],
                        NoticeType = NoticeType.Info
                    });
                    return;
                }

                TaskImportGamePackItem.Install(packPath, installFolder, installName, dialog.GameType, dialog.DontKnowGameType);
            }
        });
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        SearchKey = SearchBox.Text ?? "";
        UpdateGameList();
    }

    private void GameTypeSel_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            var tag = "";
            if (GameTypeSel?.SelectedItem is ComboBoxItem item)
            {
                tag = item.Tag?.ToString() ?? "";
            }

            GameType = tag;
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
}