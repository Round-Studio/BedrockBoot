using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace Wallpaper.Avalonia.Native;

/// <summary>
/// 查找"桌面图标窗口"(Progman / WorkerW 中含有 SHELLDLL_DefView 的那一层)。
/// 对应 C++ 版 FindDesktopIconWindow()。
/// </summary>
[SupportedOSPlatform("windows")]
internal static class DesktopWindowFinder
{
    private const uint SMTO_NORMAL = 0x0000;
    private const uint PM_REMOVE   = 1;
    private const uint WM_DWM_SENDTHUMBNAIL = 0x052C;

    // ──────────────────────────────────────────────
    //  Win32 P/Invoke
    // ──────────────────────────────────────────────
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowW(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetClassNameW(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeoutW(
        IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam,
        uint flags, uint timeout, out UIntPtr pdwResult);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessage(
        out MSG lpMsg, IntPtr hWnd,
        uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr   hwnd;
        public uint     message;
        public UIntPtr  wParam;
        public IntPtr   lParam;
        public uint     time;
        public int      ptX;
        public int      ptY;
    }

    // ──────────────────────────────────────────────
    //  查找逻辑
    // ──────────────────────────────────────────────
    /// <summary>
    /// 查找承载桌面壁纸和图标的最顶层窗口句柄。
    /// </summary>
    public static IntPtr FindDesktopIconWindow()
    {
        // 方案A: 直接在 Progman 的子窗口中找 SHELLDLL_DefView
        IntPtr hwndProgman = FindWindowW("Progman", null);
        if (hwndProgman != IntPtr.Zero)
        {
            IntPtr defView = IntPtr.Zero;
            EnumChildWindows(hwndProgman, (hwnd, _) =>
            {
                if (GetClassName(hwnd) == "SHELLDLL_DefView")
                {
                    defView = hwnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            if (defView != IntPtr.Zero)
                return hwndProgman;
        }

        // 方案B: 发送 0x052C 让 Progman 生成 WorkerW
        if (hwndProgman != IntPtr.Zero)
        {
            SendMessageTimeoutW(hwndProgman, WM_DWM_SENDTHUMBNAIL,
                UIntPtr.Zero, IntPtr.Zero, SMTO_NORMAL, 2000, out _);

            // 让消息队列处理完
            for (int i = 0; i < 20; i++)
            {
                PeekMessage(out _, IntPtr.Zero, 0, 0, PM_REMOVE);
                Thread.Sleep(5);
            }
        }

        // 枚举所有 WorkerW，找含有 SHELLDLL_DefView 的那个
        IntPtr workerWWithIcons = IntPtr.Zero;
        IntPtr firstWorkerW     = IntPtr.Zero;

        EnumWindows((hwnd, _) =>
        {
            if (GetClassName(hwnd) != "WorkerW")
                return true;

            if (firstWorkerW == IntPtr.Zero)
                firstWorkerW = hwnd;

            IntPtr defView = IntPtr.Zero;
            EnumChildWindows(hwnd, (child, _) =>
            {
                if (GetClassName(child) == "SHELLDLL_DefView")
                {
                    defView = child;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            if (defView != IntPtr.Zero)
            {
                workerWWithIcons = hwnd;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        if (workerWWithIcons != IntPtr.Zero)
            return workerWWithIcons;

        if (firstWorkerW != IntPtr.Zero)
            return firstWorkerW;

        return hwndProgman;
    }

    // ──────────────────────────────────────────────
    //  辅助方法
    // ──────────────────────────────────────────────
    private static string GetClassName(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        GetClassNameW(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }
}
