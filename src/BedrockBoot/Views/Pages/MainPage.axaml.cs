/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BedrockBoot.Standard.Entity;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BedrockBoot.Standard.Core;

namespace BedrockBoot.Views.Pages;

public partial class MainPage : UserControl
{
    public static MainPage Instance;
    private bool _isUpdatingGameList;
    private List<string> _gameTags;
    private static I18nManager i18n => I18nManager.Instance;

    private DispatcherTimer? _tagTimer;
    private readonly Random _random = new();
    private string? _lastTag;

    public MainPage()
    {
        InitializeComponent();
        UpdateLaunchLayout();

        #region 注册导航项

        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = i18n["MainPage.Nav.Home"],
            Tag = "main:home",
            Page = typeof(MainHomePage)
        });
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = i18n["MainPage.Nav.Manager"],
            Tag = "main:manager",
            Page = typeof(MainManager)
        });
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = i18n["MainPage.Nav.Download"],
            Tag = "main:download",
            Page = typeof(DownloadRoot)
        });
#if WINDOWS
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = i18n["MainPage.Nav.Tools"],
            Tag = "main:toolsBox",
            Page = typeof(MainToolsBoxPage)
        });
#endif
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "\uF0B9",
            ItemText = i18n["MainPage.Nav.Multiplayer"],
            Tag = "main:multiplayer",
            Page = typeof(MainGravityConePage)
        });
        RegisterTopItem(new TopBarItemInfo
        {
            ItemGlyph = "",
            ItemText = i18n["MainPage.Nav.Setting"],
            Tag = "main:setting",
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
                PlatformCore.JumpList!.ConfigureJumpList();
            }
            catch (Exception exception)
            {
                Console.WriteLine($@"创建 JumpList 出现错误：{exception}");
            }

            StartTagTimer();
        };

        DetachedFromVisualTree += (_, _) => StopTagTimer();

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
            var result = await CheckUpdate.CheckAsync();
            if (result != null)
            {
                var panel = new StackPanel();

                var body = result.Source == UpdateSource.Release
                    ? result.Release?.Body
                    : result.WorkflowRun?.HeadCommit?.Message;

                var controls = HtmlToControlConverter.ConvertHtmlToControls(body);
                foreach (var control in controls) panel.Children.Add(control);

                DialogHost.Show(new DialogInfo
                {
                    Content = new ScrollViewer()
                    {
                        Content = panel,
                        Padding = new Thickness(10, 0),
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden
                    },
                    Title = string.Format(i18n["MainPage.Update.NewVersion"], result.DisplayName),
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
            var error = GitHubHelper.HandleException(ex);

            Console.WriteLine($@"更新失败：{ex.Message}");


            DialogHost.Show(new DialogInfo
            {
                Title = "检查更新失败",
                Content = $"无法从 GitHub 获取信息，请检查网络后重试。\n{error.GetLocalizedMessage()}",
                CloseButtonText = i18n["Shared.Action.Confirm"]
            });
        }
    }

    public void RegisterTopItem(TopBarItemInfo item)
    {
        IsEditMode = false;

        var tag = $"{item.Tag}:{Guid.NewGuid().ToString("N")}";

        TopBarItem.Add(tag, item);

        SelTag.Items.Add(new SelectBarItem
        {
            Tag = tag,
            Glyph = item.ItemGlyph
        });

        IsEditMode = true;
    }

    public void SelectTagPage(string tag)
    {
        var index = TopBarItem.ToList().FindIndex(x => x.Key.Contains(tag));
        SelTag.SelectedIndex = index;
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
                if (tag.Contains("main:home"))
                {
                    VersionChooseCard.Margin = new(8);
                    BackgroundBorder.CornerRadius = new(8);
                    BottomCard.Margin = new(8);
                    BottomBarCenter.IsVisible = true;
                    BottomBarFull.Opacity = 0;
                    GameLaunchBtn.Height = 58;
                    InstanceDesc.IsVisible = true;
                    GameLaunchBtn.Padding = new(14, 0, 8, 0);
                }
                else
                {
                    VersionChooseCard.Margin = new(0, 0, -1, -1);
                    BackgroundBorder.CornerRadius = new(8,0,0,0);
                    BottomCard.Margin = new(0);
                    BottomBarCenter.IsVisible = false;
                    BottomBarFull.Opacity = 1;
                    GameLaunchBtn.Height = 41;
                    InstanceDesc.IsVisible = false;
                    GameLaunchBtn.Padding = new(8, 0);
                }
            }
            catch { }
    }

    #endregion

    private void StartTagTimer()
    {
        StopTagTimer();
        _tagTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _tagTimer.Tick += TagTimer_Tick;
        _tagTimer.Start();
    }

    private void StopTagTimer()
    {
        if (_tagTimer != null)
        {
            _tagTimer.Tick -= TagTimer_Tick;
            _tagTimer.Stop();
            _tagTimer = null;
        }
    }

    private void TagTimer_Tick(object? sender, EventArgs e)
    {
        if (_gameTags == null || _gameTags.Count == 0) return;
        if (!InstanceDesc.IsVisible) return;

        if (_gameTags.Count == 1)
        {
            InstanceDesc.Text = _gameTags[0];
            _lastTag = _gameTags[0];
            return;
        }

        string next;
        do
        {
            next = _gameTags[_random.Next(_gameTags.Count)];
        } while (next == _lastTag);

        _lastTag = next;
        InstanceDesc.Text = next;
    }

    public void SetNullInfo(bool isNull = true)
    {
        try
        {
            if (isNull)
                GameListChoose.Items.Clear();
            _isUpdatingGameList = true;
            GameListChoose.IsVisible = !isNull;
            GameLaunchBtn.IsEnabled = !isNull;
            NullBox.IsVisible = isNull;
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

    public void UpdateLaunchLayout()
    {
        if (GlobalModel.Config.Data.IsRightLaunchButton)
        {
            GameLaunchBtn.HorizontalAlignment = HorizontalAlignment.Right;
            VersionChooseCard.HorizontalAlignment = HorizontalAlignment.Left;
            VersionChooseCard.CornerRadius = new CornerRadius(0, 12, 0, 0);
            BackgroundBackBorder.CornerRadius = new CornerRadius(0, 12, 0, 0);
            BackgroundBorder.CornerRadius = new CornerRadius(0, 8, 0, 0);
        }
        else
        {
            GameLaunchBtn.HorizontalAlignment = HorizontalAlignment.Left;
            VersionChooseCard.HorizontalAlignment = HorizontalAlignment.Right;
            VersionChooseCard.CornerRadius = new CornerRadius(12, 0, 0, 0);
            BackgroundBackBorder.CornerRadius = new CornerRadius(12, 0, 0, 0);
            BackgroundBorder.CornerRadius = new CornerRadius(8, 0, 0, 0);
        }
    }

    public async Task UpdateUIAsync()
    {
        try
        {
            SetNullInfo();
            IsEditMode = false;
            _isUpdatingGameList = true;

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

    public async Task UpdateGameInfo()
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
            InstanceIconImage.Source = await ImageLoader.Shared.LoadIconAsync(IconHelper.GetGameIconUrl(version), 26);
            InstanceName.Text = version.Info.VersionName;
            _gameTags = version.GameTags;
            SetNullInfo(false);

            if (_gameTags != null && _gameTags.Count > 0)
            {
                string next;
                if (_gameTags.Count == 1)
                {
                    next = _gameTags[0];
                }
                else
                {
                    do
                    {
                        next = _gameTags[_random.Next(_gameTags.Count)];
                    } while (next == _lastTag);
                }
                _lastTag = next;
                InstanceDesc.Text = next;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"更新游戏信息失败：{ex.Message}");
            SetNullInfo();
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

        Models.Global.GlobalModel.MainWindow.OpenDraw(new DrawInstanceContent(version),
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