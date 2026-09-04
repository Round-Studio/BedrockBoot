using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Core.Models.Pack.Game.Mods;
using BedrockBoot.Interface;
using BedrockBoot.Interface.ModLoader;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Loaders;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.Control.Items.Instance;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Path = System.IO.Path;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceMods : ISetting
{
    public InstanceMods()
    {
        IsEdit = false;
        InitializeComponent();
    }

    public InstanceMods(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;

        UpdateModsLoader();
        UpdateUI();
    }

    private static I18nManager i18n => I18nManager.Instance;

    public VersionConfig VersionInfo { get; set; }
    private string SearchKey => SearchBox.Text ?? string.Empty;
    private List<IModsLoader> InstalledModsLoader = new();

    private void UpdateModsLoader()
    {
        IsEdit = false;
        ModsLoaderSelect.Items.Clear();
        InstalledModsLoader.Clear();
        foreach (var loaderType in LoadersManager.ModsLoaders)
        {
            if (typeof(IModsLoader).IsAssignableFrom(loaderType))
            {
                var instance = (IModsLoader)Activator.CreateInstance(loaderType);
                instance.OnUpdate = () => UpdateUI();
                instance.InitLoader(VersionInfo);
                if (instance.IsInstalled())
                {
                    InstalledModsLoader.Add(instance);
                    ModsLoaderSelect.Items.Add(new ComboBoxItem()
                    {
                        Content = instance.LoaderName
                    });
                }
            }
        }

        if (VersionInfo.Config.ModsLoaderSelectIndex > InstalledModsLoader.Count - 1)
            VersionInfo.Config.ModsLoaderSelectIndex = 0;
        ModsLoaderSelect.SelectedIndex = VersionInfo.Config.ModsLoaderSelectIndex;
        IsEdit = true;
    }

    private void UpdateUI()
    {
        IsEdit = false;
        NullBox.IsVisible = false;
        ResultBox.Children.Clear();

        var mods = InstalledModsLoader[ModsLoaderSelect.SelectedIndex].ModsManager.GetAllMods();
        var resultMods = new List<ModItemInfo>();

        foreach (var info in mods)
            if (string.IsNullOrEmpty(SearchKey) ||
                info.ModPath.Contains(SearchKey, StringComparison.OrdinalIgnoreCase) ||
                info.ModName.Contains(SearchKey, StringComparison.OrdinalIgnoreCase))
                resultMods.Add(info);

        if (resultMods.Count <= 0)
            NullBox.IsVisible = true;
        else
            foreach (var info in resultMods)
                ResultBox.Children.Add(new GameModItem(info, InstalledModsLoader[ModsLoaderSelect.SelectedIndex])
                {
                    UpdateCallBack = UpdateUI
                });

        IsEdit = true;
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (IsEdit)
            UpdateUI();
    }

    private void FolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var modPath = InstalledModsLoader[ModsLoaderSelect.SelectedIndex].ModsFolder;
        if (!Directory.Exists(modPath)) Directory.CreateDirectory(modPath);
        OpenFolderHelper.Open(modPath);
    }

    private void ImportModBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        InstalledModsLoader[ModsLoaderSelect.SelectedIndex].ModsManager.AddMod();
        UpdateUI();
    }

    private void ModsLoaderSelect_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            VersionInfo.Config.ModsLoaderSelectIndex = ModsLoaderSelect.SelectedIndex;
            GameInfoHelper.SaveVersionConfig(VersionInfo);
            UpdateUI();
        }
    }
}