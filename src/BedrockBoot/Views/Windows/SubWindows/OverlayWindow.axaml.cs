using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Platform;
using Avalonia.Interactivity;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Views.Windows.SubWindows;

public partial class OverlayWindow : Window
{
    // --- Win32 API 导入 ---
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hwnd, ref RECT rectangle);

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    // 键盘钩子相关
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    // --- Win32 常量 ---
    private const int GWL_EXSTYLE = -20;
    private const int GWL_STYLE = -16;
    private const int WS_CHILD = 0x40000000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SWP_NOMOVE = 0x0002;
    private const int SWP_NOSIZE = 0x0001;
    private const int HWND_BOTTOM = 1;

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104; // 捕捉带 Alt 的组合键，增强稳定性
    private const int VK_SHIFT = 0x10;
    private const int VK_TAB = 0x09;
    private const int VK_ESCAPE = 0x1B;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS
    {
        public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    // --- 成员变量 ---
    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookID = IntPtr.Zero;
    private IntPtr _targetHwnd = IntPtr.Zero;
    private IntPtr _myHandle = IntPtr.Zero;
    private bool _isOverlayVisible = false;
    private bool _isEmbedded = false; // 新增标志位，跟踪是否已内嵌
    private DispatcherTimer? _syncTimer;
    private int _originalWidth = 0;
    private int _originalHeight = 0;
    private int _originalX = 0;
    private int _originalY = 0;

    public OverlayWindow()
    {
        InitializeComponent();

        VersionBox.Text = $"Game Overlay (Ver.{GlobalModel.BodyVersion})";
        
        _proc = HookCallback;

        // 确保窗口在最上层
        Topmost = true;

        Opened += OnWindowOpened;
        Closed += OnWindowClosed;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        var platformHandle = TryGetPlatformHandle();
        if (platformHandle == null) return;
        _myHandle = platformHandle.Handle;

        // 查找目标窗口（此处以 Minecraft 为例）
        _targetHwnd = FindWindow(null, "Minecraft");

        // 3. 挂载全局键盘钩子
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule?.ModuleName), 0);
        }

        // 4. 启动同步计时器 (30 FPS 左右)
        _syncTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(32) };
        _syncTimer.Tick += (s, ev) => SyncSize();
        _syncTimer.Start();

        // 初始状态设为隐藏（完全隐藏）
        SetOverlayState(false);
    }

    private void SetOverlayState(bool visible)
    {
        _isOverlayVisible = visible;

        if (visible)
        {
            // --- 显示叠层 ---
            if (_targetHwnd != IntPtr.Zero)
            {
                // 重新嵌入目标窗口
                SetParent(_myHandle, _targetHwnd);
                _isEmbedded = true;

                var clientRect = new RECT();
                if (GetClientRect(_targetHwnd, ref clientRect))
                {
                    _originalWidth = clientRect.Right - clientRect.Left;
                    _originalHeight = clientRect.Bottom - clientRect.Top;
                    _originalX = clientRect.Left;
                    _originalY = clientRect.Top;
                }

                // 设置为子窗口样式
                var style = GetWindowLong(_myHandle, GWL_STYLE);
                SetWindowLong(_myHandle, GWL_STYLE, style | WS_CHILD);

                // 恢复窗口大小
                Width = _originalWidth;
                Height = _originalHeight;
                MoveWindow(_myHandle, _originalX, _originalY, _originalWidth, _originalHeight, true);

                // 设置窗口样式（移除穿透）
                var exStyle = GetWindowLong(_myHandle, GWL_EXSTYLE);
                exStyle &= ~WS_EX_TRANSPARENT;
                exStyle &= ~WS_EX_NOACTIVATE;
                exStyle |= WS_EX_LAYERED; // 保留分层窗口样式
                SetWindowLong(_myHandle, GWL_EXSTYLE, exStyle);

                // 显示窗口
                ShowWindow(_myHandle, SW_SHOW);
                EnableWindow(_myHandle, true);

                // 在Avalonia层面允许点击
                IsHitTestVisible = true;
                OverlayRoot.IsHitTestVisible = true;

                // 设置不透明度
                OverlayRoot.Opacity = 1;

                // 激活并置顶焦点
                SetForegroundWindow(_myHandle);
                SetFocus(_myHandle);
            }
        }
        else
        {
            // --- 完全隐藏模式 ---
            // 记录原始大小用于恢复
            if (_originalWidth == 0 || _originalHeight == 0)
            {
                _originalWidth = (int)Width;
                _originalHeight = (int)Height;
            }

            // 在Avalonia层面也设置为不可点击
            IsHitTestVisible = false;
            OverlayRoot.IsHitTestVisible = false;

            // 设置完全透明
            OverlayRoot.Opacity = 0;

            Task.Run(() =>
            {
                Thread.Sleep(300);

                Dispatcher.UIThread.Invoke(() =>
                {
                    // 设置窗口穿透样式以确保不影响目标窗口的鼠标事件
                    var exStyle = GetWindowLong(_myHandle, GWL_EXSTYLE);
                    exStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                    // 移除可能导致干扰的样式
                    exStyle &= ~WS_EX_LAYERED;
                    SetWindowLong(_myHandle, GWL_EXSTYLE, exStyle);

                    // 移除内嵌关系，让窗口完全脱离目标窗口
                    if (_isEmbedded)
                    {
                        SetParent(_myHandle, IntPtr.Zero); // 设置父窗口为桌面
                        _isEmbedded = false;

                        // 恢复原始窗口样式
                        var style = GetWindowLong(_myHandle, GWL_STYLE);
                        SetWindowLong(_myHandle, GWL_STYLE, style & ~WS_CHILD);
                    }

                    // 焦点彻底交还给游戏
                    if (_targetHwnd != IntPtr.Zero)
                    {
                        SetForegroundWindow(_targetHwnd);
                        SetFocus(_targetHwnd);
                    }

                    // 完全隐藏窗口
                    ShowWindow(_myHandle, SW_HIDE);
                    EnableWindow(_myHandle, false);
                });
            });
        }
    }

    private void SyncSize()
    {
        // 只有在显示状态下且已内嵌时才同步大小
        if (!_isOverlayVisible || !_isEmbedded || _targetHwnd == IntPtr.Zero || _myHandle == IntPtr.Zero) return;

        var clientRect = new RECT();
        if (GetClientRect(_targetHwnd, ref clientRect))
        {
            var w = clientRect.Right - clientRect.Left;
            var h = clientRect.Bottom - clientRect.Top;

            // 只有在尺寸变化时才调用 MoveWindow，减少开销
            if (Width != w || Height != h)
            {
                Width = w;
                Height = h;
                MoveWindow(_myHandle, 0, 0, w, h, true);
            }
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            var vkCode = Marshal.ReadInt32(lParam);

            // 检测 Shift + Tab (Toggle显示/隐藏)
            var isShiftDown = (GetKeyState(VK_SHIFT) & 0x8000) != 0;

            if (vkCode == VK_TAB && isShiftDown)
            {
                var foreground = GetForegroundWindow();
                // 仅当游戏窗口或叠层窗口在前端时响应
                if (foreground == _targetHwnd || foreground == _myHandle)
                {
                    // 异步切换 UI 状态，避免阻塞钩子链
                    Dispatcher.UIThread.Post(() => SetOverlayState(!_isOverlayVisible));

                    // 返回 1 吞掉按键，防止游戏内弹出多余菜单
                    return (IntPtr)1;
                }
            }

            // 检测 ESC (仅用于退出显示状态，不触发显示)
            if (vkCode == VK_ESCAPE)
            {
                var foreground = GetForegroundWindow();
                // 仅当叠层窗口在前端时响应ESC
                if (foreground == _myHandle && _isOverlayVisible)
                {
                    // 异步隐藏叠层，避免阻塞钩子链
                    Dispatcher.UIThread.Post(() => SetOverlayState(false));

                    // 返回 1 吞掉按键，防止游戏响应ESC
                    return (IntPtr)1;
                }
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _syncTimer?.Stop();
        if (_hookID != IntPtr.Zero) UnhookWindowsHookEx(_hookID);
    }

    private void CloseBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        SetOverlayState(false);
    }
}