using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Views.Pages.SetupPage;

public partial class SetupCompleted : UserControl
{
    public SetupCompleted()
    {
        InitializeComponent();
    }

    private void Start_OnClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine(@"跳转主页面.jpg");
        Dispatcher.UIThread.Invoke(() => GlobalModel.MainWindow.MainFrame.NavigateTo(new MainPage()));
        Core.Global.GlobalModel.Config.Data.IsFirstRun = false;
        Core.Global.GlobalModel.Config.Save();
    }
}