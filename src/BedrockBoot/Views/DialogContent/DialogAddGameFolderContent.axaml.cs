using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogAddGameFolderContent : UserControl
{
    private static I18nManager i18n => I18nManager.Instance;

    public DialogAddGameFolderContent()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 获取或设置选中的文件夹路径
    /// </summary>
    public string FolderPath
    {
        get => PathInputBox.Text ?? string.Empty;
        set => PathInputBox.Text = value;
    }

    /// <summary>
    /// 获取或设置文件夹的显示名称
    /// </summary>
    public string FolderName
    {
        get => PathNameInputBox.Text ?? string.Empty;
        set => PathNameInputBox.Text = value;
    }

    /// <summary>
    /// 调用系统文件夹选择器
    /// </summary>
    private async void OpenChooseFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // 调用 Avalonia 11+ 标准存储提供程序
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = i18n["Dialog.AddFolder.Picker.Title"],
                AllowMultiple = false
            });

        if (folders is { Count: > 0 })
        {
            try
            {
                var folder = folders[0];
                var path = folder.Path.LocalPath;

                // 路径合法性初步校验
                if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Path is null");

                PathInputBox.Text = path;

                // 修正：获取当前选中的文件夹名称（而非其父级名称）
                // 使用 DirectoryInfo 处理路径，能自动适配不同系统的路径分隔符
                var dirInfo = new DirectoryInfo(path);
                
                // 如果是磁盘根目录（如 C:\），则使用全路径作为名称
                PathNameInputBox.Text = string.IsNullOrEmpty(dirInfo.Name) || dirInfo.Name == dirInfo.Root.Name
                    ? path 
                    : dirInfo.Name;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Folder selection error: {ex.Message}");
                
                // 提示用户路径无效
                DialogHost.Close();
                DialogHost.Show(new DialogInfo
                {
                    Title = i18n["MainWindow.Dialog.Error.Title"],
                    Content = i18n["Dialog.AddFolder.Error.InvalidPath"],
                    CloseButtonText = i18n["MainWindow.Common.Confirm"]
                });
            }
        }
    }
}