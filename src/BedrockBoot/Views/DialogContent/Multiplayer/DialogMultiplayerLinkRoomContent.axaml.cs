using Avalonia.Controls;

namespace BedrockBoot.Views.DialogContent.Multiplayer;

public partial class DialogMultiplayerLinkRoomContent : UserControl
{
    public DialogMultiplayerLinkRoomContent()
    {
        InitializeComponent();
    }

    /// <summary>联机码。Trim 处理粘贴时携带的空白字符；输入框为空时返回空字符串而非 null。</summary>
    public string RoomCode => RoomCodeInput.Text?.Trim() ?? string.Empty;
}