using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Control.Items;

public partial class CurseForgeModBuildFileItem : UserControl
{
    public CurseForgeModBuildFileItem()
    {
        InitializeComponent();
    }

    public CurseForgeModBuildFileItem(CurseForgeResponse.ModFile modFile) : this()
    {
        ModFile = modFile;
        UpdateUI();
    }

    private static I18nManager i18n => I18nManager.Instance;
    public CurseForgeResponse.ModFile ModFile { get; set; } = null!;

    private void UpdateUI()
    {
        Card.Header = ModFile.DisplayName;
        Card.Description = $"{ModFile.FileDate.ToString("yyyy/MM/dd HH:mm")}, {ToFileSizeString(ModFile.FileLength)}";
    }
    
    public static string ToFileSizeString(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
    
    /// <summary>
    ///     另存为：手动选择下载位置
    /// </summary>
    private async void SaveBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // 获取文件后缀，如果没有则默认为 .mcpack
        var extension = Path.GetExtension(ModFile.FileName);
        if (string.IsNullOrEmpty(extension)) extension = ".mcpack";

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = i18n["Download.CurseForge.SaveAs.Title"],
            SuggestedFileName = ModFile.FileName,
            FileTypeChoices = new[]
            {
                new FilePickerFileType(i18n["Download.CurseForge.FileType.Bedrock"])
                {
                    Patterns = new[] { $"*{extension}" }
                }
            }
        });

        var localPath = file?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(localPath))
        {
            GlobalModel.MainWindow.CloseDraw();
            TaskDownloadCurseForgeResourceItem.Download(ModFile, localPath);
        }
    }

    /// <summary>
    ///     下载到实例：弹出对话框选择目标游戏实例
    /// </summary>
    private void DownloadBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogChooseGameContent();

        DialogHost.Show(new DialogInfo
        {
            Content = dialog,
            Title = i18n["Download.CurseForge.InstallTo.Title"],
            CloseButtonText = "下载",
            SecondaryButtonText = i18n["MainWindow.Common.Cancel"],
            CloseAction = () =>
            {
                var conf = dialog.VersionConfig;
                if (conf == null) return;

                GlobalModel.MainWindow.CloseDraw();

                // 下载到临时目录并触发自动导入逻辑
                var tempFilePath = Path.Combine(PathsList.TempPath, ModFile.FileName);
                TaskDownloadCurseForgeResourceItem.Download(ModFile, tempFilePath, conf);
            }
        });
    }
}