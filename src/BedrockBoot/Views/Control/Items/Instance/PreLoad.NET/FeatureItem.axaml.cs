using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Standard.Entity.Game.Pack.Loader.PreLoad.NET;

namespace BedrockBoot.Views.Control.Items.Instance.PreLoad.NET;

public partial class FeatureItem : UserControl
{
    private readonly FeatureXml.FeatureDoc _feature;
    private readonly bool _isEnable;

    public FeatureItem()
    {
        InitializeComponent();
    }

    public FeatureItem(FeatureXml.FeatureDoc feature, bool isEnable) : this()
    {
        _feature = feature;
        _isEnable = isEnable;
        FeatureDesc.Text = feature.Description;
        FeatureKey.Text = feature.Name;
        FeatureTitle.Text = feature.Title;
        ConfigToggleSwitch.IsChecked = isEnable;
    }

    public (string, bool) GetEnable()
    {
        return ((string, bool))(_feature.Name, ConfigToggleSwitch.IsChecked!);
    }
}