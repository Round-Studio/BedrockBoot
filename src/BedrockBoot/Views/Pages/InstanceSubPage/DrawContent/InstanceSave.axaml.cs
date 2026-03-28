using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Navigation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.Pages.InstanceSubPage.DrawContent.ContentView;
using BedrockBoot.Views.Pages.InstanceSubPage.LevelEditor;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceSave : UserControl
{
    public VersionConfig VersionInfo { get; set; }
    public bool IsEdit { get; set; }

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

    private void UpdateUI()
    {
        OnNavigatedTo(true);
    }

    public void OnNavigatedTo(bool isSavesView, object page = null)
    {
        if (isSavesView)
        {
            NavigationFrame.NavigateTo(new SavesView(VersionInfo)
            {
                EditAction = (info => OnNavigatedTo(false, new LevelEditorRoot(info)
                {
                    BackAction = () =>
                        OnNavigatedTo(true)
                }))
            });
        }
        else
        {
            NavigationFrame.NavigateTo((UserControl)page);
        }
    }
}