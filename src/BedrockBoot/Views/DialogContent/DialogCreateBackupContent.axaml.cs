using Avalonia.Controls;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogCreateBackupContent : UserControl
{
    public DialogCreateBackupContent()
    {
        InitializeComponent();
    }

    public string BackupNameInfo => string.IsNullOrEmpty(BackupName.Text) ? "新建备份" : BackupName.Text;
}