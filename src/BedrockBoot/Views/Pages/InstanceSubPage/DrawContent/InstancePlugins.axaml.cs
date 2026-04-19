using System.Collections.Generic;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Interface.Instance;
using BedrockBoot.Plugin.Instance;
using BedrockBoot.Views.Control.Items;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstancePlugins : UserControl
{
    public InstancePlugins(VersionConfig versionConfig)
    {
        VersionConfig = versionConfig;
        InitializeComponent();
        UpdateUI();
    }

    public VersionConfig VersionConfig { get; set; }

    public List<IInstancePlugin> Plugins { get; } = new()
    {
        new PluginLeviLamina()
    };

    public void UpdateUI()
    {
        Plugins.ForEach(plugin =>
        {
            plugin.Init(VersionConfig);
            ResultBox.Children.Add(new InstancePluginItem(plugin));
        });
    }
}