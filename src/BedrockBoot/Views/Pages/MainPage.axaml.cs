using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry;
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
    private bool _isUpdatingGameList; // 添加：控制游戏列表更新的标志

    public MainPage()
    {
        InitializeComponent();

        #region 注册导航项

        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = "主页",
            Tag = "Home",
            Page = typeof(MainHomePage)
        });
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = "实例",
            Tag = "Manager",
            Page = typeof(MainManager)
        });
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = "下载",
            Tag = "Download",
            Page = typeof(DownloadRoot)
        });
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = "工具",
            Tag = "ToolsBox",
            Page = typeof(MainToolsBoxPage)
        });
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "\uF0B9",
            ItemText = "多人联机",
            Tag = "Multiplayer",
            Page = typeof(MainMultiplayerPage)
        });
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = "设置",
            Tag = "Setting",
            Page = typeof(MainSettingPage)
        });

        #endregion

        Instance = this;

        IsEditMode = true;
        SelTag.SelectedIndex = 0;
        SelTag_OnSelectionChanged(null, null);

        RegisterService.API.RegisterNavigationBarItem = RegisterTopItem;

        if (GlobalModel.Config.Data.IsAutoCheckUpdate) Update();

        Loaded += (sender, args) =>
        {
            PluginLoader.LoadAll();

            JumpListManager.ConfigureJumpList();
        };

        var sel = -1;
        var count = -1;
        GlobalModel.Config.AfterSave += (sender, args) =>
        {
            if ((GlobalModel.Config.Data.GameFolderSelIndex != sel ||
                 GlobalModel.Config.Data.GameFolders.Count != count) &&
                IsEditMode)
            {
                count = GlobalModel.Config.Data.GameFolders.Count;
                sel = GlobalModel.Config.Data.GameFolderSelIndex;
                
                Avalonia.Threading.Dispatcher.UIThread.Invoke(UpdateUI);
            }
        };
        UpdateUI();
    }

    public bool IsEditMode { get; set; }

    public Dictionary<string, TopBarItemInfo> TopBarItem { get; } = new();

    public static async Task Update(bool isShowNeo = false)
    {
        try
        {
            var result = await CheckUpdate.Update();
            if (result != null)
                DialogHost.Show(new DialogInfo
                {
                    Content = $"我们有新的更新：\n\n{result.Body}",
                    Title = $"更新 {result.TagName}",
                    CloseButtonText = "现在更新",
                    PrimaryButtonText = "取消",
                    CloseAction = () => { TaskDownloadUpdateFileItem.Update(result); }
                });
            else if (isShowNeo)
                DialogHost.Show(new DialogInfo
                {
                    Content = "当前已是最新版本",
                    Title = "检查更新",
                    CloseButtonText = "确定"
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"更新失败：{ex.Message}");
        }
    }

    public void RegisterTopItem(TopBarItemInfo item)
    {
        IsEditMode = false;

        TopBarItem.Add(item.Tag, item);

        SelTag.Items.Add(new SelectBarItem
        {
            Tag = item.Tag,
            Glyph = item.ItemGlyph
        });

        IsEditMode = true;
    }

    private void SelTag_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
            try
            {
                // 修复：检查 SelectedItem 是否为 null
                if (SelTag.SelectedItem == null) return;

                var item = (SelectBarItem)SelTag.SelectedItem;
                var tag = item.Tag as string;

                // 修复：检查 tag 是否存在于字典中
                if (string.IsNullOrEmpty(tag) || !TopBarItem.ContainsKey(tag)) return;

                BedrockBootPage page = null;

                if (TopBarItem[tag].Page is Type selPageType)
                    page = (BedrockBootPage)Activator.CreateInstance(selPageType);
                else
                    DialogHost.Show(new DialogInfo
                    {
                        Title = "页面无效",
                        Content = $"页面 {tag} 无效",
                        CloseButtonText = "确定"
                    });

                if (page.HeaderView != null) HeaderContent.NavigateTo(page.HeaderView);

                MainFrame.NavigateTo(page);
            }
            catch
            {
                // 移除：不要在这里重置 IsEditMode，以免影响其他事件
            }
    }

    public void UpdateUI()
    {
        void NullFunc()
        {
            try
            {
                _isUpdatingGameList = true; // 添加：设置更新标志
                GameListChoose.Items.Clear();
                GameListChoose.Items.Add("无可用实例");
                GameListChoose.SelectedIndex = 0;
                GameControls.IsEnabled = false;
                GameInfo.Text = "";
                GameName.Text = "";
                GameSettingBtn.IsVisible = false;
                _isUpdatingGameList = false; // 添加：清除更新标志
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"NullFunc执行出错: {ex.Message}");
                _isUpdatingGameList = false;
            }
        }

        try
        {
            // 在开始更新前先设置标志
            IsEditMode = false;
            _isUpdatingGameList = true;

            GameControls.IsEnabled = true;
            GameSettingBtn.IsVisible = true;

            // 确保控件已初始化
            if (GameListChoose == null)
            {
                Console.WriteLine(@"GameListChoose 控件未初始化");
                return;
            }

            // 清空现有项目
            try
            {
                GameListChoose.Items.Clear();
            }
            catch
            {
            }

            if (GlobalModel.Config.Data.GameFolders.Count <= 0)
            {
                NullFunc();
                return;
            }

            // 修复：检查 GameFolderSelIndex 是否有效
            if (GlobalModel.Config.Data.GameFolderSelIndex < 0 ||
                GlobalModel.Config.Data.GameFolderSelIndex >= GlobalModel.Config.Data.GameFolders.Count)
            {
                NullFunc();
                return;
            }

            var versions = GameInfoHelper.GetVersionConfigs(GlobalModel.Config.Data
                .GameFolders[GlobalModel.Config.Data.GameFolderSelIndex].GameFolderPath);

            if (versions.Count <= 0)
            {
                NullFunc();
                return;
            }

            // 添加版本到选择框
            versions.ForEach(v => { GameListChoose.Items.Add($"{v.Info.VersionName}"); });

            // 修复：检查 GameSelIndex 是否有效，如果无效则设置为 0
            var gameFolder = GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex];
            if (gameFolder.GameSelIndex < 0 || gameFolder.GameSelIndex >= versions.Count) gameFolder.GameSelIndex = 0;

            GameListChoose.SelectedIndex = gameFolder.GameSelIndex;

            UpdateGameInfo();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"UpdateUI执行出错: {ex}");
            // 发生异常时也要确保标志位被重置
            _isUpdatingGameList = false;
            IsEditMode = true;
        }
        finally
        {
            // 确保在finally块中重置标志，即使发生异常也能恢复
            _isUpdatingGameList = false;
            IsEditMode = true;
        }
    }

    public void UpdateGameInfo()
    {
        try
        {
            // 修复：添加边界检查防止数组越界
            if (GlobalModel.Config.Data.GameFolders.Count == 0 ||
                GlobalModel.Config.Data.GameFolderSelIndex < 0 ||
                GlobalModel.Config.Data.GameFolderSelIndex >= GlobalModel.Config.Data.GameFolders.Count)
            {
                GameInfo.Text = "无可用实例";
                GameName.Text = "";
                GameBuildType.Text = "";
                return;
            }

            var gameFolder = GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex];
            var versions = GameInfoHelper.GetVersionConfigs(gameFolder.GameFolderPath);

            if (versions.Count == 0 ||
                gameFolder.GameSelIndex < 0 ||
                gameFolder.GameSelIndex >= versions.Count)
            {
                GameInfo.Text = "无可用实例";
                GameName.Text = "";
                GameBuildType.Text = "";
                return;
            }

            var version = versions[gameFolder.GameSelIndex];

            GameInfo.Text = $"{version.Info.VersionType} {version.Info.Version}";
            GameName.Text = version.Info.VersionName;
            GameBuildType.Text = version.Info.BuildType.ToString();
        }
        catch (Exception ex)
        {
            // 修复：添加异常处理
            Console.WriteLine($@"更新游戏信息失败：{ex.Message}");
            GameInfo.Text = "加载失败";
            GameName.Text = "";
            GameBuildType.Text = "";
        }
    }

    private void GameListChoose_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // 修改：添加 _isUpdatingGameList 检查，防止在更新列表时触发
        if (IsEditMode && !_isUpdatingGameList)
        {
            var selIndex = GameListChoose.SelectedIndex;

            // 修复：检查索引是否有效
            if (selIndex >= 0 && GlobalModel.Config.Data.GameFolderSelIndex >= 0 &&
                GlobalModel.Config.Data.GameFolderSelIndex < GlobalModel.Config.Data.GameFolders.Count)
            {
                GlobalModel.Config.Data
                    .GameFolders[GlobalModel.Config.Data.GameFolderSelIndex].GameSelIndex = selIndex;
                GlobalModel.Config.Save();

                UpdateGameInfo();
            }
        }
    }

    private void GameSettingBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // 修复：添加边界检查
        if (GlobalModel.Config.Data.GameFolders.Count == 0 ||
            GlobalModel.Config.Data.GameFolderSelIndex < 0 ||
            GlobalModel.Config.Data.GameFolderSelIndex >= GlobalModel.Config.Data.GameFolders.Count)
            return;

        var gameFolder = GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex];
        var versions = GameInfoHelper.GetVersionConfigs(gameFolder.GameFolderPath);

        if (versions.Count == 0 || gameFolder.GameSelIndex < 0 || gameFolder.GameSelIndex >= versions.Count) return;

        var version = versions[gameFolder.GameSelIndex];

        GlobalModel.MainWindow.OpenDraw(new DrawInstanceContent(version),
            $"{version.Info.VersionName} - {version.Info.Version}");
    }

    private void GameLaunchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // 修复：添加边界检查
        if (GlobalModel.Config.Data.GameFolders.Count == 0 ||
            GlobalModel.Config.Data.GameFolderSelIndex < 0 ||
            GlobalModel.Config.Data.GameFolderSelIndex >= GlobalModel.Config.Data.GameFolders.Count)
            return;

        var gameFolder = GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex];
        var versions = GameInfoHelper.GetVersionConfigs(gameFolder.GameFolderPath);

        if (versions.Count == 0 || gameFolder.GameSelIndex < 0 || gameFolder.GameSelIndex >= versions.Count) return;

        var version = versions[gameFolder.GameSelIndex];

        TaskLaunchGameItem.Launch(version);
    }
}