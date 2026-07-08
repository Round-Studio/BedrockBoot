using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;
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
    private string _currentType = "resource";
    private ResourcePackManager? _packManager;
    private bool _isLoading = false;

    public InstancePack()
    {
        InitializeComponent();
        IsEdit = true;
    }

    public InstancePack(VersionConfig versionConfig) : this()
    {
        VersionInfo = versionConfig;
        _ = RefreshPacksAsync();
    }

    private static I18nManager i18n => I18nManager.Instance;

    public VersionConfig VersionInfo { get; set; }

    public ResourcePackManager? PackManager => _packManager;

    private async Task RefreshPacksAsync()
    {
        if (_isLoading) return;
        _isLoading = true;

        try
        {
            LoadBox.IsVisible = _isLoading;
            ScBox.IsVisible = !_isLoading;
            NullBox.IsVisible = !_isLoading;
            ResultBox.Children.Clear();

            _packManager ??= VersionInfo != null ? new ResourcePackManager(VersionInfo) : null;
            if (_packManager == null) return;

            var filteredPacks = await Task.Run(() =>
            {
                _packManager.GetAllPack();
                return _packManager.Packs
                    .AsParallel()
                    .Where(x => x?.Header != null &&
                                x.PackType.ToString().Equals(_currentType, StringComparison.OrdinalIgnoreCase) &&
                                (string.IsNullOrWhiteSpace(_searchText) ||
                                 x.Header.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            });
            
            NullBox.IsVisible = filteredPacks.Count == 0;

            await Dispatcher.UIThread.InvokeAsync(() => UpdateUiWithPacks(filteredPacks));
        }
        finally
        {
            _isLoading = false;
            LoadBox.IsVisible = _isLoading;
            ScBox.IsVisible = !_isLoading;
        }
    }

    private void UpdateUiWithPacks(List<ResourcePackManifest> packs)
    {
        NumberBox.Text = string.Format(i18n["Instance.Pack.Count.Format"], packs.Count);

        if (packs.Count == 0)
        {
            NullBox.IsVisible = true;
            return;
        }

        NullBox.IsVisible = false;
        ScBox.IsVisible = true;
        ResultBox.Children.AddRange(packs.Select(pack => new GameResourcePackItem(pack)
        {
            RefreshCallBack = () => _ = RefreshPacksAsync()
        }));
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
            var filePaths = files.Select(f => f.Path.LocalPath).ToList();
            var body = new DialogImportResourcePackContent();
            
            DialogHost.Show(new DialogInfo
            {
                Title = i18n["Instance.Pack.Import.Dialog.Title"],
                Content = body,
                CloseButtonText = i18n["Instance.Pack.Import.Dialog.Action"],
                PrimaryButtonText = i18n["MainWindow.Common.Cancel"],
                CloseAction = async () =>
                {
                    DialogHost.Close();
                    DialogHost.Show(new DialogInfo
                    {
                        Title = i18n["Instance.Pack.Import.Progress.Title"],
                        Content = i18n["Instance.Pack.Import.Progress.Content"]
                    });

                    try
                    {
                        await Task.Run(() => _packManager?.AddRangePacks(filePaths));
                        await RefreshPacksAsync();
                    }
                    finally
                    {
                        DialogHost.Close();
                    }
                }
            });
            
            body.Import(filePaths);
        }
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text ?? string.Empty;
        _ = RefreshPacksAsync();
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsEdit || TypeSel?.SelectedItem is not ListBoxItem item) return;

        var newType = item.Tag?.ToString()?.ToLower() ?? "resource";
        if (_currentType != newType)
        {
            _currentType = newType;
            _ = RefreshPacksAsync();
        }
    }

    private void FolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!IsEdit || TypeSel?.SelectedItem is not ListBoxItem item) return;

        var typeTag = item.Tag?.ToString()?.ToLower() ?? "resource";
        var folderType = typeTag switch
        {
            "resource" => InstanceFolderType.ResourcePackFolder,
            "behavior" => InstanceFolderType.BehaviorPackFolder,
            "skin" => InstanceFolderType.SkinPackFolder,
            "template" => InstanceFolderType.WorldTemplateFolder,
            _ => InstanceFolderType.UserFolder
        };

        OpenFolderHelper.Open(IsolationCore.GetInstanceFolderPath(VersionInfo, folderType));
    }
}