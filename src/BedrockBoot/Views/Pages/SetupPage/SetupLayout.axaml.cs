using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;

namespace BedrockBoot.Views.Pages.SetupPage;

public partial class SetupLayout : ISetting
{
    public SetupLayout()
    {
        InitializeComponent();
        IsRightLaunchButton.SelectedIndex = GlobalModel.Config.Data.IsRightLaunchButton ? 1 : 0;
        
        IsEdit = true;
    }
    private void IsRightLaunchButton_OnSelectionChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsRightLaunchButton =
                (IsRightLaunchButton.SelectedIndex == 1);
            GlobalModel.Config.Save();
        }
    }
}