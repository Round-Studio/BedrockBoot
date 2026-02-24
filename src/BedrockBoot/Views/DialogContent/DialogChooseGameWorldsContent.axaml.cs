using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
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
        var allWorlds = worlds.SelectMany(w => w).ToList();
        
        allWorlds.ForEach(w =>
        {
            WorldsList.Items.Add(new ListBoxItem()
            {
                Content = new TextBlock()
                {
                    Text = w.Name,
                    Margin = new Thickness(10,4)
                }
            });
        });
    }
}