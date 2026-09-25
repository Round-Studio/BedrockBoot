using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogSettingResourcePackContent : UserControl
{
    private readonly ResourcePackManifest _manifest;

    public DialogSettingResourcePackContent()
    {
        InitializeComponent();
    }

    public DialogSettingResourcePackContent(ResourcePackManifest manifest) : this()
    {
        _manifest = manifest;
        UpdateUi();
    }

    public void UpdateUi()
    {
        PackName.Text = _manifest.Header.Name;
        PackDescription.Text = _manifest.Header.Description;
        AllowAchievement.IsChecked = _manifest.Metadata.ProductType == "addon";
    }

    public void OnSave()
    {
        _manifest.Header.Name = PackName.Text!;
        _manifest.Header.Description = PackDescription.Text!;
        _manifest.Metadata.ProductType = (bool)AllowAchievement.IsChecked! ? "addon" : "";
        _manifest.SaveConfig();
    }
}