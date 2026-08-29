using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Interface;
using BedrockBoot.Models.Pack.Game.Loaders;
using BedrockBoot.Views.Control.Items.Instance;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceLoaders : UserControl
{
    private readonly VersionConfig _versionInfo;

    public InstanceLoaders()
    {
        InitializeComponent();
    }

    public InstanceLoaders(VersionConfig versionInfo) : this()
    {
        _versionInfo = versionInfo;
        UpdateUi();
    }

    public void UpdateUi()
    {
        LoadersList.Children.Clear();
        if (LoadersManager.ModsLoaders != null)
        {
            foreach (var loaderType in LoadersManager.ModsLoaders)
            {
                if (typeof(IModsLoader).IsAssignableFrom(loaderType))
                {
                    var instance = (IModsLoader)Activator.CreateInstance(loaderType);
                    instance.OnUpdate = () => UpdateUi();
                    LoadersList.Children.Add(new ModLoaderItem(_versionInfo, instance));
                }
            }
        }
    }
}