using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;
using BedrockBoot.Views.TaskItem;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation.LeftSelectBar;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawInstanceContent : UserControl
{
    public DrawInstanceContent()
    {
        InitializeComponent();

        IsEditMode = true;

#if RELEASE
        GameControls.IsEnabled = BedrockBoot.Models.Global.GlobalModel.FunctionOption.IsEnableGameInstanceControl;
#endif

#if LINUX
        Mods.IsVisible = false;
        Plugin.IsVisible = false;
#endif
    }

    public DrawInstanceContent(VersionConfig info) : this()
    {
        VersionInfo = info;

        Update();
    }

    public VersionConfig VersionInfo { get; set; }
    public bool IsEditMode { get; set; }

    public void Update()
    {
        IsEditMode = false;
        InstanceFrame.NavigateTo(new InstanceInfo(VersionInfo));

        IsEditMode = true;
    }

    private void InstanceTabControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            var tag = ((LeftSelectBarItem)InstanceTabControl.SelectedItem).Tag.ToString();

            switch (tag)
            {
                case "Info":
                    InstanceFrame.NavigateTo(new InstanceInfo(VersionInfo));
                    break;
                case "Mods":
                    InstanceFrame.NavigateTo(new InstanceMods(VersionInfo));
                    break;
                case "Pack":
                    InstanceFrame.NavigateTo(new InstancePack(VersionInfo));
                    break;
                case "Save":
                    InstanceFrame.NavigateTo(new InstanceSave(VersionInfo));
                    break;
                case "Screenshots":
                    InstanceFrame.NavigateTo(new InstanceScreenshots(VersionInfo));
                    break;
                case "Server":
                    InstanceFrame.NavigateTo(new InstanceServer(VersionInfo));
                    break;
                case "Plugin":
                    InstanceFrame.NavigateTo(new InstancePlugins(VersionInfo));
                    break;
                case "Controls":
                    InstanceFrame.NavigateTo(new InstanceControls(VersionInfo));
                    break;
            }
        }
    }
}