using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BedrockBoot.Base.Entry;
using BedrockBoot.Core.Global;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Plugin;
using BedrockBoot.Views.Control.Items;
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

public partial class NeoMainPage : UserControl
{
    public static NeoMainPage Instance;
    private bool _isUpdatingGameList;
    private static I18nManager i18n => I18nManager.Instance;

    public NeoMainPage()
    {
        InitializeComponent();

        #region 注册导航项

        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = i18n["MainPage.Nav.Home"],
            Tag = "Home",
            Page = typeof(MainHomePage)
        });
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = i18n["MainPage.Nav.Manager"],
            Tag = "Manager",
            Page = typeof(MainManager)
        });
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = i18n["MainPage.Nav.Download"],
            Tag = "Download",
            Page = typeof(DownloadRoot)
        });
#if WINDOWS
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = i18n["MainPage.Nav.Tools"],
            Tag = "ToolsBox",
            Page = typeof(MainToolsBoxPage)
        });
#endif
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "\uF0B9",
            ItemText = i18n["MainPage.Nav.Multiplayer"],
            Tag = "Multiplayer",
            Page = typeof(MainGravityConePage)
        });
#if LINUX
        if(GlobalModel.Config.Data.IsUseNeoLaunch)
            RegisterTopItem(new TopBarItemInfo
            {
                ItemGlyph = "\uE716",
                ItemText = "账户管理",
                Tag = "AccountManager",
                Page = typeof(MainAccountPage)
            });
