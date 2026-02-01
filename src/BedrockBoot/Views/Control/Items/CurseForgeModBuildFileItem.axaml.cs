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

        Update();
    }

    public CurseForgeResponse.ModFile ModFile { get; set; }

    private void Update()
    {
        Card.Header = ModFile.DisplayName;
        Card.Description = $"{ModFile.FileDate.ToShortDateString()} {ModFile.FileDate.ToShortTimeString()}";
    }

    private async void SaveBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "下载资源包",
            SuggestedFileName = ModFile.FileName,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Minecraft Bedrock 资源文件")
                {
                    Patterns = new[] { Path.GetExtension(ModFile.FileName) }
                }
            }
        });

        if (file is not null)
        {
            GlobalModel.MainWindow.CloseDraw();
            TaskDownloadCurseForgeResourceItem.Download(ModFile, file.TryGetLocalPath());
        }
    }

    private void DownloadBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogChooseGameContent();
        DialogHost.Show(new DialogInfo()
        {
            Content = dialog,
            Title = "下载资源到...",
            CloseButtonText = "下载",
            SecondaryButtonText = "取消",
            CloseAction = () =>
            {
                var conf = dialog.VersionConfig;
                GlobalModel.MainWindow.CloseDraw();
                TaskDownloadCurseForgeResourceItem.Download(ModFile,
                    Path.Combine(PathsList.TempPath, ModFile.FileName), conf);
            }
        });
    }
}