using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Wallpaper.Avalonia.Native;

namespace Wallpaper.Avalonia.Controls;

/// <summary>
/// Avalonia 桌面缩略图控件。
/// 将 DWM 桌面缩略图直接注册到 Avalonia 顶层窗口的原生 HWND 上，
/// 通过 rcDestination（窗口客户区坐标）划定控件所占的区域。
///
/// 用法:
///   <controls:DesktopThumbnailControl />
/// </summary>
[SupportedOSPlatform("windows")]
public class DesktopThumbnailControl : UserControl
{
    // ──────────────────────────────────────────────
    //  依赖属性
    // ──────────────────────────────────────────────
    public static readonly StyledProperty<bool> ClientAreaOnlyProperty =
        AvaloniaProperty.Register<DesktopThumbnailControl, bool>(nameof(ClientAreaOnly), false);

    public static readonly StyledProperty<byte> ThumbnailOpacityProperty =
        AvaloniaProperty.Register<DesktopThumbnailControl, byte>(nameof(ThumbnailOpacity), 255);

    public static readonly StyledProperty<bool> AutoFindDesktopProperty =
        AvaloniaProperty.Register<DesktopThumbnailControl, bool>(nameof(AutoFindDesktop), true);

    public bool ClientAreaOnly
    {
        get => GetValue(ClientAreaOnlyProperty);
        set => SetValue(ClientAreaOnlyProperty, value);
    }

    public byte ThumbnailOpacity
    {
        get => GetValue(ThumbnailOpacityProperty);
        set => SetValue(ThumbnailOpacityProperty, value);
    }

    public bool AutoFindDesktop
    {
        get => GetValue(AutoFindDesktopProperty);
        set => SetValue(AutoFindDesktopProperty, value);
    }

    // ──────────────────────────────────────────────
    //  内部状态
    // ──────────────────────────────────────────────
    private IntPtr _windowHwnd = IntPtr.Zero;      // 顶层窗口 HWND
    private IntPtr _sourceHwnd = IntPtr.Zero;       // 桌面图标窗口 HWND
    private IntPtr _thumbnailId = IntPtr.Zero;       // DWM 缩略图句柄
    private bool _thumbnailRegistered;
    private TopLevel? _topLevel;

    // ──────────────────────────────────────────────
    //  构造
    // ──────────────────────────────────────────────
    public DesktopThumbnailControl()
    {
        // 注册属性变更监听
        ClientAreaOnlyProperty.Changed.AddClassHandler<DesktopThumbnailControl>(
            (ctrl, _) => ctrl.UpdateThumbnail());
        ThumbnailOpacityProperty.Changed.AddClassHandler<DesktopThumbnailControl>(
            (ctrl, _) => ctrl.UpdateThumbnail());
        AutoFindDesktopProperty.Changed.AddClassHandler<DesktopThumbnailControl>(
            (ctrl, _) => ctrl.OnAutoFindDesktopChanged());
    }

