using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Views.Control;

namespace BedrockBoot.Views.Pages.DownloadSubPage;

public partial class DownloadAssetsPage : UserControl
{
    public string Key => TextBox.Text!;

    public DownloadAssetsPage()
    {
        InitializeComponent();
        Search();
    }

    public void Search()
    {
        var key = Key;
        ScrollViewer.IsVisible = false;
        ItemsPanel.Children.Clear();
        NoneBox.IsVisible = false;
        LoadingRing.IsVisible = true;
        Task.Run(() =>
        {
            var items = new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey).SearchModsAsync(key, pageSize: 50).Result;

            if (items.Data.Count > 0)
                items.Data.ForEach(i =>
                {
                    Dispatcher.UIThread.Invoke(() => { ItemsPanel.Children.Add(new CurseForgeModItem(i)); });
                });
            else
                Dispatcher.UIThread.Invoke(() => NoneBox.IsVisible = true);

            Dispatcher.UIThread.Invoke(() => LoadingRing.IsVisible = false);
            Dispatcher.UIThread.Invoke(() => ScrollViewer.IsVisible = true);
        });
    }

    private void SearchBtn_OnClick(object? sender, RoutedEventArgs e) => Search();
}