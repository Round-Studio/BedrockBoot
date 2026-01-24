using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Plugin;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation.CornerSelectBar;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation.SelectBar;
using Round.SDK.Entry.BedrockBoot;
using Round.SDK.Plugin.BedrockBoot.Register;

namespace BedrockBoot.Views.Pages;

public partial class MainPage : UserControl
{
    public static MainPage Instance;
    public bool IsEditMode { get; set; } = false;

    public MainPage()
    {
        InitializeComponent();

        #region 注册导航项
        
        RegisterTopItem(new TopBarItemInfo()
        {
            ItemGlyph = "",
            ItemText = "主页",
            Tag = "Home",
            Page = typeof(MainHomePage)
        });
        RegisterTopItem(new TopBarItemInfo()
        {
            ItemGlyph = "",
            ItemText = "实例",
            Tag = "Manager",
            Page = typeof(MainManager)
        });
        RegisterTopItem(new TopBarItemInfo()
        {
            ItemGlyph = "",
            ItemText = "下载",
            Tag = "Download",
            Page = typeof(MainDownloadPage)
        });
        RegisterTopItem(new TopBarItemInfo()
        {
            ItemGlyph = "",
            ItemText = "任务",
            Tag = "Task",
            Page = typeof(MainTaskPage)
        });
#if DEBUG
        RegisterTopItem(new TopBarItemInfo()
        {
            ItemGlyph = "",
            ItemText = "工具",
            Tag = "ToolsBox",
            Page = typeof(MainToolsBoxPage)
        });
#endif
#if RELEASE
        if (GlobalModel.FunctionOption.IsEnableToolsBox)
            RegisterTopItem(new TopBarItemInfo()
            {
                ItemGlyph = "",
                ItemText = "工具",
                Tag = "ToolsBox",
                Page = typeof(MainToolsBoxPage)
            });
#endif
        RegisterTopItem(new TopBarItemInfo()
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
        
        RegisterService.API.RegisterTopBarItem = RegisterTopItem;

        if (GlobalModel.Config.Data.IsAutoCheckUpdate) Update();

        this.Loaded += (sender, args) =>
        {
            PluginLoader.LoadAll();
        
            JumpListManager.ConfigureJumpList();
        };

        var sel = -1;
        GlobalModel.Config.AfterSave += (sender, args) =>
        {
            if (args.Config.Data.GameFolderSelIndex != sel)
            {
                sel = args.Config.Data.GameFolderSelIndex;
                UpdateUI();
            }
        };
        UpdateUI();
    }

    public static async Task Update(bool isShowNeo = false)
    {
        try
        {
            var result = await CheckUpdate.Update();
            if (result != null)
                DialogHost.Show(new DialogInfo()
                {
                    Content = $"我们有新的更新：\n\n{result.Body}",
                    Title = $"更新 {result.TagName}",
                    CloseButtonText = "现在更新",
                    PrimaryButtonText = "取消",
                    CloseAction = () => { TaskDownloadUpdateFileItem.Update(result); }
                });
            else if (isShowNeo)
                DialogHost.Show(new DialogInfo()
                {
                    Content = $"当前已是最新版本",
                    Title = $"检查更新",
                    CloseButtonText = "确定"
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"更新失败：{ex.Message}");
        }
    }
    public Dictionary<string, TopBarItemInfo> TopBarItem { get; private set; } = new();
    public void RegisterTopItem(TopBarItemInfo item)
    {
        IsEditMode = false;

        TopBarItem.Add(item.Tag, item);

        SelTag.Items.Add(new SelectBarItem()
        {
            Tag = item.Tag,
            Glyph = item.ItemGlyph
        });

        IsEditMode = true;
    }
    private void SelTag_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            try
            {
                var item = (SelectBarItem)SelTag.SelectedItem;
                var tag = item.Tag as string;

                BedrockBootPage page = null;

                if (TopBarItem[tag].Page is Type selPageType)
                {
                    page = (BedrockBootPage)Activator.CreateInstance(selPageType);
                }
                else
                {
                    DialogHost.Show(new DialogInfo()
                    {
                        Title = "页面无效",
                        Content = $"页面 {tag} 无效",
                        CloseButtonText = "确定"
                    });
                }

                if (page.HeaderView != null)
                {
                    HeaderContent.NavigateTo(page.HeaderView);
                }

                MainFrame.NavigateTo(page);
            }
            catch
            {
            }
        }
    }
    public void UpdateUI()
    {
        void NullFunc()
        {
            GameListChoose.Items.Clear();
            GameListChoose.Items.Add("无可用实例");
            GameListChoose.SelectedIndex = 0;
            GameControls.IsEnabled = false;
            GameInfo.Text = "";
            GameName.Text = "";
            GameSettingBtn.IsVisible = false;

            IsEditMode = true;
        }
        
        IsEditMode = false;
        
        GameListChoose.Items.Clear();
        GameControls.IsEnabled = true;
        GameSettingBtn.IsVisible = true;

        if (GlobalModel.Config.Data.GameFolders.Count <= 0)
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
        
        versions.ForEach(v =>
        {
            GameListChoose.Items.Add($"{v.Info.VersionName} [{v.Info.Version}]");
        });

        GameListChoose.SelectedIndex = GlobalModel.Config.Data
            .GameFolders[GlobalModel.Config.Data.GameFolderSelIndex].GameSelIndex;

        UpdateGameInfo();
        
        IsEditMode = true;
    }

    public void UpdateGameInfo()
    {
        var version = GameInfoHelper.GetVersionConfigs(GlobalModel.Config.Data
            .GameFolders[GlobalModel.Config.Data.GameFolderSelIndex].GameFolderPath)[GlobalModel.Config.Data
            .GameFolders[GlobalModel.Config.Data.GameFolderSelIndex].GameSelIndex];

        GameInfo.Text = $"{version.Info.VersionType} {version.Info.Version}";
        GameName.Text = version.Info.VersionName;
        GameBuildType.Text = version.Info.BuildType.ToString();
    }

    private void GameListChoose_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            var selIndex = GameListChoose.SelectedIndex;
            GlobalModel.Config.Data
                .GameFolders[GlobalModel.Config.Data.GameFolderSelIndex].GameSelIndex = selIndex;
            GlobalModel.Config.Save();
            
            UpdateGameInfo();
        }
    }

    private void GameSettingBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var version = GameInfoHelper.GetVersionConfigs(GlobalModel.Config.Data
            .GameFolders[GlobalModel.Config.Data.GameFolderSelIndex].GameFolderPath)[GlobalModel.Config.Data
            .GameFolders[GlobalModel.Config.Data.GameFolderSelIndex].GameSelIndex];
        
        GlobalModel.MainWindow.OpenDraw(new DrawInstanceContent(version),$"{version.Info.VersionName} - {version.Info.Version}");
    }

    private void GameLaunchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var version = GameInfoHelper.GetVersionConfigs(GlobalModel.Config.Data
            .GameFolders[GlobalModel.Config.Data.GameFolderSelIndex].GameFolderPath)[GlobalModel.Config.Data
            .GameFolders[GlobalModel.Config.Data.GameFolderSelIndex].GameSelIndex];
        
        TaskLaunchGameItem.Launch(version);
    }
}