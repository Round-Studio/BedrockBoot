using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Views.Control.Items;

namespace BedrockBoot.Views.Pages.InstanceSubPage.LevelSettings;

public partial class LevelSettingsPack : UserControl
{
    private readonly ArchiveInfo? _info;
    public ArchivePackManager Manager { get; private set; }
    public ResourcePackType SelectType => (ResourcePackType)TypeSelBox.SelectedIndex;
    public string SearchKey => SearchBox.Text;

    public LevelSettingsPack()
    {
        InitializeComponent();
    }
    
    public LevelSettingsPack(ArchiveInfo info):this()
    {
        _info = info;
        Manager = new(_info);
        
        UpdateUi();
    }

    public void UpdateUi()
    {
        Manager.Refresh();

        var searchKey = string.IsNullOrEmpty(SearchKey) ? "" : SearchKey;

        var unAct = Manager.GetUnActivatedPacks(SelectType)
            .Where(x => x.Header.Name.Contains(searchKey) || x.Header.Description.Contains(searchKey))
            .ToList();

        var act = Manager.GetActivatedPacks(SelectType)
            .Where(x => x.Header.Name.Contains(searchKey) || x.Header.Description.Contains(searchKey))
            .ToList();

        ActivatedViewer.Children.Clear();
        UnActivatedViewer.Children.Clear();

        act.ForEach(x =>
        {
            ActivatedViewer.Children.Add(new GameArchivePackItem(x, true)
            {
                ActiveAction = (manifest) =>
                {
                    Manager.UninstallPack(manifest);
                    UpdateUi();
                }
            });
        });

        unAct.ForEach(x =>
        {
            UnActivatedViewer.Children.Add(new GameArchivePackItem(x, false)
            {
                ActiveAction = (manifest) =>
                {
                    Manager.InstallPack(manifest);
                    UpdateUi();
                }
            });
        });
    }

    private void TypeSelBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TypeSelBox != null)
        {
            UpdateUi();
        }
    }

    private void RefreshBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        UpdateUi();
    }

    private readonly Models.Helper.UiDebouncer _searchDebouncer = new();

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (SearchBox != null)
        {
            // UpdateUi 会重建资源包列表，逐字符触发时做防抖
            _searchDebouncer.Debounce(() => UpdateUi());
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _searchDebouncer.Dispose();
    }
}