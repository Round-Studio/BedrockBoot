using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Pack.Xbox.Cape;
using BedrockBoot.Views.Control.Items.Xbox.Capes;

namespace BedrockBoot.Views.Pages.XboxSubPage.DrawContent;

public partial class XboxCapes : UserControl
{
    private readonly string _xbl;

    public XboxCapes()
    {
        InitializeComponent();
    }

    public XboxCapes(string xbl) : this()
    {
        _xbl = xbl;
        MainContent.IsVisible = false;
        Task.Run(async () =>
        {
            var capes = await CapeApiClient.GetPlayerCapesAsync(_xbl);
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                LoadingCard.IsVisible = false;
                MainContent.IsVisible = true;

                ListPanel.Children.Clear();
                capes.Capes.ForEach(cape => { ListPanel.Children.Add(new CapeItem(cape)); });
            });
        });
    }
}