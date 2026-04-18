using System;
using System.Timers;
using Avalonia.Controls;
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
        _dispatcherTimer.Start();

        UpdateTime();
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