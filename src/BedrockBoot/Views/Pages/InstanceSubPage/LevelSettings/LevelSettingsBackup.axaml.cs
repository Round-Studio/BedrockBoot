using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game.Pack.Archive;

namespace BedrockBoot.Views.Pages.InstanceSubPage.LevelSettings;

public partial class LevelSettingsBackup : UserControl
{
    private ArchiveInfo _info;
    public LevelSettingsBackup() => InitializeComponent();
    public LevelSettingsBackup(ArchiveInfo info):this()
    {
        _info = info;
    }
}