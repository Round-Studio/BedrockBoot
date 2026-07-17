using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Core.Global;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Interface;
using BedrockBoot.Models;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.TaskItem;

namespace BedrockBoot.Views.Control.Widgets.DesktopWidgets;

public partial class WidgetLaunchGame : IWidgetTemplated
{
    private bool _isUpdating;
    private bool _hasValidGame;

    public WidgetLaunchGame()
    {
        SupportWidgetSize = new()
        {
            WidgetSize.ExtraLarge,
            WidgetSize.Large
        };
        InitializeComponent();
        

        _ = UpdateUIAsync();

        GlobalModel.Config.AfterSave += async (sender, args) =>
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await UpdateUIAsync();
            });
        };

        LaunchButton.Click += LaunchButton_OnClick;
    }

    private Bitmap GetImage(string url)
    {
        var uri = new Uri(url);
        using (var stream = AssetLoader.Open(uri))
        {
            return new Bitmap(stream);
        }
    }

    private async Task UpdateUIAsync()
    {
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            var config = GlobalModel.Config.Data;

            if (config.GameFolders == null || config.GameFolders.Count == 0)
            {
                SetEmptyState();
                return;
            }

            if (config.GameFolderSelIndex < 0 || config.GameFolderSelIndex >= config.GameFolders.Count)
            {
                SetEmptyState();
                return;
            }

            var gameFolder = config.GameFolders[config.GameFolderSelIndex];
            var versions = await GameInfoHelper.GetVersionConfigsAsync(gameFolder.GameFolderPath);

            if (versions == null || versions.Count == 0)
            {
                SetEmptyState();
                return;
            }

            if (gameFolder.GameSelIndex < 0 || gameFolder.GameSelIndex >= versions.Count)
            {
                gameFolder.GameSelIndex = 0;
                GlobalModel.Config.Save();
            }

            var version = versions[gameFolder.GameSelIndex];
            UpdateGameInfo(version);
            _hasValidGame = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[WidgetLaunchGame] 更新 UI 失败：{ex.Message}");
            SetEmptyState();
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private async Task UpdateGameInfo(VersionConfig version)
    {
        if (version == null)
        {
            SetEmptyState();
            return;
        }

        GameNameText.Text = version.Info.VersionName;
        GameVersionText.Text = version.Info.Version;
        GameIcon.Source = await ImageLoader.LoadIconAsync(IconHelper.GetGameIconUrl(version));
        LaunchButton.IsEnabled = true;
        _hasValidGame = true;
    }

    private void SetEmptyState()
    {
        GameNameText.Text = "游戏";
        GameVersionText.Text = "未选择版本";
        GameIcon.Source = GetImage("avares://BedrockBoot/Assets/Icon/Logo/Grass.png");
        LaunchButton.IsEnabled = false;
        _hasValidGame = false;
    }

    private async void LaunchButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_hasValidGame) return;

        try
        {
            var config = GlobalModel.Config.Data;

            if (config.GameFolders == null || config.GameFolders.Count == 0) return;
            if (config.GameFolderSelIndex < 0 || config.GameFolderSelIndex >= config.GameFolders.Count) return;

            var gameFolder = config.GameFolders[config.GameFolderSelIndex];
            var versions = await GameInfoHelper.GetVersionConfigsAsync(gameFolder.GameFolderPath);

            if (versions == null || versions.Count == 0) return;
            if (gameFolder.GameSelIndex < 0 || gameFolder.GameSelIndex >= versions.Count) return;

            var version = versions[gameFolder.GameSelIndex];
            TaskLaunchGameItem.Launch(version);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[WidgetLaunchGame] 启动游戏失败：{ex.Message}");
        }
    }
}