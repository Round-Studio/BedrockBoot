using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogLaunchGameContent : UserControl
{
    public VersionConfig VersionInfo { get; set; }
    public bool IsCancel = false;
    public DialogLaunchGameContent()
    {
        InitializeComponent();
    }

    public DialogLaunchGameContent(VersionConfig info) : this()
    {
        VersionInfo = info;
    }

    public void Launch(Action launchCompleted)
    {
        Console.WriteLine($"正在启动：{VersionInfo.Info.VersionName} ({VersionInfo.Info.Version}) Type：{VersionInfo.Info.VersionType} {VersionInfo.Info.BuildType}");

        Task.Run(() =>
        {
            GlobalModel.BedrockCore.RemoveGame(VersionInfo.Info.VersionType);
            if (IsCancel) return;
            Dispatcher.UIThread.Invoke(() => LaunchProgressBar.IsIndeterminate = false);

            GlobalModel.BedrockCore.ChangeVersion(VersionInfo.VersionPath, new InstallCallback()
            {
                result_callback = new Action<AsyncStatus, Exception>((s, e) =>
                {
                    Console.WriteLine($"result_callback: {s}");
                    if (IsCancel) return;
                    if (s == AsyncStatus.Completed)
                    {
                        Console.WriteLine(GlobalModel.BedrockCore.LaunchGame(VersionInfo.Info.VersionType));
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            LaunchProgressText.Text = $"步骤：部署完毕，即将启动。";
                        });

                        Thread.Sleep(1200);

                        launchCompleted();
                    }
                }),
                registerProcess_percent = new Action<string, uint>((s, e) =>
                {
                    Console.WriteLine($"registerProcess_percent: {s} - {e}");
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        LaunchProgressText.Text = $"步骤：{s} ({e}%)";
                        LaunchProgressBar.Value = e;
                    });
                })
            });
        });
    }

    public static void Launch(VersionConfig gameInfo)
    {
        var body = new DialogLaunchGameContent(gameInfo);
        DialogHost.Show(new ()
        {
            CloseButtonText = "取消",
            CloseAction = () =>
            {
                body.IsCancel = true;
            },
            Title = $"启动 {gameInfo.Info.VersionName}",
            Content = body
        });

        body.Launch((() =>
        {
            Dispatcher.UIThread.Invoke(DialogHost.Close);
        }));
    }
}