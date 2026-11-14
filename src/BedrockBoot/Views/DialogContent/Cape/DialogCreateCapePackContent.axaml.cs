using Avalonia.Controls;

namespace BedrockBoot.Views.DialogContent.Cape;

public partial class DialogCreateCapePackContent : UserControl
{
    public string PackNameStr => PackName.Text?.Trim() ?? string.Empty;
    public string AuthorStr => Author.Text?.Trim() ?? string.Empty;

    public DialogCreateCapePackContent()
    {
        InitializeComponent();
    }
}
