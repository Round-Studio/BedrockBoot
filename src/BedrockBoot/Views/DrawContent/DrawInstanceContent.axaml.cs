using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawInstanceContent : UserControl
{
    public VersionInfo VersionInfo { get; set; }
    public bool IsEditMode { get; set; } = false;

    public DrawInstanceContent()
    {
        InitializeComponent();
        InstanceFrame.NavigateTo(new InstanceInfo());

        IsEditMode = true;
    }

    public DrawInstanceContent(VersionInfo info) : this()
    {
        VersionInfo = info;
        
        Update();
    }

    public void Update()
    {
        
    }

    private void InstanceTabControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            var tag = ((TabItem)(InstanceTabControl.SelectedItem)).Tag.ToString();

            switch (tag)
            {
                case "Info":
                    InstanceFrame.NavigateTo(new InstanceInfo());
                    break;
            }
        }
    }
}