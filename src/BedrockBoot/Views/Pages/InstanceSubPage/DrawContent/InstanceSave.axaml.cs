using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Views.Control;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceSave : UserControl
{
    public bool IsEdit { get; set; } = false;
    public VersionConfig VersionInfo { get; set; }
    public ArchiveManifest ArchiveManifest { get; private set; }
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
        IsEdit = false;
        var body = new ArchiveCheck(VersionInfo);
        ArchiveManifest = body.Check();
        
        ArchiveManifest.Manifest.ToList().ForEach(user =>
        {
            UserChooseBox.Items.Add(new ComboBoxItem()
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
        SavesBox.Children.Clear();
        saves.ForEach(save =>
        {
            SavesBox.Children.Add(new ArchiveItem(save));
        });
    }
}