using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Pack.Game.Import;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.TaskItem;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainHomePage : BedrockBootPage
{
    private bool _isEditing = false;
    
    public MainHomePage()
    {
        InitializeComponent();
        UpdateUI();
    }

    /// <summary>
    /// 更新主页面UI状态
    /// </summary>
    public void UpdateUI()
    {
        _isEditing = false;
        
        try
        {
            // 检查是否有可用的游戏文件夹
            if (HasValidGameFolder())
            {
                SetupGameSelectionUI();
            }
            else
            {
                SetupEmptyStateUI();
            }
        }
        catch (Exception ex)
        {
            // 处理异常情况
            HandleUIUpdateError(ex);
        }
        finally
        {
            _isEditing = true;
        }
    }

    /// <summary>
    /// 检查是否存在有效的游戏文件夹
    /// </summary>
    private bool HasValidGameFolder()
    {
        var config = GlobalModel.Config.Data;
        return config.GameFolders?.Count > 0 && 
               config.GameFolderSelIndex >= 0 && 
               config.GameFolderSelIndex < config.GameFolders.Count;
    }

    /// <summary>
    /// 设置游戏选择UI
    /// </summary>
    private void SetupGameSelectionUI()
    {
        EditLabel.IsVisible = false;
        BuildTypeLabel.IsVisible = false;
        BuildTypeLabel.Text = "未知";
        var config = GlobalModel.Config.Data;
        var selectedGameFolder = config.GameFolders[config.GameFolderSelIndex];
        
        // 获取版本信息
        var versions = GameInfoHelper.GetVersionConfigs(selectedGameFolder.GameFolderPath);
        
        // 启用设置卡片
        SetupCard.IsEnabled = true;
        
        // 更新游戏列表
        UpdateGameList(versions);
        
        // 设置选中项
        SetSelectedGame(versions, selectedGameFolder.GameSelIndex);
        
        // 更新启动能力状态
        UpdateLaunchAbility(versions);
    }

    /// <summary>
    /// 更新游戏列表
    /// </summary>
    private void UpdateGameList(List<VersionConfig> versions)
    {
        GameListChoose.Items.Clear();
        
        if (versions?.Count > 0)
        {
            foreach (var version in versions)
            {
                GameListChoose.Items.Add(new ComboBoxItem()
                {
                    Content = $"{version.Info.VersionName} ({version.Info.Version})",
                    Tag = version // 保存版本对象供后续使用
                });
            }
        }
        else
        {
            GameListChoose.Items.Add(new ComboBoxItem() { Content = "无可用版本" });
            UpdateSetupCard();
            GlobalModel.IsAbleToLaunchGame = false;
        }
    }

    /// <summary>
    /// 设置选中的游戏
    /// </summary>
    private void SetSelectedGame(List<VersionConfig> versions, int selectedIndex)
    {
        if (versions?.Count > 0)
        {
            // 验证并设置选中索引
            var validIndex = selectedIndex >= 0 && selectedIndex < versions.Count ? selectedIndex : 0;
            GameListChoose.SelectedIndex = validIndex;

            GlobalModel.IsAbleToLaunchGame = true;
            
            // 更新游戏标题
            if (validIndex < versions.Count)
            {
                var selectedVersion = versions[validIndex];
                GameTitle.Text = $"{selectedVersion.Info.VersionName}";
                
                EditLabel.IsVisible = selectedVersion.Config.IsEditModel;
                BuildTypeLabel.IsVisible = true;
                BuildTypeLabel.Text = selectedVersion.Info.BuildType.ToString();
                
                UpdateSetupCard(selectedVersion);
            }
        }
        else
        {
            GameListChoose.SelectedIndex = 0;
            GameTitle.Text = "请选择游戏";
        }
    }

    /// <summary>
    /// 更新启动游戏能力状态
    /// </summary>
    private void UpdateLaunchAbility(List<VersionConfig> versions)
    {
        GlobalModel.IsAbleToLaunchGame = versions?.Count > 0;
        
        if (!GlobalModel.IsAbleToLaunchGame)
        {
            GameTitle.Text = "无可用游戏版本";
            UpdateSetupCard();
        }
    }

    /// <summary>
    /// 设置空状态UI（无游戏文件夹时）
    /// </summary>
    private void SetupEmptyStateUI()
    {
        GameTitle.Text = "请选择游戏";
        UpdateSetupCard();
        GlobalModel.IsAbleToLaunchGame = false;
        
        GameListChoose.Items.Clear();
        GameListChoose.Items.Add(new ComboBoxItem() { Content = "无实例" });
        GameListChoose.SelectedIndex = 0;
    }

    /// <summary>
    /// 处理UI更新错误
    /// </summary>
    private void HandleUIUpdateError(Exception ex)
    {
        // 这里可以添加日志记录
        Console.WriteLine($"UI更新失败: {ex.Message}");
        
        // 设置错误状态
        GameTitle.Text = "无可用游戏版本";
        UpdateSetupCard();
        GlobalModel.IsAbleToLaunchGame = false;
        
        GameListChoose.Items.Clear();
        GameListChoose.Items.Add(new ComboBoxItem() { Content = "无可用版本" });
        GameListChoose.SelectedIndex = 0;
    }

    /// <summary>
    /// 当用户选择不同游戏版本时调用
    /// </summary>
    private void OnGameSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isEditing && sender is ComboBox comboBox)
        {
            var config = GlobalModel.Config.Data;
            
            if (HasValidGameFolder() && comboBox.SelectedIndex >= 0)
            {
                // 更新全局配置中的选中索引
                config.GameFolders[config.GameFolderSelIndex].GameSelIndex = comboBox.SelectedIndex;
                
                // 更新游戏标题
                if (comboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is VersionConfig content)
                {
                    EditLabel.IsVisible = false;
                    BuildTypeLabel.Text = "未知";
                    
                    GameTitle.Text = content.Info.VersionName;
                    EditLabel.IsVisible = content.Config.IsEditModel;
                    BuildTypeLabel.IsVisible = true;
                    BuildTypeLabel.Text = content.Info.BuildType.ToString();
                    UpdateSetupCard(content);
                }
                
                // 这里可以添加保存配置的逻辑
                GlobalModel.Config.Save();
            }
        }
    }
    
    private void UpdateSetupCard(VersionConfig info = null)
    {
        if(info == null)
            GlobalModel.IsAbleToLaunchGame = false;
        
        if (!GlobalModel.IsAbleToLaunchGame)
        {
            SetupCard.IsEnabled = false;
            GameType.Text = "...";
            GameVersion.Text = "...";
        }
        else
        {
            SetupCard.IsEnabled = true;
            GameType.Text = info.Info.VersionType.ToString();
            GameVersion.Text = info.Info.Version;
            
            LaunchGameBtn.Tag = info;
        }
    }

    private void LaunchGameBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (GlobalModel.IsAbleToLaunchGame)
        {
            var info = (VersionConfig)LaunchGameBtn.Tag;
            TaskLaunchGameItem.Launch(info);
        }
    }

    private void GameSettingBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (GlobalModel.IsAbleToLaunchGame)
        {
            var info = (VersionConfig)LaunchGameBtn.Tag;
            
            GlobalModel.MainWindow.OpenDraw(new DrawInstanceContent(info),$"{info.Info.VersionName} - {info.Info.Version}");
        }
    }
}