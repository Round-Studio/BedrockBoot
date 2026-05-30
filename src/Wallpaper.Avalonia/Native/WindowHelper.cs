using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Wallpaper.Avalonia.Native;

/// <summary>
/// 窗口管理相关的 Win32 P/Invoke 封装。
/// </summary>
[SupportedOSPlatform("windows")]
internal static class WindowHelper
{
    // ──────────────────────────────────────────────
    //  窗口样式常量
    // ──────────────────────────────────────────────
    public const int GWL_EXSTYLE     = -20;
    public const int GWL_STYLE       = -16;

    public const long WS_EX_TOOLWINDOW   = 0x00000080L;
    public const long WS_EX_NOACTIVATE   = 0x08000000L;
    public const long WS_EX_APPWINDOW    = 0x00040000L;
    public const long WS_EX_TRANSPARENT  = 0x00000020L;
    public const long WS_POPUP           = 0x80000000L;
    public const long WS_VISIBLE         = 0x10000000L;

    // ──────────────────────────────────────────────
    //  SetWindowPos 常量
    // ──────────────────────────────────────────────
    public const int SWP_NOMOVE       = 0x0002;
    public const int SWP_NOSIZE       = 0x0001;
    public const int SWP_NOZORDER     = 0x0004;
    public const int SWP_NOACTIVATE   = 0x0010;
    public const int SWP_SHOWWINDOW   = 0x0040;
    public const int SWP_NOOWNERZORDER = 0x0200;

    public static readonly IntPtr HWND_BOTTOM   = new(1);
    public static readonly IntPtr HWND_NOTOPMOST = new(-2);
    public static readonly IntPtr HWND_TOP       = IntPtr.Zero;

    // ──────────────────────────────────────────────
    //  ShowWindow 常量
    // ──────────────────────────────────────────────
    public const int SW_HIDE           = 0;
    public const int SW_SHOWNOACTIVATE = 4;
    public const int SW_SHOW           = 5;
    public const int SW_MINIMIZE       = 6;
    public const int SW_RESTORE        = 9;

    // ──────────────────────────────────────────────
    //  DWM 圆角属性 (Win11)
    // ──────────────────────────────────────────────
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_DEFAULT     = 0;
    private const int DWMWCP_DONOTROUND  = 1;
    private const int DWMWCP_ROUND       = 2;
    private const int DWMWCP_ROUNDSMALL  = 3;

    // ──────────────────────────────────────────────
    //  P/Invoke
    // ──────────────────────────────────────────────
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetWindowLongPtrW(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy,
        uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd, int dwAttribute,
        ref int pvAttribute, int cbAttribute);

    // ──────────────────────────────────────────────
    //  结构体
    // ──────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly int Width  => Right - Left;
        public readonly int Height => Bottom - Top;
    }

    // ──────────────────────────────────────────────
    //  公开方法
    // ──────────────────────────────────────────────
    private static readonly bool _isWindows11 = DetectWindows11();

    private static bool DetectWindows11()
    {
        var v = Environment.OSVersion.Version;
        return v.Major >= 10 && v.Build >= 22000;
    }

    /// <summary>
    /// 将窗口置于目标窗口的正下方（Z序）。
    /// </summary>
    public static void PlaceBelow(IntPtr hWnd, IntPtr hWndOwner)
    {
        if (hWnd == IntPtr.Zero || hWndOwner == IntPtr.Zero)
            return;

        // 将 hWnd 放在 hWndOwner 之后 → Z序上位于其下方
        SetWindowPos(
            hWnd, hWndOwner,
            0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
    }

    /// <summary>
    /// 将窗口移动到指定位置和大小（屏幕坐标）。
    /// </summary>
    public static void MoveWindow(IntPtr hWnd, int x, int y, int width, int height)
    {
        if (hWnd == IntPtr.Zero) return;

        SetWindowPos(
            hWnd, IntPtr.Zero,
            x, y, width, height,
            SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
    }

    /// <summary>
    /// 根据当前操作系统版本设置 DWM 圆角属性。
    /// Win11 → 启用圆角；Win10 → 禁用圆角。
    /// </summary>
    public static void ApplySystemCornerPreference(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return;

        int preference = _isWindows11 ? DWMWCP_ROUND : DWMWCP_DONOTROUND;
        _ = DwmSetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE,
            ref preference, sizeof(int));
    }

    public static bool IsWindows11() => _isWindows11;
}
