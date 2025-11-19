using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.WindowFrame;
using Round.SDK.Helper;

namespace BedrockBoot.Views.Windows;

public partial class MainWindow : OnePointWindow
{
    public MainWindow()
    {
        GlobalModel.MainWindow = this;
        InitializeComponent();

        MainFrame.NavigateTo(new LoadingPage());
        BuildTime.Text = $"Build.2.{((DateTime)(CheckVersion.GetBuildTimestamp(Assembly.GetExecutingAssembly()))).ToString("yy.MMdd.HHmmss")}";
        Task.Run(() =>
        {
            GlobalModel.BedrockCore = new  BedrockCore();
            try
            {
                GlobalModel.BedrockCore.Init();
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
                
#if DEBUG
                var date = CheckVersion.GetBuildTimestamp(Assembly.GetExecutingAssembly());
                var zt = CheckVersion.CheckTimeAndExecute24Hour((DateTime)date);
                Console.WriteLine(@$"当前模式：Debug 模式");
                Console.WriteLine(@$"当前程序集编译日期：{date}");
                Console.WriteLine(@$"当前版本是否在可用时间段内：{zt}");

                if (zt)
                {
                    DialogHost.Show(new DialogInfo()
                    {
                        Content =
                            $"当前版本为预览版本，请勿添加到整合包中使用。\n当前版本仅作为测试部分功能，将于 24h 后失效，请抓紧时间进行测试。\n当前可用状态：{zt}",
                        Title = "版本模式提示",
                        CloseButtonText = "开始测试",
                        AccountButton = DialogButtons.CloseButton
                    });
                }
                else
                {
                    DialogHost.Show(new DialogInfo()
                    {
                        Content =
                            $"当前版本为预览版本，请勿添加到整合包中使用。\n当前版本仅作为测试部分功能，将于 24h 后失效，当前已失效。\n当前可用状态：{zt}",
                        Title = "版本模式提示",
                        CloseButtonText = "退出",
                        CloseAction = () => { Environment.Exit(0); },
                        AccountButton = DialogButtons.CloseButton
                    });
                }
#endif
            }
        });
    }
}