using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using BedrockBoot.Base.Enum;
using BedrockBoot.Core.Global;
using BedrockBoot.Models.Media;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Notice.Info;

namespace BedrockBoot.Views.Windows;

public partial class BedrockBootWindow : Window
{
    private readonly Timer _stateTimer;
    private bool _ctrlPressed = false;
    public int DrawMarginLR = 10;
    private DispatcherTimer _volumeControlTimer;

    public BedrockBootWindow()
    {
        InitializeComponent();
        
        MediaManager.Instance.Volume = (float)Math.Clamp(GlobalModel.Config.Data.MediaVolume, 0.0, 1.0);
        this.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        this.AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
        Deactivated += OnWindowDeactivated;
        this.AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        Frame.NavigateTo("");
        _volumeControlTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.8)
        };
        _volumeControlTimer.Tick += VolumeControlTimer_Tick;
        _stateTimer = new Timer(state =>
        {
            try
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    UpdateWindowBorder();
                    if (OperatingSystem.IsWindows())
                    {
                        if (WindowState == WindowState.Maximized &&
                            !GlobalModel.Config.Data.IsUseSystemWindow)
                            Padding = new Thickness(8);
                        else Padding = new Thickness(0);
                    }

                    if (WindowState == WindowState.Maximized) MaxBtnIcon.Glyph = "\uE923";
                    else MaxBtnIcon.Glyph = "\uE922";

                    BackgroundCover.IsVisible = GlobalModel.Config.Data.StyleConfig.StyleType ==
                                                StyleType.Blur;

                    TitleBlock.Text = Title;
                });
            }
            catch
            {
            }
        });
        _stateTimer.Change(TimeSpan.FromMilliseconds(0), TimeSpan.FromMilliseconds(100));
        BottomBorder.Margin = new Thickness(DrawMarginLR, 0, DrawMarginLR, 0);
    }
    
    /// <summary>
    /// 唤醒音量提示框
    /// </summary>
    public void ShowVolumeCard()
    {
        // 确保在 UI 线程执行
        Dispatcher.UIThread.Post(() =>
        {
            _volumeControlTimer.Stop();
            MediaVolumeCard.Margin = new Thickness(0, 19, 0, 0);

            _volumeControlTimer.Start();
        });
    }

    private void VolumeControlTimer_Tick(object? sender, EventArgs e)
    {
        // 时间到了，缩回顶部
        MediaVolumeCard.Margin = new Thickness(0, -76, 0, 0);
        _volumeControlTimer.Stop();
    }
    
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
        {
            _ctrlPressed = true;
        }
    }
    
    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
        {
            _ctrlPressed = false;
        }
    }

    private void OnWindowDeactivated(object sender, EventArgs e)
    {
        _ctrlPressed = false;
    }
    
    private void OnPointerWheelChanged(object sender, PointerWheelEventArgs e)
    {
        if (_ctrlPressed)
        {
            ShowVolumeCard();
            
            double delta = e.Delta.Y;
            double step = 0.05;
        
            double newVolume = MediaManager.Instance.Volume + (delta > 0 ? step : -step);
            if (newVolume * 100 < 0)
            {
                newVolume = 0;  
            }
            else if (newVolume * 100 > 100)
            {
                newVolume = 1;
            }

            MediaVolume.Value = (newVolume * 100);

            if (MediaVolume.Value != 0)
            {
                MediaVolumeCard.Width = 170;
            }
            else
            {
                MediaVolumeCard.Width = 150;
            }
            
            DisableVolumeText.IsVisible = false;

            switch (MediaVolume.Value)
            {
                case <= 0:
                    MediaVolumeIcon.Glyph = "\uE74F";
                    DisableVolumeText.IsVisible = true;
                    break;
                case < 33:
                    MediaVolumeIcon.Glyph = "\uE993";
                    break;
                case < 66:
                    MediaVolumeIcon.Glyph = "\uE994";
                    break;
                case < 100:
                    MediaVolumeIcon.Glyph = "\uE995";
                    break;
            }
        
            Console.WriteLine($@"当前音量：{(int)(newVolume * 100)}%");
        
            // 应用新音量
            GlobalModel.Config.Data.MediaVolume = newVolume;
            GlobalModel.Config.Save();
            
            MediaManager.Instance.Volume = (float)Math.Clamp(GlobalModel.Config.Data.MediaVolume, 0.0, 1.0);
        
            // 阻止事件继续冒泡
            e.Handled = true;
        }
    }
    
    public bool IsMainWindow
    {
        get => _isMainWindow;
        set
        {
            _isMainWindow = value;
            UpdateUI();
        }
    }

    public object? MainContent
    {
        get => _mainContent;
        set
        {
            _mainContent = value;
            UpdateUI();
        }
    }

    public object? TitleBarContent
    {
        get => _titleBarContent;
        set
        {
            _titleBarContent = value;
            UpdateUI();
        }
    }

    public object? TitleBarContentContent
    {
        get => _titleBarControlContent;
        set
        {
            _titleBarControlContent = value;
            UpdateUI();
        }
    }

    public NoticePanel Notice => NoticePanel;
    private object? _mainContent { get; set; }
    private object? _titleBarContent { get; set; }
    private object? _titleBarControlContent { get; set; }
    private bool _isMainWindow { get; set; }
    private bool _isMinBtn { get; set; } = true;
    private bool _isMaxBtn { get; set; } = true;

    public bool IsTaskCardOpen { get; private set; }

    public void UpdateWindowBorder()
    {
        MaxBtn.IsVisible = !GlobalModel.Config.Data.IsUseSystemWindow;
        MinBtn.IsVisible = !GlobalModel.Config.Data.IsUseSystemWindow;
        CloseBtn.IsVisible = !GlobalModel.Config.Data.IsUseSystemWindow;
        ExtendClientAreaToDecorationsHint = !GlobalModel.Config.Data.IsUseSystemWindow;
        ExtendClientAreaTitleBarHeightHint = -1;
        ExtendClientAreaChromeHints = GlobalModel.Config.Data.IsUseSystemWindow
            ? ExtendClientAreaChromeHints.Default
            : ExtendClientAreaChromeHints.NoChrome;
    }

    private void UpdateUI()
    {
        PART_MainContent.Content = _mainContent;
        TitleBlock.Text = Title;
        TitleContent.Content = _titleBarContent;
        TitleBarContentBarContent.Content = _titleBarControlContent;

        MaxBtn.IsVisible = _isMaxBtn;
        MinBtn.IsVisible = _isMaxBtn;

        if (IsMainWindow) DialogHost.SetHost(DialogHost);
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void MinBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaxBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();

        if (IsMainWindow) Environment.Exit(0);
    }

    public void CloseDraw()
    {
        SetBorderState(false);
    }

    public async void OpenDraw(object? page, string title)
    {
        BorderTitle.Text = title;
        await SetBorderState(true);

        Frame.NavigateTo(page);
    }

    private async Task SetBorderState(bool state)
    {
        if (state)
        {
            BottomBorder.Margin = new Thickness(DrawMarginLR, Height, DrawMarginLR, -Height);
            await Task.Delay(100);
            BorderGrid.IsVisible = true;
            BottomBorder.Margin = new Thickness(DrawMarginLR, 76, DrawMarginLR, 0);
            BorderBackground.Opacity = 0.3;
            await Task.Delay(200);
        }
        else
        {
            BottomBorder.Margin = new Thickness(DrawMarginLR, Height, DrawMarginLR, -Height);
            BorderBackground.Opacity = 0;
            await Task.Delay(800);
            BorderGrid.IsVisible = false;
            Frame.NavigateTo("");
        }
    }

    private void CloseBorderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        SetBorderState(false);
    }

    public void SetBlurState(bool state)
    {
        ContentView.Effect = new BlurEffect
        {
            Radius = state ? 50 : 0
        };
        BackgroundGroupBox.Effect = new BlurEffect
        {
            Radius = state ? 50 : 0
        };
    }

    public async void OpenTaskCard()
    {
        SetBlurState(true);
        TaskCard.Margin = new Thickness(10);
        BackgroundGroupBox.Margin = new Thickness(-50);
        IsTaskCardOpen = true;
        BlackView.IsVisible = true;
        
        DropBox.Opacity = 0;
        SetBlurState(false);
        await Task.Delay(360);
        DropBox.IsVisible = false;
    }

    public void CloseTaskCard()
    {
        SetBlurState(false);
        TaskCard.Margin = new Thickness(500, 10, -500, 10);
        BackgroundGroupBox.Margin = new Thickness(0);
        IsTaskCardOpen = false;
        BlackView.IsVisible = false;
    }

    private void BlackView_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        CloseTaskCard();
    }
}