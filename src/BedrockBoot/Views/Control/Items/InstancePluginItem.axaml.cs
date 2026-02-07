using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using BedrockBoot.Core.Interface.Instance;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Views.Control.Items;

public partial class InstancePluginItem : UserControl
{
    public InstancePluginItem()
    {
        InitializeComponent();
    }
    
    public IInstancePlugin InstancePlugin { get; set; }

    public InstancePluginItem(IInstancePlugin plugin) : this()
    {
        InstancePlugin = plugin;
        UpdateUI();
    }

    public async Task UpdateUI()
    {
        if (InstancePlugin.IsInstalled())
        {
            InstalledBox.Background = Brushes.Orange;
            InstalledBox.Text = "已安装";
        };
        CardDescription.Text = InstancePlugin.Description;
        CardHeader.Text = InstancePlugin.Name;

        if (!string.IsNullOrEmpty(InstancePlugin.Icon))
        {
            Card.ImageIcon = await ImageLoader.LoadIconAsync(InstancePlugin.Icon);
            Card.IsFontIcon = false;
        }
    }

    private void Card_OnClick(object? sender, RoutedEventArgs e)
    {
        InstancePlugin.Install();
    }
}