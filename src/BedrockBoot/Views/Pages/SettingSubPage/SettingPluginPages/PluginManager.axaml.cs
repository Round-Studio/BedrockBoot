using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using BedrockBoot.Interface;
using BedrockBoot.Models.Pack.Plugin;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingPluginPages;

public partial class PluginManager : ISettingPage
{
    public PluginManager()
    {
        InitializeComponent();
        UpdateUI();

        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "插件",
                ItemClickAction = info =>
                    MainSettingPage.NavigateTo(new SettingPlugin())
            },
            new()
            {
                ItemName = "管理"
            }
        };
    }

    public void UpdateUI()
    {
        LoadingCard.IsVisible = true;
        InfoCard.IsVisible = false;
        PluginList.Children.Clear();
        ScrollViewer.IsVisible = false;

        if (PluginLoader.Plugins.Count == 0)
        {
            LoadingCard.IsVisible = false;
            InfoCard.IsVisible = true;
        }
        
        PluginLoader.Plugins.ForEach(plugin =>
        {
            PluginList.Children.Add(new PluginItem(plugin));
        });
        
        ScrollViewer.IsVisible = true;
        LoadingCard.IsVisible = false;
        InfoCard.IsVisible = false;
    }
    private async void ImportButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择插件包",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("BedrockBoot 插件包")
                {
                    Patterns = new[] { "*.rplck" }
                }
            }
        });

        if (files.Count > 0)
        {
            string selectedPath = files[0].Path.LocalPath;
            bool success = await PluginLoader.Install(selectedPath);
            
            if (success)
            {
                UpdateUI();
                // 此处可根据需要调用 OnePointUI 的通知组件提示：导入成功，重启生效
            }
        }
    }
}