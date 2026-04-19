using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Models.Pack.Game.Archive;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogChooseGameWorldsContent : UserControl
{
    public DialogChooseGameWorldsContent()
    {
        InitializeComponent();
    }

    public DialogChooseGameWorldsContent(VersionConfig versionConfig) : this()
    {
        var worlds = new ArchiveCheck(versionConfig).Check().Manifest.Values.ToList();
        ArchiveInfos = worlds.SelectMany(w => w).ToList();

        ArchiveInfos.ForEach(w =>
        {
            WorldsList.Items.Add(new ListBoxItem
            {
                Content = new TextBlock
                {
                    Text = w.Name,
                    Margin = new Thickness(0, 0)
                }
            });
        });
    }

    public List<ArchiveInfo> ArchiveInfos { get; }

    public ArchiveInfo? SelectedArchiveInfo =>
        ArchiveInfos.Count == 0 ? null : ArchiveInfos[WorldsList.SelectedIndex];
}