#endif
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = i18n["MainPage.Nav.Setting"],
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

        Loaded += async (sender, args) =>
        {
            try
            {
                PluginLoader.LoadAll();
            }
            catch
            {
            }

            try
            {
                JumpListManager.ConfigureJumpList();
            }
            catch(Exception exception)
            {
                Console.WriteLine($@"创建 JumpList 出现错误：{exception}");
            }
        };
        _ = UpdateUIAsync();

        var sel = -1;
        var count = -1;
        GlobalModel.Config.AfterSave += async (sender, args) =>
        {
            if ((GlobalModel.Config.Data.GameFolderSelIndex != sel ||
                 GlobalModel.Config.Data.GameFolders.Count != count) &&
                IsEditMode)
            {
                count = GlobalModel.Config.Data.GameFolders.Count;
                sel = GlobalModel.Config.Data.GameFolderSelIndex;

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () => await UpdateUIAsync());
            }
        };
        Models.Global.GlobalModel.MainPageUpdateInstance = () =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() => UpdateUIAsync());
        };
    }

    public bool IsEditMode { get; set; }

    #region 导航栏
    
    public Dictionary<string, TopBarItemInfo> TopBarItem { get; } = new();

    public static async Task Update(bool isShowNeo = false)
    {
        try
        {
            var result = await CheckUpdate.Update();
            if (result != null)
            {
                var panel = new StackPanel();
                var controls =
                    HtmlToControlConverter.ConvertHtmlToControls(result.Body);
                foreach (var control in controls) panel.Children.Add(control);
                DialogHost.Show(new DialogInfo
                {
                    Content = new ScrollViewer()
                    {
                        Content = panel,
                        Padding = new Thickness(10,0),
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden
                    },
                    Title = string.Format(i18n["MainPage.Update.NewVersion"], result.TagName),
                    CloseButtonText = i18n["MainPage.Update.Action.Now"],
                    PrimaryButtonText = i18n["Shared.Action.Cancel"],
                    CloseAction = () => { TaskDownloadUpdateFileItem.Update(result); }
                });
            }
            else if (isShowNeo)
            {
                DialogHost.Show(new DialogInfo
                {
                    Content = i18n["MainPage.Update.Action.Latest"],
                    Title = i18n["MainPage.Update.Title"],
                    CloseButtonText = i18n["Shared.Action.Confirm"]
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"更新失败：{ex.Message}");
        }
    }

    public void RegisterTopItem(TopBarItemInfo item)
    {
        IsEditMode = false;

        var tag = $"{item.Tag}{Guid.NewGuid().ToString("N")}";

        TopBarItem.Add(tag, item);

        SelTag.Items.Add(new SelectBarItem
        {
            Tag = tag,
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

                BedrockBootPage? page = null;

                if (TopBarItem[tag].Page is Type selPageType)
                    page = Activator.CreateInstance(selPageType) as BedrockBootPage;
                else
                    DialogHost.Show(new DialogInfo
                    {
                        Title = i18n["MainPage.Error.InvalidPage.Title"],
                        Content = string.Format(i18n["MainPage.Error.InvalidPage.Content"], tag),
                        CloseButtonText = i18n["Shared.Action.Confirm"]
                    });

                if (page == null) return;

                if (page.HeaderView != null) HeaderContent.NavigateTo(page.HeaderView);

                MainFrame.NavigateTo(page);
            }
            catch
            {
                // 移除：不要在这里重置 IsEditMode，以免影响其他事件
            }
    }

    #endregion

    public void SetNullInfo(bool isNull = true)
    {
        try
        {
            if (isNull)
                GameListChoose.Items.Clear();
            _isUpdatingGameList = true;
            GameListChoose.IsVisible = !isNull;
            GameControls.IsEnabled = !isNull;
            GameInfoItem.IsVisible = !isNull;
            GameSettingBtn.IsVisible = !isNull;
            _isUpdatingGameList = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"NullFunc执行出错: {ex.Message}");
            _isUpdatingGameList = false;
        }
    }

    public async Task UpdateUIAsync()
    {
        try
        {
            SetNullInfo();
            IsEditMode = false;
            _isUpdatingGameList = true;

            // 确保控件已初始化
            if (GameListChoose == null)
            {
                Console.WriteLine(@"GameListChoose 控件未初始化");
                return;
            }

            if (GlobalModel.Config.Data.GameFolders.Count <= 0)
            {
                SetNullInfo();
                return;
            }

            if (GlobalModel.Config.Data.GameFolderSelIndex < 0 ||
                GlobalModel.Config.Data.GameFolderSelIndex >= GlobalModel.Config.Data.GameFolders.Count)
            {
                SetNullInfo();
                return;
            }

            var versions = await GameInfoHelper.GetVersionConfigsAsync(GlobalModel.Config.Data
                .GameFolders[GlobalModel.Config.Data.GameFolderSelIndex].GameFolderPath);

            if (versions.Count <= 0)
            {
                SetNullInfo();
                return;
            }

            GameListChoose.Items.Clear();
            versions.ForEach(v =>
            {
                GameListChoose.Items.Add(new ListBoxItem()
                {
                    Content = new MainChooseGameItem(v)
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Left
                    },
                    Padding = new Thickness(0),
                });
            });

            // 修复：检查 GameSelIndex 是否有效，如果无效则设置为 0
            var gameFolder = GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex];
            if (gameFolder.GameSelIndex < 0 || gameFolder.GameSelIndex >= versions.Count) gameFolder.GameSelIndex = 0;

            GameListChoose.SelectedIndex = gameFolder.GameSelIndex;

            UpdateGameInfo();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"UpdateUI执行出错: {ex}");
            _isUpdatingGameList = false;
            IsEditMode = true;
        }
        finally
        {
            _isUpdatingGameList = false;
            IsEditMode = true;
        }
    }

    public void UpdateGameInfo()
    {
        try
        {
            if (GlobalModel.Config.Data.GameFolders.Count == 0 ||
                GlobalModel.Config.Data.GameFolderSelIndex < 0 ||
                GlobalModel.Config.Data.GameFolderSelIndex >= GlobalModel.Config.Data.GameFolders.Count)
            {
                SetNullInfo();
                return;
            }

            var gameFolder = GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex];
            var versions = GameInfoHelper.GetVersionConfigs(gameFolder.GameFolderPath);

            if (versions.Count == 0 ||
                gameFolder.GameSelIndex < 0 ||
                gameFolder.GameSelIndex >= versions.Count)
            {
                SetNullInfo();
                return;
            }

            var version = versions[gameFolder.GameSelIndex];

            GameInfoItem.Update(version);
            SetNullInfo(false);
        }
        catch (Exception ex)
        {
            // 修复：添加异常处理
            Console.WriteLine($@"更新游戏信息失败：{ex.Message}");
            SetNullInfo();
        }
    }

    private void GameListChoose_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
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

        Models.Global.GlobalModel.MainWindow.OpenDraw(new DrawInstanceContent(version),
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