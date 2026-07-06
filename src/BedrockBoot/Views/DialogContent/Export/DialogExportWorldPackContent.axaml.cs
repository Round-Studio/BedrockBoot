using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Base.Enum.Type.Export;

namespace BedrockBoot.Views.DialogContent.Export;

public partial class DialogExportWorldPackContent : UserControl
{
    public ArchiveExportType ArchiveExportType => (ArchiveExportType)ExportType.SelectedIndex;
    public string ArchiveName => NameInputBox.Text;
    public string ArchiveDescription => DescriptionInputBox.Text;
    public string ArchiveVersion => VersionInputBox.Text;
    public bool ArchiveAllowRandomSeed => AllowRandomSeed.IsChecked ?? false;
    public bool ArchiveLockTemplateOptions => LockTemplateOptions.IsChecked ?? false;
    private readonly ArchiveInfo _info;

    public DialogExportWorldPackContent()
    {
        InitializeComponent();
    }

    public DialogExportWorldPackContent(ArchiveInfo info) : this()
    {
        _info = info;
        NameInputBox.Text = info.Name;
    }

    private void ExportType_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ExportType == null) return;
        TemplateItems.IsVisible = ExportType.SelectedIndex == 1;
    }
}