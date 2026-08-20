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
        
        IsEdit = true;
    }

    private void KLeft_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        GlobalModel.Config.Data.IsRightLaunchButton = !(bool)KLeft.IsChecked!;
        GlobalModel.Config.Save();
    }

    private void KRight_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        GlobalModel.Config.Data.IsRightLaunchButton = (bool)KRight.IsChecked!;
        GlobalModel.Config.Save();
    }
}