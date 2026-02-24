using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
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
        var dialog = new OpenFileDialog
        {
            Title = "选择 Java Edition 存档压缩包",
            AllowMultiple = false,
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter
                {
                    Name = "压缩文件",
                    Extensions = new List<string> { "zip" }
                }
            }
        };

        var window = this.VisualRoot as Window;
        if (window == null) return;

        var result = await dialog.ShowAsync(window);
        
        if (result != null && result.Any())
        {
            string selectedFile = result.First();
            
            string extension = Path.GetExtension(selectedFile).ToLower();
            if (extension.Contains("zip"))
            {
                WorldPath.Text = selectedFile;
            }
            else
            {
                DialogHost.Show(new()
                {
                    Title = "错误的文件",
                    Content = "您选择的文件非 zip 文件",
                    CloseButtonText = "确定"
                });
            }
        }
    }

    private async void SaveMcWorld_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "保存为 McWorld 文件",
            DefaultExtension = "mcworld",
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter
                {
                    Name = "Minecraft 基岩版存档文件",
                    Extensions = new List<string> { "mcworld" }
                }
            }
        };

        var window = this.VisualRoot as Window;
        if (window == null) return;

        var result = await dialog.ShowAsync(window);
        
        if (!string.IsNullOrWhiteSpace(result))
        {
            DialogHost.Show(new()
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
        DialogHost.Show(new()
        {
            Title = "选择实例",
            Content = ins,
            CloseButtonText = "确定",
            CloseAction = () =>
            {
                var insInfo = ins.VersionConfig;

                DialogHost.Show(new()
                {
                    Title = "转换存档",
                    Content = new DialogChunkerConversionContent(
                        ChunkerType.JavaToBedrock,
                        SaveType.File,
                        BedrockBoot.Chunker.Chunker.SupportBedrock[GameVersionChoose.SelectedIndex],
                        WorldPath.Text!,
                        Path.Combine(ChunkerHelper.ChunkerTempFolderPath,
                            $"pack_input_{Guid.NewGuid().ToString().Replace("-", "")}.mcworld"),
                        (s =>
                        {
                            Task.Run(() =>
                            {
                                var acrh = new ArchiveCheck(insInfo);
                                acrh.Check();
                                acrh.ImportWorldPack(s);

                                Dispatcher.UIThread.Invoke(() =>
                                {
                                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo()
                                    {
                                        Title = "导入完成",
                                        Message = "已完成转换并导入"
                                    });
                                });
                            });
                        }))
                });
            },
            PrimaryButtonText = "取消",
            AccountButton = DialogButtons.CloseButton
        });
    }
}