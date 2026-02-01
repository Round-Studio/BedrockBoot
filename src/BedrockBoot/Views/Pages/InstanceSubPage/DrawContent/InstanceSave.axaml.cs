using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Views.Control.Items;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceSave : UserControl
{
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

    public bool IsEdit { get; set; }
    public VersionConfig VersionInfo { get; set; }
    public ArchiveManifest ArchiveManifest { get; private set; }
    private string SearchKey => SearchBox.Text;
    private int SelIndex => UserChooseBox.SelectedIndex;

    private void UpdateUI()
    {
        IsEdit = false;
        var body = new ArchiveCheck(VersionInfo);
        ArchiveManifest = body.Check();

        ArchiveManifest.Manifest.ToList().ForEach(user =>
        {
            UserChooseBox.Items.Add(new ComboBoxItem
            {
                Content = user.Key,
                Tag = user.Value
            });
        });

        if (ArchiveManifest.Manifest.Count > 0)
        {
            UserChooseBox.SelectedIndex = 0;

            UpdateSaves(ArchiveManifest.Manifest.Values.ToList()[0]);
        }

        IsEdit = true;
    }

    public void UpdateSaves(List<ArchiveInfo> saves)
    {
        NullBox.IsVisible = saves.Count <= 0;

        SavesBox.Children.Clear();
        saves.ForEach(save => { SavesBox.Children.Add(new ArchiveItem(save)); });
    }

    public void UpdateSearch()
    {
        var lst = ArchiveManifest.Manifest.Values.ToList()[SelIndex];
        var result = new List<ArchiveInfo>();
        if (!string.IsNullOrEmpty(SearchKey))
        {
            lst.ForEach(save =>
            {
                if (save.Name.Contains(SearchKey))
                    result.Add(save);
            });

            UpdateSaves(result);
        }
        else
        {
            UpdateSaves(lst);
        }
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        UpdateSearch();
    }

    private void UserChooseBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateSearch();
    }
}