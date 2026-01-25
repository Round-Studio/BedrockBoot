using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Views.Control.Items;

namespace BedrockBoot.Views.Pages.DownloadSubPage;

public partial class DownloadAssetsResultPage : UserControl
{
    public DownloadAssetsResultPage(List<CurseForgeResponse.ModData> mods)
    {
        InitializeComponent();

        Task.Run(() =>
        {
            mods.ForEach(i =>
            {
                Dispatcher.UIThread.Invoke(() => ItemsPanel.Children.Add(new CurseForgeModItem(i)));
            });
        });
    }
}