using Avalonia.Controls;
using PaperConnect.Core.Entry;

namespace BedrockBoot.Views.Control.Items.Multiplayer;

public partial class PlayerItem : UserControl
{
    public PlayerItem()
    {
        InitializeComponent();
    }

    public PlayerItem(AgreementEntry.PlayerEntry info) : this()
    {
        PlayerName.Text = info.PlayerName;
        HostBox.IsVisible = info.IsRoomHost;

        Card.Description = info.ClientId;
    }
}