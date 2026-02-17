using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Views.Control.Items;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceSave : UserControl
{
    private static I18nManager i18n => I18nManager.Instance;
    public VersionConfig VersionInfo { get; set; }
    public ArchiveManifest? ArchiveManifest { get; private set; }
    public bool IsEdit { get; set; }

    private string SearchKey => SearchBox.Text ?? string.Empty;
    private int SelIndex => UserChooseBox.SelectedIndex;

    public InstanceSave()
    {
        IsEdit = false;
        InitializeComponent();
    }

    public InstanceSave(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
        UpdateUI();
    }

    /// <summary>
    /// 初始化并刷新存档元数据
    /// </summary>
    private void UpdateUI()
    {
        IsEdit = false;
        
        // 执行存档目录扫描
        var checker = new ArchiveCheck(VersionInfo);
        ArchiveManifest = checker.Check();

        UserChooseBox.Items.Clear();

        if (ArchiveManifest?.Manifest != null)
        {
            foreach (var user in ArchiveManifest.Manifest)
            {
                UserChooseBox.Items.Add(new ComboBoxItem
                {
                    Content = user.Key,
                    Tag = user.Value
                });
            }

            if (ArchiveManifest.Manifest.Count > 0)
            {
                UserChooseBox.SelectedIndex = 0;
                // 默认显示第一个用户的存档
                UpdateSaves(ArchiveManifest.Manifest.Values.FirstOrDefault() ?? new List<ArchiveInfo>());
            }
        }

        IsEdit = true;
    }

    /// <summary>
    /// 将存档对象渲染到 UI 列表
    /// </summary>
    public void UpdateSaves(List<ArchiveInfo> saves)
    {
        SavesBox.Children.Clear();
        NullBox.IsVisible = saves.Count <= 0;

        foreach (var save in saves)
        {
            SavesBox.Children.Add(new ArchiveItem(save));
        }
    }

    /// <summary>
    /// 处理搜索和用户切换逻辑
    /// </summary>
    public void UpdateSearch()
    {
        try
        {
            if (ArchiveManifest?.Manifest == null || ArchiveManifest.Manifest.Count == 0)
            {
                UpdateSaves(new List<ArchiveInfo>());
                return;
            }

            // 获取当前选中的用户存档列表
            List<ArchiveInfo> currentSaves;
            if (SelIndex >= 0 && SelIndex < UserChooseBox.Items.Count)
            {
                var selectedItem = UserChooseBox.Items[SelIndex] as ComboBoxItem;
                currentSaves = selectedItem?.Tag as List<ArchiveInfo> ?? new List<ArchiveInfo>();
            }
            else
            {
                currentSaves = ArchiveManifest.Manifest.Values.FirstOrDefault() ?? new List<ArchiveInfo>();
            }

            // 执行搜索过滤
            if (!string.IsNullOrEmpty(SearchKey))
            {
                var filtered = currentSaves
                    .Where(s => s.Name.Contains(SearchKey, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                UpdateSaves(filtered);
            }
            else
            {
                UpdateSaves(currentSaves);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"UpdateSearch error: {ex.Message}");
            UpdateSaves(new List<ArchiveInfo>());
        }
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (IsEdit) UpdateSearch();
    }

    private void UserChooseBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit) UpdateSearch();
    }

    /// <summary>
    /// 导入 .mcworld 存档包
    /// </summary>
    private async void ImportPackBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = i18n["Instance.Save.Import.Picker.Title"],
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(i18n["Instance.Save.Import.Picker.Type"])
                {
                    Patterns = new[] { "*.mcworld" }
                }
            }
        });

        if (files is { Count: >= 1 })
        {
            var path = files[0].Path.LocalPath;
            if (string.IsNullOrEmpty(path)) return;

            var checker = new ArchiveCheck(VersionInfo);
            // 默认导入到 Shared 目录（公共目录）
            checker.ImportWorldPack(path, "Shared");
            
            UpdateUI();
        }
    }
}