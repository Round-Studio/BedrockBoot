using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Enum;
using BedrockBoot.Interface;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Isolation;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstancePack : ISetting
{
    private string _searchText = string.Empty;
    private string _type = "resource";

    public InstancePack()
    {
        InitializeComponent();
        IsEdit = true;
    }

    public InstancePack(VersionConfig versionConfig) : this()
    {
        VersionInfo = versionConfig;
        UpdateUI();
    }

    private static I18nManager i18n => I18nManager.Instance;

    public VersionConfig VersionInfo { get; set; }
    public ResourcePackManager? ResourcePackManager { get; set; }

    private void UpdateUI()
    {
        // UI 状态重置
        ResultBox.Children.Clear();
        ScBox.IsVisible = false;
        NullBox.IsVisible = false;
        LoadBox.IsVisible = true;

        Task.Run(() =>
        {
            if (ResourcePackManager == null) ResourcePackManager = new ResourcePackManager(VersionInfo);

            // 1. 在后台线程处理数据解析（IO 密集型）
            ResourcePackManager.GetAllPack();

            var filteredPacks = ResourcePackManager.Packs
                .Where(x => x?.Header != null)
                .Where(x => x.PackType.ToString().Equals(_type, StringComparison.OrdinalIgnoreCase))
                .Where(x => string.IsNullOrWhiteSpace(_searchText) ||
                            x.Header.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // 2. 切回 UI 线程进行控件实例化和渲染
            Dispatcher.UIThread.Invoke(() =>
            {
                NullBox.IsVisible = filteredPacks.Count == 0;
                NumberBox.Text = string.Format(i18n["Instance.Pack.Count.Format"], filteredPacks.Count);

                // 必须在 UI 线程内 new 控件
                var packItems = filteredPacks.Select(pack => new GameResourcePackItem(pack)
                {
                    RefreshCallBack = UpdateUI
                }).ToList();

                ResultBox.Children.AddRange(packItems);

                ScBox.IsVisible = true;
                LoadBox.IsVisible = false;
            });
        });
    }

    private async void ImportPackBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = i18n["Instance.Pack.Import.Picker.Title"],
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(i18n["Instance.Pack.Import.Picker.Type"])
                {
                    Patterns = new[] { "*.mcpack", "*.mcaddon", "*.mctemplate" }
                }
            }
        });

        if (files is { Count: >= 1 })
        {
            var selectedFiles = files.Select(f => f.Path.LocalPath).ToList();
            var body = new DialogImportResourcePackContent();

            DialogHost.Show(new DialogInfo
            {
                Title = i18n["Instance.Pack.Import.Dialog.Title"],
                Content = body,
                CloseButtonText = i18n["Instance.Pack.Import.Dialog.Action"],
                PrimaryButtonText = i18n["MainWindow.Common.Cancel"],
                CloseAction = async () =>
                {
                    DialogHost.Show(new DialogInfo
                    {
                        Title = i18n["Instance.Pack.Import.Progress.Title"],
                        Content = i18n["Instance.Pack.Import.Progress.Content"]
                    });

                    // 导入操作在后台执行
                    await Task.Run(() => { ResourcePackManager?.AddRangePacks(selectedFiles); });

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
        // 增加 IsEdit 判断防止初始化时的意外触发
        if (IsEdit && TypeSel?.SelectedItem is ListBoxItem item)
        {
            var newType = item.Tag?.ToString()?.ToLower() ?? "resource";
            if (_type != newType)
            {
                _type = newType;
                UpdateUI();
            }
        }
    }

    private void FolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (IsEdit && TypeSel?.SelectedItem is ListBoxItem item)
        {
            var folder = IsolationCore.GetInstanceFolderPath(VersionInfo, (item.Tag?.ToString()?.ToLower() ?? "resource") switch
            {
                "resource" => InstanceFolderType.ResourcePackFolder,
                "behavior" => InstanceFolderType.BehaviorPackFolder,
                "skin" => InstanceFolderType.SkinPackFolder,
                "template" => InstanceFolderType.WorldTemplateFolder,
                _ => InstanceFolderType.UserFolder
            });
            
            OpenFolderHelper.Open(folder);
        }
    }
}