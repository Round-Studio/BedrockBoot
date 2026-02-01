using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Views.Control.Items;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogImportResourcePackContent : UserControl
{
    public DialogImportResourcePackContent()
    {
        InitializeComponent();
    }

    public void Import(List<string> files)
    {
        Task.Run(() =>
        {
            files.ForEach(file =>
            {
                new ResourcePackAnalysis(file).GetPackManifests().ForEach(conf =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        PacksList.Children.Add(new GameResourcePackItem(conf, true));
                        LoadingRing.IsVisible = false;
                    });
                });
            });
        });
    }
}