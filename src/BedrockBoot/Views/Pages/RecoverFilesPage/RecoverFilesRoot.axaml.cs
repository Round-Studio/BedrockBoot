using Avalonia.Controls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation;

namespace BedrockBoot.Views.Pages.RecoverFilesPage;

public partial class RecoverFilesRoot : UserControl
{
    public RecoverFilesRoot()
    {
        InitializeComponent();
        NavigationFrameStatic = NavigationFrame;
        NavigationFrameStatic.NavigateTo(new RecoverFilesWellcom());
    }

    public static NavigationFrame NavigationFrameStatic { get; set; }
}