    // ──────────────────────────────────────────────
    //  生命周期
    // ──────────────────────────────────────────────
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel == null)
            return;

        // 获取顶层窗口的原生 HWND
        var handle = _topLevel.TryGetPlatformHandle();
        if (handle == null || handle.Handle == IntPtr.Zero)
        {
            // 可能窗口尚未完全初始化，延迟尝试
            _topLevel.Opened += OnTopLevelOpened;
            return;
        }

        InitializeThumbnail(handle.Handle);
    }

    private void OnTopLevelOpened(object? sender, EventArgs e)
    {
        if (_topLevel == null) return;
        _topLevel.Opened -= OnTopLevelOpened;

        var handle = _topLevel.TryGetPlatformHandle();
        if (handle != null && handle.Handle != IntPtr.Zero)
        {
            InitializeThumbnail(handle.Handle);
        }
    }

    private void InitializeThumbnail(IntPtr windowHwnd)
    {
        _windowHwnd = windowHwnd;

        // 监听 EffectiveViewportChanged 以捕获滚动/视口变化
        EffectiveViewportChanged += OnEffectiveViewportChanged;

        // 自动查找桌面窗口
        if (AutoFindDesktop)
            FindAndRegisterDesktop();
    }

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        ScheduleThumbnailUpdate();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        EffectiveViewportChanged -= OnEffectiveViewportChanged;

        if (_topLevel != null)
            _topLevel.Opened -= OnTopLevelOpened;

        UnregisterThumbnail();
        _windowHwnd = IntPtr.Zero;
        _topLevel = null;

        base.OnDetachedFromVisualTree(e);
    }

    // ──────────────────────────────────────────────
    //  布局 —— 每次 arrange 时更新缩略图位置
    // ──────────────────────────────────────────────
    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        ScheduleThumbnailUpdate();
        return size;
    }

    private void ScheduleThumbnailUpdate()
    {
        // 使用 Render 优先级确保在渲染前更新完毕
        Dispatcher.UIThread.Post(UpdateThumbnail, DispatcherPriority.Render);
    }

    // ──────────────────────────────────────────────
    //  桌面窗口查找 & 缩略图注册
    // ──────────────────────────────────────────────
    private void FindAndRegisterDesktop()
    {
        var desktopHwnd = DesktopWindowFinder.FindDesktopIconWindow();
        if (desktopHwnd != IntPtr.Zero)
            RegisterThumbnail(desktopHwnd);
    }

    private void OnAutoFindDesktopChanged()
    {
        if (AutoFindDesktop && !_thumbnailRegistered)
            FindAndRegisterDesktop();
    }

    private void RegisterThumbnail(IntPtr sourceHwnd)
    {
        UnregisterThumbnail();

        if (_windowHwnd == IntPtr.Zero || sourceHwnd == IntPtr.Zero)
            return;

        _sourceHwnd = sourceHwnd;

        int hr = DwmApi.DwmRegisterThumbnail(_windowHwnd, sourceHwnd, out _thumbnailId);
        if (DwmApi.Succeeded(hr) && _thumbnailId != IntPtr.Zero)
        {
            _thumbnailRegistered = true;
            UpdateThumbnail();
            Console.WriteLine($@"缩略图已注册: window=0x{_windowHwnd:X}, source=0x{sourceHwnd:X}");
        }
        else
        {
            Console.WriteLine($@"DwmRegisterThumbnail 失败: HRESULT=0x{hr:X}");
        }
    }

    private void UnregisterThumbnail()
    {
        if (_thumbnailRegistered && _thumbnailId != IntPtr.Zero)
        {
            DwmApi.DwmUnregisterThumbnail(_thumbnailId);
            _thumbnailId = IntPtr.Zero;
            _thumbnailRegistered = false;
        }
        _sourceHwnd = IntPtr.Zero;
    }

    // ──────────────────────────────────────────────
    //  更新缩略图位置 & 大小
    // ──────────────────────────────────────────────
    internal void UpdateThumbnail()
    {
        if (!_thumbnailRegistered || _thumbnailId == IntPtr.Zero || _windowHwnd == IntPtr.Zero)
            return;

        if (_topLevel == null)
            return;

        try
        {
            // 1. 计算缩放因子 (DPI)
            double scale = _topLevel.RenderScaling; // e.g. 1.0, 1.5, 2.0

            // 2. 控件在窗口客户区中的像素坐标
            //    PointToScreen(0,0)   → 控件左上角屏幕坐标
            //    topLevel.PointToScreen(0,0) → 窗口客户区左上角屏幕坐标
            //    二者之差即为控件在窗口中的客户区偏移
            var controlScreenPos = this.PointToScreen(new Point(0, 0));
            var windowClientScreenPos = _topLevel.PointToScreen(new Point(0, 0));

            int destLeft   = (int)Math.Round((controlScreenPos.X - windowClientScreenPos.X) * scale);
            int destTop    = (int)Math.Round((controlScreenPos.Y - windowClientScreenPos.Y) * scale);
            int destRight  = destLeft + (int)Math.Round(Bounds.Width * scale);
            int destBottom = destTop  + (int)Math.Round(Bounds.Height * scale);

            if (destRight <= destLeft) destRight = destLeft + 1;
            if (destBottom <= destTop) destBottom = destTop + 1;

            var props = new DwmApi.DWM_THUMBNAIL_PROPERTIES
            {
                dwFlags = DwmApi.DWM_TNP_VISIBLE |
                          DwmApi.DWM_TNP_RECTDESTINATION |
                          DwmApi.DWM_TNP_OPACITY |
                          DwmApi.DWM_TNP_SOURCECLIENTAREAONLY,
                fVisible = true,
                fSourceClientAreaOnly = ClientAreaOnly,
                opacity = ThumbnailOpacity,
                rcDestination = new DwmApi.RECT
                {
                    left   = destLeft,
                    top    = destTop,
                    right  = destRight,
                    bottom = destBottom,
                },
            };

            DwmApi.DwmUpdateThumbnailProperties(_thumbnailId, ref props);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"UpdateThumbnail 出错: {ex}");
        }
    }
}
