using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;
using BedrockBoot.Views.TaskItem;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawInstanceContent : UserControl
{
    public VersionConfig VersionInfo { get; set; }
    public bool IsEditMode { get; set; } = false;

    public DrawInstanceContent()
    {
        InitializeComponent();

        IsEditMode = true;

#if RELEASE
        GameControls.IsEnabled = GlobalModel.FunctionOption.IsEnableGameInstanceControl;
        GameModes.IsEnabled = GlobalModel.FunctionOption.IsEnableGameInstanceMods;
#endif
    }

    public DrawInstanceContent(VersionConfig info) : this()
    {
        VersionInfo = info;
        
        Update();
    }

    public void Update()
    {
        IsEditMode = false;
        
        InstanceFrame.NavigateTo(new InstanceInfo(VersionInfo));
        VersionName.Text = VersionInfo.Info.VersionName;
        VersionReady.Text = $"{VersionInfo.Info.Version} · {VersionInfo.Info.VersionType} · {VersionInfo.Info.BuildType}";
        
        IsEditMode = true;
    }

    private void InstanceTabControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            var tag = ((TabItem)(InstanceTabControl.SelectedItem)).Tag.ToString();

            switch (tag)
            {
                case "Info":
                    InstanceFrame.NavigateTo(new InstanceInfo(VersionInfo));
                    break;
                case "Mods":
                    InstanceFrame.NavigateTo(new InstanceMods());
                    break;
                case "Pack":
                    InstanceFrame.NavigateTo(new InstancePack());
                    break;
                case "Save":
                    InstanceFrame.NavigateTo(new InstanceSave(VersionInfo));
                    break;
                case "Controls":
                    InstanceFrame.NavigateTo(new InstanceControls(VersionInfo));
                    break;
            }
        }
    }

    private void LaunchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskLaunchGameItem.Launch(VersionInfo);
    }
}