using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawInstanceContent : UserControl
{
    public VersionConfig VersionInfo { get; set; }
    public bool IsEditMode { get; set; } = false;

    public DrawInstanceContent()
    {
        InitializeComponent();

        IsEditMode = true;
    }

    public DrawInstanceContent(VersionConfig info) : this()
    {
        VersionInfo = info;
        
        Update();
    }

    public void Update()
    {
        InstanceFrame.NavigateTo(new InstanceInfo(VersionInfo));
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
                case "Controls":
                    InstanceFrame.NavigateTo(new InstanceControls());
                    break;
            }
        }
    }
}