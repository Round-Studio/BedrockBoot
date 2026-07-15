using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Timers;

namespace BedrockBoot.Views.Control.Widgets.DesktopWidgets;

public partial class WidgetTimer : UserControl
{
    private TextBlock? _timeTextBlock;
    private Timer? _timer;

    public WidgetTimer()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _timeTextBlock = this.FindControl<TextBlock>("TimeTextBlock");
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        _timer = new Timer(1000);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        _timer.Start();
        
        UpdateTime();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (_timer != null)
        {
            _timer.Elapsed -= OnTimerElapsed;
            _timer.Stop();
            _timer.Dispose();
            _timer = null;
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(UpdateTime);
    }

    private void UpdateTime()
    {
        if (_timeTextBlock == null) return;
        _timeTextBlock.Text = DateTime.Now.ToString("H:mm");
    }
}