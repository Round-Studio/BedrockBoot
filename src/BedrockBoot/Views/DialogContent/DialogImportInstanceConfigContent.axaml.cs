using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game.Pack.Isolation;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogImportInstanceConfigContent : UserControl
{
    public MigrationConfig MigrationConfig => new()
    {
        IsEnableResourcePack = (bool)IsEnableResourcePack.IsChecked,
        IsEnableBehaviorPack = (bool)IsEnableBehaviorPack.IsChecked,
        IsEnableArchive = (bool)IsEnableArchivePack.IsChecked,
        IsEnableConfig = (bool)IsEnableConfigPack.IsChecked
    };
    public DialogImportInstanceConfigContent()
    {
        InitializeComponent();
    }
}