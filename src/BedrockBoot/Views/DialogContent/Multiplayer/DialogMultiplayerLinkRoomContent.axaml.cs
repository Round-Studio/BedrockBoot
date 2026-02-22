using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BedrockBoot.Views.DialogContent.Multiplayer;

public partial class DialogMultiplayerLinkRoomContent : UserControl
{
    public string RoomCode => RoomCodeInput.Text;
    public DialogMultiplayerLinkRoomContent()
    {
        InitializeComponent();
    }
}