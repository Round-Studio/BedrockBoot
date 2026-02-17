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
    private bool _isUpdatingGameList; 

    public MainPage()
    {
        InitializeComponent();

        #region 注册导航项 (使用国际化 Key)

        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = I18nManager.Instance["MainPage.Nav.Home"],
            Tag = "Home",
            Page = typeof(MainHomePage)
        });
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = I18nManager.Instance["MainPage.Nav.Manager"],
            Tag = "Manager",
            Page = typeof(MainManager)
        });
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = I18nManager.Instance["MainPage.Nav.Download"],
            Tag = "Download",
            Page = typeof(DownloadRoot)
        });

#if DEBUG
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = I18nManager.Instance["MainPage.Nav.Tools"],
            Tag = "ToolsBox",
            Page = typeof(MainToolsBoxPage)
        });
#endif
#if RELEASE
        if (GlobalModel.FunctionOption.IsEnableToolsBox)
            RegisterTopItem(new TopBarItemInfo()
            {
                ItemGlyph = "",
                ItemText = I18nManager.Instance["MainPage.Nav.Tools"],
                Tag = "ToolsBox",
                Page = typeof(MainToolsBoxPage)
            });
#endif
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = I18nManager.Instance["MainPage.Nav.Setting"],
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
                UpdateUI();
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
                    Title = string.Format(I18nManager.Instance["MainPage.Update.NewVersion"], result.TagName),
                    Content = string.Format(I18nManager.Instance["MainPage.Update.Content"], result.Body),
                    CloseButtonText = I18nManager.Instance["MainPage.Update.Action.Now"],
                    PrimaryButtonText = I18nManager.Instance["MainWindow.Common.Cancel"],
                    CloseAction = () => { TaskDownloadUpdateFileItem.Update(result); }
                });
            else if (isShowNeo)
                DialogHost.Show(new DialogInfo
                {
                    Title = I18nManager.Instance["MainPage.Update.Title"],
                    Content = I18nManager.Instance["MainPage.Update.Action.Latest"],
                    CloseButtonText = I18nManager.Instance["MainWindow.Common.Confirm"]
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
                if (SelTag.SelectedItem == null) return;

                var item = (SelectBarItem)SelTag.SelectedItem;
                var tag = item.Tag as string;

                if (string.IsNullOrEmpty(tag) || !TopBarItem.ContainsKey(tag)) return;

                BedrockBootPage page = null;

                if (TopBarItem[tag].Page is Type selPageType)
                    page = (BedrockBootPage)Activator.CreateInstance(selPageType);
                else
                    DialogHost.Show(new DialogInfo
                    {
                        Title = I18nManager.Instance["MainPage.Error.InvalidPage.Title"],
                        Content = string.Format(I18nManager.Instance["MainPage.Error.InvalidPage.Content"], tag),
                        CloseButtonText = I18nManager.Instance["MainWindow.Common.Confirm"]
                    });

                if (page.HeaderView != null) HeaderContent.NavigateTo(page.HeaderView);
                MainFrame.NavigateTo(page);
            }
            catch { }
    }

    public void UpdateUI()
    {
        void NullFunc()
        {
            try
            {
                _isUpdatingGameList = true; 
                GameListChoose.Items.Clear();
                GameListChoose.Items.Add(I18nManager.Instance["MainPage.Status.NoInstance"]); // 国际化显示
                GameListChoose.SelectedIndex = 0;
                GameControls.IsEnabled = false;
                GameInfo.Text = "";
                GameName.Text = "";
                GameSettingBtn.IsVisible = false;
                _isUpdatingGameList = false; 
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"NullFunc执行出错: {ex.Message}");
                _isUpdatingGameList = false;
            }
        }

        try
        {
            IsEditMode = false;
            _isUpdatingGameList = true;

            if (GameListChoose == null) return;

            try { GameListChoose.Items.Clear(); } catch { }

            GameControls.IsEnabled = true;
            GameSettingBtn.IsVisible = true;

            if (GlobalModel.Config.Data.GameFolders.Count <= 0)
            {
                NullFunc();
                return;
            }

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

            versions.ForEach(v => { GameListChoose.Items.Add($"{v.Info.VersionName}"); });

            var gameFolder = GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex];
            if (gameFolder.GameSelIndex < 0 || gameFolder.GameSelIndex >= versions.Count) gameFolder.GameSelIndex = 0;

            GameListChoose.SelectedIndex = gameFolder.GameSelIndex;
            UpdateGameInfo();
        }
        catch (Exception ex)
        {
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
                GameInfo.Text = I18nManager.Instance["MainPage.Status.NoInstance"];
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
                GameInfo.Text = I18nManager.Instance["MainPage.Status.NoInstance"];
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
            GameInfo.Text = I18nManager.Instance["MainPage.Status.LoadFailed"];
            GameName.Text = "";
            GameBuildType.Text = "";
        }
    }

    private void GameListChoose_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode && !_isUpdatingGameList)
        {
            var selIndex = GameListChoose.SelectedIndex;

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