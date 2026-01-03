using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Views.Control;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstancePack : UserControl
{
    public VersionConfig VersionInfo { get; set; }

    public InstancePack()
    {
        InitializeComponent();
    }

    public InstancePack(VersionConfig versionConfig) : this()
    {
        VersionInfo = versionConfig;
        new ResourcePackManager(VersionInfo).GetAllPack().ForEach(x =>
        {
            if (x != null &&
                x.Header != null)
            {
                Console.WriteLine($"找到包：{x.Header.Name}");
                ResultBox.Children.Add(new GameResourcePackItem(x));
            }
        });
    }

    private async void ImportPackBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "导入 Minecraft Bedrock 包",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Minecraft 支持包")
                {
                    Patterns = new[] { "*.mcpack", "*.mcaddon" }
                }
            }
        });

        if (files != null && files.Count >= 1)
        {
            IStorageFile selectedFile = files[0];
            string filePath = selectedFile.Path.LocalPath;

            if (File.Exists(filePath))
            {
                // todo
            }
        }
    }
}