using Avalonia.Controls;
using BedrockBoot.GravityCone.Entry.Result;
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

    public PlayerItem(PlayerInfo info) : this()
    {
        PlayerName.Text = info.Player;
        HostBox.IsVisible = info.IsRoomHost;

        Card.Description = info.ClientId;
    }
}