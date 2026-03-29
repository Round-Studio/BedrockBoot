using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogCreateBackupContent : UserControl
{
    public string BackupNameInfo => string.IsNullOrEmpty(BackupName.Text) ? "新建备份" : BackupName.Text;
    public DialogCreateBackupContent()
    {
        InitializeComponent();
    }
}