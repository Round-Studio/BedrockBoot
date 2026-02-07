using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Interface.Instance;
using BedrockBoot.Plugin.Instance;
using BedrockBoot.Views.Control.Items;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstancePlugins : UserControl
{
    public VersionConfig VersionConfig { get; set; }
    public List<IInstancePlugin> Plugins { get; } = new()
    {
        new PluginLeviLamina()
    };
    public InstancePlugins(VersionConfig versionConfig)
    {
        VersionConfig = versionConfig;
        InitializeComponent();
        UpdateUI();
    }

    public void UpdateUI()
    {
        Plugins.ForEach(plugin =>
        {
            plugin.Init(VersionConfig);
            ResultBox.Children.Add(new InstancePluginItem(plugin));
        });
    }
}