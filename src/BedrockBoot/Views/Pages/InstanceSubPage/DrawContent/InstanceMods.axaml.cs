using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Interface;
using BedrockBoot.Models.Pack.Game.Mods;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceMods : ISetting
{
    public VersionConfig VersionInfo { get; set; }
    public ModsManager ModsManager { get; set; }
    public InstanceMods()
    {
        IsEdit = false;
        
        InitializeComponent();
    }

    public InstanceMods(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
        ModsManager = new(VersionInfo);
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        ModsManager.RefreshMods();
    }
}