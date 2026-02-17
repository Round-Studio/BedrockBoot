using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstancePack : ISetting
{
    private static I18nManager i18n => I18nManager.Instance;
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

    public VersionConfig VersionInfo { get; set; }
    public ResourcePackManager? ResourcePackManager { get; set; }

    /// <summary>
    /// 异步更新 UI 列表
    /// </summary>
    private void UpdateUI()
    {
        ResultBox.Children.Clear();
        ScBox.IsVisible = false;
        LoadBox.IsVisible = true;

        Task.Run(() =>
        {
            if (ResourcePackManager == null)
            {
                ResourcePackManager = new ResourcePackManager(VersionInfo);
            }

            // 重新获取包数据
            ResourcePackManager.GetAllPack();

            var filteredPacks = ResourcePackManager.Packs
                .Where(x => x?.Header != null)
                .Where(x => x.PackType.ToString().Equals(_type, StringComparison.OrdinalIgnoreCase))
                .Where(x => string.IsNullOrWhiteSpace(_searchText) ||
                            x.Header.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // 批量生成 UI 控件，避免在循环中频繁调用 Dispatcher
            var packItems = filteredPacks.Select(pack => new GameResourcePackItem(pack)
            {
                RefreshCallBack = UpdateUI
            }).ToList();

            Dispatcher.UIThread.Invoke(() =>
            {
                NullBox.IsVisible = filteredPacks.Count == 0;
                NumberBox.Text = string.Format(i18n["Instance.Pack.Count.Format"], filteredPacks.Count);
                
                foreach (var item in packItems)
                {
                    ResultBox.Children.Add(item);
                }

                ScBox.IsVisible = true;
                LoadBox.IsVisible = false;
            });
        });
    }

    /// <summary>
    /// 导入包文件 (.mcpack / .mcaddon)
    /// </summary>
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
                    Patterns = new[] { "*.mcpack", "*.mcaddon" }
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
                    // 显示进度提示
                    DialogHost.Show(new DialogInfo
                    {
                        Title = i18n["Instance.Pack.Import.Progress.Title"],
                        Content = i18n["Instance.Pack.Import.Progress.Content"]
                    });

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
        if (IsEdit && TypeSel?.SelectedItem is ListBoxItem item)
        {
            _type = item.Tag?.ToString()?.ToLower() ?? "resource";
            UpdateUI();
        }
    }

    public void RefreshData()
    {
        ResourcePackManager?.GetAllPack();
        UpdateUI();
    }
}