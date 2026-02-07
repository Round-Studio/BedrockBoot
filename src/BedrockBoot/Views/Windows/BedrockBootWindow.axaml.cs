using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Notice.Info;

namespace BedrockBoot.Views.Windows;

public partial class BedrockBootWindow : Window
{
    private readonly Timer _stateTimer;

    public int DrawMarginLR = 10;

    public BedrockBootWindow()
    {
        InitializeComponent();

        Frame.NavigateTo("");
        _stateTimer = new Timer(state =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                if (OperatingSystem.IsWindows())
                {
                    if (WindowState == WindowState.Maximized) Padding = new Thickness(8);
                    else Padding = new Thickness(0);
                }

                if (WindowState == WindowState.Maximized) MaxBtnIcon.Glyph = "\uE923";
                else MaxBtnIcon.Glyph = "\uE922";

                TitleBlock.Text = Title;
            });
        });
        _stateTimer.Change(TimeSpan.FromMilliseconds(0), TimeSpan.FromMilliseconds(100));
        BottomBorder.Margin = new Thickness(DrawMarginLR, 0, DrawMarginLR, 0);
        GlobalModel.TaskManager.OnChanged = () => Dispatcher.UIThread.Invoke(UpdateTaskUI);
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

    public bool IsMaxBtn
    {
        get => _isMaxBtn;
        set
        {
            _isMaxBtn = value;
            UpdateUI();
        }
    }

    public bool IsMinBtn
    {
        get => _isMinBtn;
        set
        {
            _isMinBtn = value;
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

    public bool IsTaskCardOpen { get; private set; } = false;
    public void OpenTaskCard()
    {
        TaskCard.Margin = new Thickness(10);
        ContentView.Effect = new BlurEffect()
        {
            Radius = 50
        };
        BackgroundGroupBox.Effect = new BlurEffect()
        {
            Radius = 50
        };
        BackgroundGroupBox.Margin = new(-50);
        IsTaskCardOpen = true;
        BlackView.IsVisible = true;
    }
    
    public void CloseTaskCard()
    {
        TaskCard.Margin = new Thickness(500,10,-500,10);
        ContentView.Effect = new BlurEffect()
        {
            Radius = 0
        };
        BackgroundGroupBox.Effect = new BlurEffect()
        {
            Radius = 0
        };
        BackgroundGroupBox.Margin = new(0);
        IsTaskCardOpen = false;
        BlackView.IsVisible = false;
    }

    public void UpdateTaskUI()
    {
        TaskList.Children.Clear();
        TaskViewer.IsVisible = true;
        NoneBox.IsVisible = false;

        if (GlobalModel.TaskManager.Tasks.Count <= 0)
        {
            TaskViewer.IsVisible = false;
            NoneBox.IsVisible = true;
        }

        GlobalModel.TaskManager.Tasks.ForEach(task =>
        {
            task.Item.Margin = new Thickness(5);
            TaskList.Children.Add(task.Item);
        });
    }

    private void BlackView_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        CloseTaskCard();
    }
}