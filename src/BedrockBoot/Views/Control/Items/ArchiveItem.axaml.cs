using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Helper;

namespace BedrockBoot.Views.Control.Items;

public partial class ArchiveItem : UserControl
{
    public ArchiveItem()
    {
        InitializeComponent();
    }

    public ArchiveItem(ArchiveInfo info, bool isView = false) : this()
    {
        ArchiveInfo = info;
        UpdateUI();

        ControlPanel.IsVisible = !isView;
    }

    private static I18nManager i18n => I18nManager.Instance;
    public ArchiveInfo? ArchiveInfo { get; set; }
    public Action? EditAction { get; set; }
    public Action? RefreshCallBack { get; set; }

    /// <summary>
    ///     更新存档卡片 UI
    /// </summary>
    public void UpdateUI()
    {
        if (ArchiveInfo == null) return;

        WorldName.Text = ArchiveInfo.LevelWorldData.LevelName;

        // 时间转换与格式化
        var lastPlayedTime = UnixTimeConverter.UnixTimeStampToDateTime(ArchiveInfo.LevelWorldData.LastPlayed);
        WorldLastPlayed.Text = lastPlayedTime.ToString("yyyy/MM/dd HH:mm");

        // 标记是否为项目实例
        ProjectLabel.IsVisible = ArchiveInfo.IsProject;

        // 异步或流式加载图标，防止文件占用
        LoadWorldIcon(ArchiveInfo.IconPath);
    }

    private void LoadWorldIcon(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

        try
        {
            using var stream = File.OpenRead(path);
            var bitmap = new Bitmap(stream);
            ImageBox.Background = new ImageBrush
            {
                Stretch = Stretch.UniformToFill,
                Source = bitmap
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Failed to load world icon: {ex.Message}");
        }
    }

    /// <summary>
    ///     在资源管理器中打开存档目录
    /// </summary>
    private void OpenFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ArchiveInfo == null || !Directory.Exists(ArchiveInfo.Path)) return;

        OpenFolderHelper.Open(ArchiveInfo.Path);
    }

    /// <summary>
    ///     导出存档为 .mcworld 包
    /// </summary>
    private async void SaveBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ArchiveInfo == null) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = i18n["Archive.Export.Title"],
            DefaultExtension = "mcworld",
            SuggestedFileName = $"{ArchiveInfo.Name}.mcworld",
            FileTypeChoices = new[]
            {
                new FilePickerFileType(i18n["Archive.Export.FileType"])
                {
                    Patterns = new[] { "*.mcworld" }
                }
            }
        });

        var localPath = file?.TryGetLocalPath();
        if (string.IsNullOrEmpty(localPath)) return;

        try
        {
            ArchiveInfo.Save(localPath);
            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
            {
                Title = i18n["Common.Success"],
                Message = i18n["Archive.Export.Success"],
                NoticeType = NoticeType.Info
            });
        }
        catch (Exception ex)
        {
            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
            {
                Title = i18n["MainWindow.Dialog.Error.Title"],
                Message = ex.Message,
                NoticeType = NoticeType.Error
            });
        }
    }

    private void EditBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        EditAction?.Invoke();
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = "删除警告",
            Content = "你确定要删除此存档吗，这将会失去很久...",
            CloseButtonText = "确定删除",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                ArchiveInfo.Delete();

                RefreshCallBack?.Invoke();
            }
        });
    }
}