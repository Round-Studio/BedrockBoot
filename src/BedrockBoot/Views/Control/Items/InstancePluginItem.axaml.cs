using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using BedrockBoot.Core.Interface.Instance;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Views.Control.Items;

public partial class InstancePluginItem : UserControl
{
	private ImageLoader _imageLoader = new ImageLoader();
    public InstancePluginItem()
    {
        InitializeComponent();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
	    base.OnUnloaded(e);
	    _imageLoader.Dispose();
    }

    public InstancePluginItem(IInstancePlugin plugin) : this()
    {
        InstancePlugin = plugin;
        _ = UpdateUIAsync();
    }

    private static I18nManager i18n => I18nManager.Instance;
    public IInstancePlugin? InstancePlugin { get; set; }

    public async Task UpdateUIAsync()
    {
        if (InstancePlugin == null) return;

        // 状态显示
        if (InstancePlugin.IsInstalled())
        {
            InstalledBox.Background = Brushes.Orange;
            InstalledBox.Text = i18n["Instance.Plugin.Status.Installed"];
        }
        else
        {
            InstalledBox.IsVisible = false;
        }

        CardHeader.Text = InstancePlugin.Name;
        CardDescription.Text = InstancePlugin.Description;

        // 图标异步加载
        if (!string.IsNullOrEmpty(InstancePlugin.Icon))
            try
            {
                var icon = await _imageLoader.LoadIconAsync(InstancePlugin.Icon);
                if (icon != null)
                {
                    Card.IsFontIcon = false;
                    Card.ImageIcon = icon;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Failed to load plugin icon: {ex.Message}");
            }
    }

    private void Card_OnClick(object? sender, RoutedEventArgs e)
    {
        InstancePlugin?.Install();
    }
}