using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Base.Enum;
using BedrockBoot.Chunker.Base.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Chunker;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DialogContent.Chunker;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Window = Avalonia.Controls.Window;

namespace BedrockBoot.Views.DrawContent.Chunker;

public partial class DrawChunkerJavaToBedrockContent : UserControl
{
    public DrawChunkerJavaToBedrockContent()
    {
        InitializeComponent();
    }

    private async void ImportArchive_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择 Java Edition 存档压缩包",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("压缩文件")
                {
                    Patterns = new[] { "*.zip" }
                }
            }
        });

        if (files.Count > 0)
        {
            var selectedFile = files[0].Path.LocalPath;

            var extension = Path.GetExtension(selectedFile).ToLower();
            if (extension.Contains("zip"))
                WorldPath.Text = selectedFile;
            else
                DialogHost.Show(new DialogInfo
                {
                    Title = "错误的文件",
                    Content = "您选择的文件非 zip 文件",
                    CloseButtonText = "确定"
                });
        }
    }

    private async void SaveMcWorld_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存为 McWorld 文件",
            DefaultExtension = "mcworld",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Minecraft 基岩版存档文件")
                {
                    Patterns = new[] { "*.mcworld" }
                }
            }
        });

        if (file != null)
        {
            var result = file.Path.LocalPath;
            DialogHost.Show(new DialogInfo
            {
                Title = "转换存档",
                Content = new DialogChunkerConversionContent(
                    ChunkerType.JavaToBedrock,
                    SaveType.File,
                    BedrockBoot.Chunker.Chunker.SupportJava[GameVersionChoose.SelectedIndex],
                    WorldPath.Text!,
                    result)
            });
        }
    }

    private void SaveIns_OnClick(object? sender, RoutedEventArgs e)
    {
        var ins = new DialogChooseGameContent();
        DialogHost.Show(new DialogInfo
        {
            Title = "选择实例",
            Content = ins,
            CloseButtonText = "确定",
            CloseAction = () =>
            {
                var insInfo = ins.VersionConfig;

                DialogHost.Show(new DialogInfo
                {
                    Title = "转换存档",
                    Content = new DialogChunkerConversionContent(
                        ChunkerType.JavaToBedrock,
                        SaveType.File,
                        BedrockBoot.Chunker.Chunker.SupportBedrock[GameVersionChoose.SelectedIndex],
                        WorldPath.Text!,
                        Path.Combine(ChunkerHelper.ChunkerTempFolderPath,
                            $"pack_input_{Guid.NewGuid().ToString().Replace("-", "")}.mcworld"),
                        s =>
                        {
                            Task.Run(() =>
                            {
                                var acrh = new ArchiveCheck(insInfo);
                                acrh.Check();
                                acrh.ImportWorldPack(s);

                                Dispatcher.UIThread.Invoke(() =>
                                {
                                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                                    {
                                        Title = "导入完成",
                                        Message = "已完成转换并导入"
                                    });
                                });
                            });
                        })
                });
            },
            PrimaryButtonText = "取消",
            AccountButton = DialogButtons.CloseButton
        });
    }
}