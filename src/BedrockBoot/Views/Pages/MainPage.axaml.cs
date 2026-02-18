using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Plugin;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages.DownloadPage;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation.SelectBar;
using Round.SDK.Entry.BedrockBoot;
using Round.SDK.Plugin.BedrockBoot.Register;

namespace BedrockBoot.Views.Pages;

public partial class MainPage : UserControl
{
    public static MainPage Instance;
    private bool _isUpdatingGameList;
    private List<VersionConfig> _cachedVersions = new(); // 缓存当前文件夹下的版本列表
    private FileSystemWatcher _folderWatcher; // 监听磁盘变动

    public MainPage()
    {
        InitializeComponent();
        Instance = this;

        InitNavigation();

        IsEditMode = true;
        SelTag.SelectedIndex = 0;
        SelTag_OnSelectionChanged(null, null);

        RegisterService.API.RegisterNavigationBarItem = RegisterTopItem;

        if (GlobalModel.Config.Data.IsAutoCheckUpdate) _ = Update();

        Loaded += (sender, args) =>
        {
            PluginLoader.LoadAll();
            JumpListManager.ConfigureJumpList();
            _ = UpdateUIAsync(); // 初始加载
        };

        // 监听配置更改（如切换游戏目录）
        var lastFolderIndex = -1;
        var lastFolderCount = -1;
        GlobalModel.Config.AfterSave += async (sender, args) =>
        {
            if (IsEditMode && (GlobalModel.Config.Data.GameFolderSelIndex != lastFolderIndex ||
                               GlobalModel.Config.Data.GameFolders.Count != lastFolderCount))
            {
                lastFolderCount = GlobalModel.Config.Data.GameFolders.Count;
                lastFolderIndex = GlobalModel.Config.Data.GameFolderSelIndex;
                await UpdateUIAsync();
            }
        };
    }

    public bool IsEditMode { get; set; }
    public Dictionary<string, TopBarItemInfo> TopBarItem { get; } = new();

    #region 初始化与导航

    private void InitNavigation()
    {
        var navItems = new List<TopBarItemInfo>
        {
            new() { ItemGlyph = "", ItemText = I18nManager.Instance["MainPage.Nav.Home"], Tag = "Home", Page = typeof(MainHomePage) },
            new() { ItemGlyph = "", ItemText = I18nManager.Instance["MainPage.Nav.Manager"], Tag = "Manager", Page = typeof(MainManager) },
            new() { ItemGlyph = "", ItemText = I18nManager.Instance["MainPage.Nav.Download"], Tag = "Download", Page = typeof(DownloadRoot) }
        };

        // 工具箱逻辑
        bool showTools = false;
#if DEBUG
        showTools = true;
#else
        showTools = GlobalModel.FunctionOption.IsEnableToolsBox;
#endif
        if (showTools)
            navItems.Add(new() { ItemGlyph = "", ItemText = I18nManager.Instance["MainPage.Nav.Tools"], Tag = "ToolsBox", Page = typeof(MainToolsBoxPage) });

        navItems.Add(new() { ItemGlyph = "", ItemText = I18nManager.Instance["MainPage.Nav.Setting"], Tag = "Setting", Page = typeof(MainSettingPage) });

        foreach (var item in navItems) RegisterTopItem(item);
    }

    public void RegisterTopItem(TopBarItemInfo item)
    {
        IsEditMode = false;
        TopBarItem[item.Tag] = item;
        SelTag.Items.Add(new SelectBarItem { Tag = item.Tag, Glyph = item.ItemGlyph });
        IsEditMode = true;
    }

