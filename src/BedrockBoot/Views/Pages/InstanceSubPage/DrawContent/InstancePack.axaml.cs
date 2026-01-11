using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Views.Control;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstancePack : UserControl
{
    public VersionConfig VersionInfo { get; set; }
    public ResourcePackManager ResourcePackManager { get; set; }

    public InstancePack()
    {
        InitializeComponent();
    }

    public InstancePack(VersionConfig versionConfig) : this()
    {
        VersionInfo = versionConfig;
        Update();
    }

    private string _type = "resource";

    public void Update()
    {
        ResultBox.Children.Clear();
        ResourcePackManager = new ResourcePackManager(VersionInfo);
        ResourcePackManager.GetAllPack();
        var pack = ResourcePackManager.Packs;

        NullBox.IsVisible = pack.Count == 0;
        
        pack.ForEach(x =>
        {
            if (x != null &&
                x.Header != null)
            {
                if (x.PackType.ToString().ToLower() == _type)
                {
                    Console.WriteLine($"找到包：{x.Header.Name}");
                    ResultBox.Children.Add(new GameResourcePackItem(x)
                    {
                        RefreshCallBack = Update
                    });
                }
            }
        });
    }

    private async void ImportPackBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "导入 Minecraft Bedrock 包",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Minecraft 支持包")
                {
                    Patterns = new[] { "*.mcpack", "*.mcaddon", "*.zip" }
                }
            }
        });

        if (files != null && files.Count >= 1)
        {
            var selectedFiles = files.Select(f => f.Path.LocalPath).ToList();

            var body = new DialogImportResourcePackContent();
            DialogHost.Show(new()
            {
                Title = "导入包",
                Content = body,
                CloseButtonText = "导入",
                PrimaryButtonText = "取消",
                CloseAction = () =>
                {
                    DialogHost.Show(new DialogInfo()
                    {
                        Title = "导入包...",
                        Content = "正在导入包..."
                    });
                    Task.Run(() =>
                    {
                        ResourcePackManager.AddRangePacks(selectedFiles);

                        Dispatcher.UIThread.Invoke((() => DialogHost.Close()));
                        Dispatcher.UIThread.Invoke(Update);
                    });
                }
            });
            body.Import(selectedFiles);
        }
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var pack = ResourcePackManager.Packs.Where(p => p.Header.Name.Contains(SearchBox.Text)).ToList();
        NullBox.IsVisible = pack.Count == 0;
        ResultBox.Children.Clear();
        
        pack.ForEach(x =>
        {
            if (x != null &&
                x.Header != null)
            {
                Console.WriteLine($"找到包：{x.Header.Name}");
                ResultBox.Children.Add(new GameResourcePackItem(x)
                {
                    RefreshCallBack = Update
                });
            }
        });
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            _type = ((ListBoxItem)TypeSel.SelectedItem).Tag.ToString().ToLower();
            Update();
        }
        catch
        {
        }
    }
}