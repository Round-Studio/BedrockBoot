using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Wallpaper.Avalonia.Native;

namespace Wallpaper.Avalonia.Controls;

/// <summary>
/// 一个无边框桌面缩略图窗口，跟随指定的主窗口移动/缩放，
/// 始终保持在主窗口 Z 序下方，主窗口最小化时自动隐藏。
/// </summary>
[SupportedOSPlatform("windows")]
public partial class DesktopThumbnailWindow : Window
{
    // ──────────────────────────────────────────────
    //  内部状态
    // ──────────────────────────────────────────────
    private Window? _owner;
    private IntPtr _ownerHwnd = IntPtr.Zero;
    private IntPtr _myHwnd = IntPtr.Zero;
    private bool _isFollowing;
    private DispatcherTimer? _syncTimer;
    private bool _lastWasMinimized;

    // ──────────────────────────────────────────────
    //  构造
    // ──────────────────────────────────────────────
    public DesktopThumbnailWindow()
    {
        InitializeComponent();

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Debug.WriteLine("[DesktopThumbnailWindow] 当前仅支持 Windows。");
            return;
        }

        Opened += OnOpened;
        Closed += OnClosed;
    }

    // ──────────────────────────────────────────────
    //  公开方法：绑定到主窗口并显示
    // ──────────────────────────────────────────────
    /// <summary>
    /// 绑定到指定的主窗口，并将本窗口显示在主窗口下方。
    /// </summary>
    public void ShowBelow(Window owner)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        if (_isFollowing)
            DetachOwner();

        _owner = owner;
        AttachOwner();
        Show();

        // 窗口显示后立即执行一次位置/大小同步和 Z 序设定
        Dispatcher.UIThread.Post(() =>
        {
            SyncPositionAndSize();
            EnsureZOrder();
        }, DispatcherPriority.Loaded);
    }

    // ──────────────────────────────────────────────
    //  事件绑定 / 解绑
    // ──────────────────────────────────────────────
    private void AttachOwner()
    {
        if (_owner == null) return;

        _owner.PositionChanged += OnOwnerPositionChanged;
        _owner.SizeChanged += OnOwnerSizeChanged;
        _owner.Activated += OnOwnerActivated;

        // 启动定时器，轮询窗口状态（最小化检测、位置/大小同步）
        StartSyncTimer();
        _isFollowing = true;
    }

    private void DetachOwner()
    {
        StopSyncTimer();

        if (_owner == null) return;

        _owner.PositionChanged -= OnOwnerPositionChanged;
        _owner.SizeChanged -= OnOwnerSizeChanged;
        _owner.Activated -= OnOwnerActivated;
        _owner = null;
        _ownerHwnd = IntPtr.Zero;
        _isFollowing = false;
    }

    // ──────────────────────────────────────────────
    //  窗口生命周期
    // ──────────────────────────────────────────────
    private void OnOpened(object? sender, EventArgs e)
    {
        // 缓存自身 HWND
        var handle = TryGetPlatformHandle();
        if (handle != null)
            _myHwnd = handle.Handle;

        if (_myHwnd != IntPtr.Zero)
        {
            // 确保窗口不在任务栏显示
            RemoveFromTaskbar();

            // Win10 / Win11 自适应圆角
            WindowHelper.ApplySystemCornerPreference(_myHwnd);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        DetachOwner();
    }

    // ──────────────────────────────────────────────
    //  主窗口事件处理
    // ──────────────────────────────────────────────
    private void OnOwnerPositionChanged(object? sender, PixelPointEventArgs e)
    {
        SyncPositionAndSize();
        EnsureZOrder();
    }

    private void OnOwnerSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        SyncPositionAndSize();
        EnsureZOrder();
    }

    private void OnOwnerActivated(object? sender, EventArgs e)
    {
        // 主窗口获得焦点 → 立即复位 Z 序和位置
        Dispatcher.UIThread.Post(() =>
        {
            SyncPositionAndSize();
            EnsureZOrder();
        }, DispatcherPriority.Render);
    }

    // ──────────────────────────────────────────────
    //  同步定时器（兜底）
    // ──────────────────────────────────────────────
    private void StartSyncTimer()
    {
        _syncTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(50),
            DispatcherPriority.Background,
            OnSyncTimerTick);
        _syncTimer.Start();
    }

    private void StopSyncTimer()
    {
        if (_syncTimer != null)
        {
            _syncTimer.Stop();
            _syncTimer = null;
        }
    }

    private void OnSyncTimerTick(object? sender, EventArgs e)
    {
        if (_owner == null || _myHwnd == IntPtr.Zero) return;

        // 缓存 owner HWND
        if (_ownerHwnd == IntPtr.Zero)
        {
            var handle = _owner.TryGetPlatformHandle();
            if (handle != null)
                _ownerHwnd = handle.Handle;
        }

        if (_ownerHwnd == IntPtr.Zero) return;

        // 检测主窗口是否最小化
        bool isMinimized = WindowHelper.IsIconic(_ownerHwnd);
        if (isMinimized != _lastWasMinimized)
        {
            _lastWasMinimized = isMinimized;
            if (isMinimized)
                HideThumbnail();
            else
            {
                ShowThumbnail();
                // 恢复后立即同步位置
                SyncPositionAndSize();
                EnsureZOrder();
            }
        }

        // 主窗口非最小化时持续同步
        if (!isMinimized)
        {
            SyncPositionAndSize();
        }
    }

    // ──────────────────────────────────────────────
    //  核心逻辑
    // ──────────────────────────────────────────────
    /// <summary>
    /// 将本窗口移动/缩放至与主窗口一致。
    /// </summary>
    private void SyncPositionAndSize()
    {
        if (_owner == null || _myHwnd == IntPtr.Zero) return;

        // 缓存 owner HWND
        if (_ownerHwnd == IntPtr.Zero)
        {
            var handle = _owner.TryGetPlatformHandle();
            if (handle != null)
                _ownerHwnd = handle.Handle;
        }

        if (_ownerHwnd == IntPtr.Zero) return;

        // 获取主窗口的屏幕矩形
        if (!WindowHelper.GetWindowRect(_ownerHwnd, out var rect))
            return;

        int width = rect.Width;
        int height = rect.Height;

        if (width <= 0 || height <= 0) return;

        WindowHelper.MoveWindow(_myHwnd, rect.Left, rect.Top, width, height);
    }

    /// <summary>
    /// 确保本窗口 Z 序紧贴在主窗口正下方。
    /// </summary>
    private void EnsureZOrder()
    {
        if (_ownerHwnd == IntPtr.Zero || _myHwnd == IntPtr.Zero) return;
        WindowHelper.PlaceBelow(_myHwnd, _ownerHwnd);
    }

    // ──────────────────────────────────────────────
    //  显隐控制
    // ──────────────────────────────────────────────
    private void HideThumbnail()
    {
        if (_myHwnd != IntPtr.Zero)
            WindowHelper.ShowWindow(_myHwnd, WindowHelper.SW_HIDE);
    }

    private void ShowThumbnail()
    {
        if (_myHwnd != IntPtr.Zero)
        {
            WindowHelper.ShowWindow(_myHwnd, WindowHelper.SW_SHOWNOACTIVATE);
            // 显示后恢复 Z 序
            EnsureZOrder();
        }
    }

    // ──────────────────────────────────────────────
    //  从任务栏移除
    // ──────────────────────────────────────────────
    private void RemoveFromTaskbar()
    {
        if (_myHwnd == IntPtr.Zero) return;

        IntPtr exStyle = WindowHelper.GetWindowLongPtrW(_myHwnd, WindowHelper.GWL_EXSTYLE);
        long exStyleVal = exStyle.ToInt64();

        // 添加 WS_EX_TOOLWINDOW，移除 WS_EX_APPWINDOW
        exStyleVal |= WindowHelper.WS_EX_TOOLWINDOW;
        exStyleVal &= ~WindowHelper.WS_EX_APPWINDOW;

        _ = WindowHelper.SetWindowLongPtrW(_myHwnd, WindowHelper.GWL_EXSTYLE, new IntPtr(exStyleVal));
    }
}