    private void SelTag_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsEditMode || SelTag.SelectedItem is not SelectBarItem item) return;

        var tag = item.Tag as string;
        if (string.IsNullOrEmpty(tag) || !TopBarItem.TryGetValue(tag, out var info)) return;

        try
        {
            if (Activator.CreateInstance(info.Page) is BedrockBootPage page)
            {
                if (page.HeaderView != null) HeaderContent.NavigateTo(page.HeaderView);
                MainFrame.NavigateTo(page);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"页面导航失败: {ex.Message}");
        }
    }

    #endregion

    #region 核心业务逻辑：实时版本加载

    /// <summary>
    /// 异步更新 UI 列表，防止磁盘扫描卡顿
    /// </summary>
    public async Task UpdateUIAsync()
    {
        if (_isUpdatingGameList) return;

        try
        {
            IsEditMode = false;
            _isUpdatingGameList = true;

            if (GlobalModel.Config.Data.GameFolders.Count == 0 || GlobalModel.Config.Data.GameFolderSelIndex < 0)
            {
                SetNullStatus();
                return;
            }

            var folderConfig = GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex];
            string path = folderConfig.GameFolderPath;

            // 设置文件夹监听器，实现实时感应磁盘变化
            SetupWatcher(path);

            // 异步扫描磁盘
            _cachedVersions = await Task.Run(() => GameInfoHelper.GetVersionConfigs(path));

            GameListChoose.Items.Clear();

            if (_cachedVersions.Count == 0)
            {
                SetNullStatus();
                return;
            }

            // 填充下拉框
            foreach (var v in _cachedVersions)
                GameListChoose.Items.Add(v.Info.VersionName);

            // 状态恢复
            GameControls.IsEnabled = true;
            GameSettingBtn.IsVisible = true;

            if (folderConfig.GameSelIndex < 0 || folderConfig.GameSelIndex >= _cachedVersions.Count)
                folderConfig.GameSelIndex = 0;

            GameListChoose.SelectedIndex = folderConfig.GameSelIndex;
            UpdateGameDisplay();
        }
        finally
        {
            _isUpdatingGameList = false;
            IsEditMode = true;
        }
    }

    private void SetupWatcher(string path)
    {
        _folderWatcher?.Dispose();
        if (!Directory.Exists(path)) return;

        _folderWatcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName
        };

        // 磁盘发生变化时（增删文件夹），回到 UI 线程刷新
        Action refresh = () => Dispatcher.UIThread.InvokeAsync(() => UpdateUIAsync());
        _folderWatcher.Created += (s, e) => refresh();
        _folderWatcher.Deleted += (s, e) => refresh();
        _folderWatcher.Renamed += (s, e) => refresh();
    }

    private void SetNullStatus()
    {
        GameListChoose.Items.Clear();
        GameListChoose.Items.Add(I18nManager.Instance["MainPage.Status.NoInstance"]);
        GameListChoose.SelectedIndex = 0;
        GameControls.IsEnabled = false;
        GameSettingBtn.IsVisible = false;
        GameInfo.Text = "";
        GameName.Text = "";
        GameBuildType.Text = "";
        _cachedVersions.Clear();
    }

    private void UpdateGameDisplay()
    {
        if (_cachedVersions.Count == 0 || GameListChoose.SelectedIndex < 0) return;

        var version = _cachedVersions[GameListChoose.SelectedIndex];
        GameInfo.Text = $"{version.Info.VersionType} {version.Info.Version}";
        GameName.Text = version.Info.VersionName;
        GameBuildType.Text = version.Info.BuildType.ToString();
    }

    #endregion

    #region 事件交互

    public static async Task Update(bool isShowNeo = false)
    {
        try
        {
            var result = await CheckUpdate.Update();
            if (result != null)
            {
                DialogHost.Show(new DialogInfo
                {
                    Title = string.Format(I18nManager.Instance["MainPage.Update.NewVersion"], result.TagName),
                    Content = string.Format(I18nManager.Instance["MainPage.Update.Content"], result.Body),
                    CloseButtonText = I18nManager.Instance["MainPage.Update.Action.Now"],
                    PrimaryButtonText = I18nManager.Instance["MainWindow.Common.Cancel"],
                    CloseAction = () => { TaskDownloadUpdateFileItem.Update(result); }
                });
            }
            else if (isShowNeo)
            {
                DialogHost.Show(new DialogInfo { Title = I18nManager.Instance["MainPage.Update.Title"], Content = I18nManager.Instance["MainPage.Update.Action.Latest"], CloseButtonText = I18nManager.Instance["MainWindow.Common.Confirm"] });
            }
        }
        catch (Exception ex) { Console.WriteLine($"更新检查失败: {ex.Message}"); }
    }

    private void GameListChoose_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsEditMode || _isUpdatingGameList) return;

        int selIndex = GameListChoose.SelectedIndex;
        if (selIndex >= 0 && GlobalModel.Config.Data.GameFolderSelIndex < GlobalModel.Config.Data.GameFolders.Count)
        {
            GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex].GameSelIndex = selIndex;
            GlobalModel.Config.Save();
            UpdateGameDisplay();
        }
    }

    private void GameSettingBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_cachedVersions.Count == 0 || GameListChoose.SelectedIndex < 0) return;
        var version = _cachedVersions[GameListChoose.SelectedIndex];
        GlobalModel.MainWindow.OpenDraw(new DrawInstanceContent(version), $"{version.Info.VersionName} - {version.Info.Version}");
    }

    private void GameLaunchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_cachedVersions.Count == 0 || GameListChoose.SelectedIndex < 0) return;
        TaskLaunchGameItem.Launch(_cachedVersions[GameListChoose.SelectedIndex]);
    }

    #endregion
}