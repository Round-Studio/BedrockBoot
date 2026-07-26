using System;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace BedrockBoot.Views.Pages.OverlayPage;

public partial class OverlayMain : UserControl
{
    private readonly DispatcherTimer _dispatcherTimer;
    private readonly TextBlock _timeBlock;
    private readonly Timer _timer;

    public OverlayMain()
    {
        InitializeComponent();

        _timeBlock = TimeBlock;

        _dispatcherTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _dispatcherTimer.Tick += DispatcherTimer_Tick;

        UpdateTime();
    }

    /// <summary>
    /// 仅在控件实际显示时走时钟，避免隐藏后仍然每秒唤醒 UI 线程
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        UpdateTime();
        _dispatcherTimer.Start();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _dispatcherTimer.Stop();
    }

    private void DispatcherTimer_Tick(object? sender, EventArgs e)
    {
        UpdateTime();
    }

    private void UpdateTime()
    {
        if (_timeBlock != null) _timeBlock.Text = DateTime.Now.ToString("HH:mm:ss");
    }
}