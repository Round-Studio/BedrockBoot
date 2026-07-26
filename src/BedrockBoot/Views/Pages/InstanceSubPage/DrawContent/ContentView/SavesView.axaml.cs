using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Views.Control.Items;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent.ContentView;

public partial class SavesView : UserControl
{
    public SavesView()
    {
        IsEdit = false;
        InitializeComponent();
    }

    public SavesView(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
        UpdateUI();
    }

    private static I18nManager i18n => I18nManager.Instance;
    public VersionConfig VersionInfo { get; set; }
    public ArchiveManifest? ArchiveManifest { get; private set; }
    public bool IsEdit { get; set; }
    public Action<ArchiveInfo>? EditAction { get; set; }

    private string SearchKey => SearchBox.Text ?? string.Empty;
    private int SelIndex => UserChooseBox.SelectedIndex;
    private string CurrentUser => (UserChooseBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

    private void UpdateUI()
    {
        IsEdit = false;

        var checker = new ArchiveCheck(VersionInfo);
        ArchiveManifest = checker.Check();

        UserChooseBox.Items.Clear();

        if (ArchiveManifest?.Manifest != null)
        {
            foreach (var user in ArchiveManifest.Manifest)
                UserChooseBox.Items.Add(new ComboBoxItem
                {
                    Content = user.Key,
                    Tag = user.Value
                });

            if (ArchiveManifest.Manifest.Count > 0)
            {
                UserChooseBox.SelectedIndex = 0;
                UpdateContent();
            }
        }
        else
        {
            UpdateSaves(new List<ArchiveInfo>());
        }

        IsEdit = true;
    }

    public void UpdateSaves(List<ArchiveInfo> saves)
    {
        SavesBox.Children.Clear();
        NullBox.IsVisible = saves.Count <= 0;

        foreach (var save in saves)
            SavesBox.Children.Add(new ArchiveItem(save)
            {
                EditAction = () =>
                    EditAction?.Invoke(save),
                RefreshCallBack = UpdateUI
            });
    }

    public void UpdateTemplates(List<ResourcePackManifest> resPacks)
    {
        SavesBox.Children.Clear();
        NullBox.IsVisible = resPacks.Count <= 0;

        foreach (var save in resPacks)
            SavesBox.Children.Add(new GameResourcePackItem(save)
            {
                RefreshCallBack = UpdateUI
            });
    }

    private void UpdateContent()
    {
        if (TypeSel.SelectedIndex == 0)
        {
            if (ArchiveManifest?.Manifest == null || ArchiveManifest.Manifest.Count == 0)
            {
                UpdateSaves(new List<ArchiveInfo>());
                return;
            }

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
        else if (TypeSel.SelectedIndex == 1)
        {
            if (string.IsNullOrEmpty(CurrentUser))
            {
                UpdateTemplates(new List<ResourcePackManifest>());
                return;
            }

            var resManager = new ResourcePackManager(VersionInfo);
            var templates = resManager.GetAllPack(CurrentUser)
                .Where(x => x.PackType == ResourcePackType.WorldTemplate)
                .ToList();

            if (!string.IsNullOrEmpty(SearchKey))
            {
                var filtered = templates
                    .Where(t => t.Header.Name.Contains(SearchKey, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                UpdateTemplates(filtered);
            }
            else
            {
                UpdateTemplates(templates);
            }
        }
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (IsEdit) UpdateContent();
    }

    private void UserChooseBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit) UpdateContent();
    }

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
                    Patterns = new[] { "*.mcworld", "*.mctemplate" }
                }
            }
        });

        if (files is { Count: >= 1 })
        {
            var path = files[0].Path.LocalPath;
            if (string.IsNullOrEmpty(path)) return;

            DialogHost.Show(new DialogInfo
            {
                Title = i18n["Instance.Pack.Import.Progress.Title"],
                Content = "正在导入包..."
            });

            var curUser = CurrentUser;
            
            Task.Run(() =>
            {
                if (path.ToLower().EndsWith(".mcworld", StringComparison.OrdinalIgnoreCase))
                {
                    var importer = new ArchiveCheck(VersionInfo);
                    importer.ImportWorldPack(path);
                }
                else if (path.ToLower().EndsWith(".mctemplate", StringComparison.OrdinalIgnoreCase))
                {
                    var importer = new ResourcePackManager(VersionInfo);
                    importer.AddRangePacks(new() { path }, curUser);
                }

                DialogHost.Close();
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                {
                    UpdateUI();
                });
            });

            UpdateUI();
        }
    }

    private void TypeSel_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TypeSel == null) return;
        UpdateContent();
    }
}