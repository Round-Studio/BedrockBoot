using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game.Pack.Archive;

namespace BedrockBoot.Views.Pages.InstanceSubPage.LevelSettings;

public partial class LevelSettingsControls : UserControl
{
    public LevelSettingsControls()
    {
        InitializeComponent();
    }

    public LevelSettingsControls(ArchiveInfo info) : this()
    {

    }
}