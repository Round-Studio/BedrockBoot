using Avalonia.Controls;
using BedrockBoot.Base.Entry.Game.Pack.Server;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogAddGameServerContent : UserControl
{
    public ServerItemInfo ServerItemInfo => new()
    {
        ServerName = !string.IsNullOrEmpty(ServerNameInputBox.Text) ? ServerNameInputBox.Text! : "第三方服务器",
        ServerAddress = ServerAddressInputBox.Text!,
        ServerPort = !string.IsNullOrEmpty(ServerPortInputBox.Text) ? int.Parse(ServerPortInputBox.Text!) : 19132,
        VersionConfig = null
    };
    public DialogAddGameServerContent()
    {
        InitializeComponent();
    }
}