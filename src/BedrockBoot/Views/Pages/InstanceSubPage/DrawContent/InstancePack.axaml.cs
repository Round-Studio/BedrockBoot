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
    
    private string _type = "resource";
    private string _searchText = string.Empty;

    public InstancePack()
    {
        InitializeComponent();
    }

    public InstancePack(VersionConfig versionConfig) : this()
    {
        VersionInfo = versionConfig;
        UpdateUI();
    }

    private void UpdateUI()
    {
        // 清空当前显示
        ResultBox.Children.Clear();
        ScBox.IsVisible = false;
        LoadBox.IsVisible = true;
        
        Task.Run(() =>
        {
            if (ResourcePackManager == null)
            {
                ResourcePackManager = new ResourcePackManager(VersionInfo);
                ResourcePackManager.GetAllPack();
            }
            ResourcePackManager.GetAllPack();

            var filteredPacks = ResourcePackManager.Packs
                .Where(x => x != null && x.Header != null)
                .Where(x => x.PackType.ToString().ToLower() == _type)
                .Where(x => string.IsNullOrWhiteSpace(_searchText) || 
                           x.Header.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Dispatcher.UIThread.Invoke(() =>
            {
                NullBox.IsVisible = filteredPacks.Count == 0;
                ScBox.IsVisible = false;
            });
            
            // 如果有包，添加它们
            if (filteredPacks.Count > 0)
            {
                foreach (var pack in filteredPacks)
                {
                    Dispatcher.UIThread.Invoke(() => ResultBox.Children.Add(new GameResourcePackItem(pack)
                    {
                        RefreshCallBack = UpdateUI
                    }));
                }
            }
            
            Dispatcher.UIThread.Invoke(() =>
            {
                ScBox.IsVisible = true;
                LoadBox.IsVisible = false;
            });
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
                CloseAction = async () =>
                {
                    DialogHost.Show(new DialogInfo()
                    {
                        Title = "导入包...",
                        Content = "正在导入包..."
                    });
                    
                    await Task.Run(() =>
                    {
                        ResourcePackManager.AddRangePacks(selectedFiles);
                    });

                    Dispatcher.UIThread.Invoke(() =>
                    {
                        DialogHost.Close();
                        UpdateUI();
                    });
                }
            });
            body.Import(selectedFiles);
        }
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text ?? string.Empty;
        UpdateUI();
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            _type = ((ListBoxItem)TypeSel.SelectedItem).Tag.ToString().ToLower();
            UpdateUI();
        }
        catch
        {
            // 保持当前类型
        }
    }
    
    // 如果需要手动刷新数据（比如从其他页面返回时）
    public void RefreshData()
    {
        if (ResourcePackManager != null)
        {
            ResourcePackManager.GetAllPack();
        }
        UpdateUI();
    }
}