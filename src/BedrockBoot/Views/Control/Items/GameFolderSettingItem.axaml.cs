using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Control.Items;

public partial class GameFolderSettingItem : UserControl
{
    private static I18nManager i18n => I18nManager.Instance;
    
    public GameFolderInfo GameFolderInfo { get; set; } = null!;
    public Action? CallBack { get; set; }

    public GameFolderSettingItem()
    {
        InitializeComponent();
    }

    public GameFolderSettingItem(GameFolderInfo info, Action callBack) : this()
    {
        GameFolderInfo = info;
        CallBack = callBack;
        UpdateUI();
    }

    private void UpdateUI()
    {
        Card.Header = GameFolderInfo.GameFolderName;
        Card.Description = GameFolderInfo.GameFolderPath;
    }

    /// <summary>
    /// 使用系统文件管理器打开目录
    /// </summary>
    private void OpenFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(GameFolderInfo.GameFolderPath)) return;
        
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = GameFolderInfo.GameFolderPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Explorer] Failed to open folder: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除该目录索引
    /// </summary>
    private void DeleteFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = i18n["Setting.Folder.Delete.Title"],
            Content = i18n["Setting.Folder.Delete.Content"],
            SecondaryButtonText = i18n["MainWindow.Common.Cancel"],
            CloseButtonText = i18n["MainWindow.Common.Delete"],
            AccountButton = DialogButtons.SecondaryButton,
            CloseAction = () =>
            {
                var folders = GlobalModel.Config.Data.GameFolders;
                var target = folders.Find(x => 
                    x.GameFolderPath == GameFolderInfo.GameFolderPath && 
                    x.GameFolderName == GameFolderInfo.GameFolderName);

                if (target != null)
                {
                    folders.Remove(target);
                    GlobalModel.Config.Save();
                    CallBack?.Invoke();
                }
            }
        });
    }

    /// <summary>
    /// 编辑现有目录配置
    /// </summary>
    private void SettingBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var body = new DialogAddGameFolderContent
        {
            FolderName = GameFolderInfo.GameFolderName,
            FolderPath = GameFolderInfo.GameFolderPath
        };

        DialogHost.Show(new DialogInfo
        {
            Title = i18n["Setting.Folder.Edit.Title"],
            Content = body,
            SecondaryButtonText = i18n["MainWindow.Common.Cancel"],
            CloseButtonText = i18n["MainWindow.Common.Save"],
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                var folders = GlobalModel.Config.Data.GameFolders;
                // 查找原始对象进行修改
                var target = folders.Find(x => 
                    x.GameFolderPath == GameFolderInfo.GameFolderPath && 
                    x.GameFolderName == GameFolderInfo.GameFolderName);

                if (target != null)
                {
                    target.GameFolderName = body.FolderName;
                    target.GameFolderPath = body.FolderPath;
                    
                    GlobalModel.Config.Save();
                    
                    // 同步更新当前组件 UI
                    GameFolderInfo = target;
                    UpdateUI();
                    
                    // 通知父级页面刷新列表
                    CallBack?.Invoke();
                }
            }
        });
    }
}