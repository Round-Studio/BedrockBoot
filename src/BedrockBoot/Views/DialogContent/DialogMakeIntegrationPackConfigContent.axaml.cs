using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game.Pack.Integration;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogMakeIntegrationPackConfigContent : UserControl
{
    public PackInfo PackConfig => new()
    {
        Authors = new()
        {
            new()
            {
                Name = string.IsNullOrEmpty(PackAuthorName.Text) ? "Creator" : PackAuthorName.Text,
                Links = new() { string.IsNullOrEmpty(PackAuthorLink.Text) ? "" : PackAuthorLink.Text }
            }
        },
        Name = string.IsNullOrEmpty(PackName.Text) ? "My Pack" : PackName.Text,
        Version = string.IsNullOrEmpty(PackVersion.Text) ? "0.0.0.1" : PackVersion.Text,
        Description = string.IsNullOrEmpty(PackDescription.Text) ? "My Pack Description " : PackDescription.Text,
        EnableConfig = new PackEnableConfig()
        {
            IsEnableArchive = (bool)EnableArchive.IsChecked!,
            IsEnableBehaviorPack = (bool)EnableBehaviorPack.IsChecked!,
            IsEnableDllFile = (bool)EnableDllMods.IsChecked!,
            IsEnableResourcePack = (bool)EnableResourcePack.IsChecked!
        },
        PackIconFile = (string.IsNullOrEmpty(PackIcon.Text) ? string.Empty : PackName.Text)!
    };
    public DialogMakeIntegrationPackConfigContent()
    {
        InitializeComponent();
    }
}