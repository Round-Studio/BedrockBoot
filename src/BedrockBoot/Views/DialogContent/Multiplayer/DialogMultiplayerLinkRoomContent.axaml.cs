using Avalonia.Controls;

namespace BedrockBoot.Views.DialogContent.Multiplayer;

public partial class DialogMultiplayerLinkRoomContent : UserControl
{
    public DialogMultiplayerLinkRoomContent()
    {
        InitializeComponent();
    }

    public string RoomCode => RoomCodeInput.Text;
}