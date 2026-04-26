using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Proton.Enum;

namespace BedrockBoot.Views.DialogContent.Linux.Proton;

public partial class DialogChooseDownloadBranchContent : UserControl
{
    public ProtonSource SelSource => (ProtonSource)SelBox.SelectedIndex;
    public DialogChooseDownloadBranchContent()
    {
        InitializeComponent();
    }
}