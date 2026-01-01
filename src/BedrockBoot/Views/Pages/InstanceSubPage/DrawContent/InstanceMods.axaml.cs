using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Shapes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using BedrockBoot.Interface;
using BedrockBoot.Models.Pack.Game.Mods;
using BedrockBoot.Views.Control;
using Path = System.IO.Path;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceMods : ISetting
{
    public VersionConfig VersionInfo { get; set; }
    public ModsManager ModsManager { get; set; }
    private string _searchKey => SearchBox.Text;
    public InstanceMods()
    {
        IsEdit = false;
        
        InitializeComponent();
    }

    public InstanceMods(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
        ModsManager = new(VersionInfo);
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        IsEdit = false;
        NullBox.IsVisible = false;
        ResultBox.Children.Clear();
        var mods = ModsManager.RefreshMods();
        var resultMods = new List<ModInfo>();

        mods.ForEach(info =>
        {
            if (string.IsNullOrEmpty(_searchKey) ||
                info.File.Contains(_searchKey))
            {
                resultMods.Add(info);
            }
        });

        if (resultMods.Count <= 0)
        {
            NullBox.IsVisible = true;
        }
        else
        {
            resultMods.ForEach(info =>
            {
                ResultBox.Children.Add(new GameModItem(info));
            });
        }

        IsEdit = true;
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (IsEdit)
            UpdateUI();
    }

    private void FolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start("explorer", new[] { Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "mods") });
    }
}