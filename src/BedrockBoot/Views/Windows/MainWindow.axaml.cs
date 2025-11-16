using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Views.Pages;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.WindowFrame;

namespace BedrockBoot.Views.Windows;

public partial class MainWindow : OnePointWindow
{
    public static BedrockCore BedrockCore { get; set; }
    public MainWindow()
    {
        InitializeComponent();

        MainFrame.NavigateTo(new LoadingPage());
        Task.Run(() =>
        {
            BedrockCore = new  BedrockCore();
            try
            {
                BedrockCore.Init();
                Console.WriteLine("初始化核心完毕");
            }
            catch
            {
                Console.WriteLine("无法自动打开开发者模式");
                DialogHost.Show(new DialogInfo()
                {
                    Title = "开发者模式",
                    Content = "我们貌似无法帮您打开开发者模式，请您手动前往设置中打开。",
                    CloseButtonText = "确定"
                });
            }
            finally
            {
                Console.WriteLine("跳转主页面.jpg");
                Dispatcher.UIThread.Invoke(() => MainFrame.NavigateTo(new MainPage()));
            }
        });
    }
}