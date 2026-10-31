/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Runtime.InteropServices;
using BedrockBoot.Standard.Interface.Platform;

namespace BedrockBoot.Windows.Models.Helper.Tray;

public class TrayHelper : ITrayService
{
    private const string OwnerClassName = "BedrockBootTrayOwner";
    private const string ToolTip = "BedrockBoot";

    private const uint NIM_ADD = 0x0;
    private const uint NIM_MODIFY = 0x1;
    private const uint NIF_ICON = 0x2;
    private const uint NIF_TIP = 0x4;
    private const uint NIF_INFO = 0x10;
    private const uint NIIF_INFO = 0x1;
    private const uint IMAGE_ICON = 1;
    private const int SM_CXSMICON = 49;
    private const int SM_CYSMICON = 50;

    private static readonly object Sync = new();
    private static readonly WndProcDelegate WndProcKeepAlive = WndProc;
    private static IntPtr _ownerWindow;
    private static IntPtr _iconHandle;
    private static bool _initialized;
    private static bool _added;

    public void Initialize()
    {
        lock (Sync)
        {
            if (_initialized) return;
            _initialized = true;

            _ownerWindow = CreateOwnerWindow();
            if (_ownerWindow == IntPtr.Zero)
            {
                Console.WriteLine(@"创建托盘图标失败：无法创建承载窗口");
                return;
            }

            _iconHandle = LoadTrayIcon();
            if (_iconHandle == IntPtr.Zero) Console.WriteLine(@"托盘图标为空图标");

            var data = CreateData();
            data.uFlags = NIF_ICON | NIF_TIP;
            data.szTip = ToolTip;

            _added = Shell_NotifyIcon(NIM_ADD, ref data);
            if (_added)
                Console.WriteLine(@"托盘图标已创建");
            else
                Console.WriteLine(@"创建托盘图标失败");
        }
    }

    public void ShowNotification(string title, string message)
    {
        lock (Sync)
        {
            if (!_added)
            {
                Console.WriteLine(@"托盘图标尚未创建，无法显示通知");
                return;
            }

            var data = CreateData();
            data.uFlags = NIF_INFO;
            data.szInfoTitle = Limit(title, 63);
            data.szInfo = Limit(message, 255);
            data.dwInfoFlags = NIIF_INFO;

            if (!Shell_NotifyIcon(NIM_MODIFY, ref data))
                Console.WriteLine(@"显示托盘通知失败");
            else
                Console.WriteLine(@"已通过托盘显示通知");
        }
    }

    private static string Limit(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static NOTIFYICONDATAW CreateData() => new()
    {
        cbSize = Marshal.SizeOf<NOTIFYICONDATAW>(),
        hWnd = _ownerWindow,
        uID = 0,
        hIcon = _iconHandle,
        szTip = string.Empty,
        szInfo = string.Empty,
        szInfoTitle = string.Empty
    };

    private static IntPtr CreateOwnerWindow()
    {
        var moduleHandle = GetModuleHandle(null);

        var windowClass = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WndProcKeepAlive),
            hInstance = moduleHandle,
            lpszClassName = OwnerClassName
        };

        if (RegisterClassEx(ref windowClass) == 0)
            Console.WriteLine(@"注册托盘承载窗口类失败");

        return CreateWindowEx(0, OwnerClassName, OwnerClassName, 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero,
            moduleHandle, IntPtr.Zero);
    }

    private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        => DefWindowProc(hWnd, msg, wParam, lParam);

    private static IntPtr LoadTrayIcon()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            Console.WriteLine(@"无法获取进程路径，托盘图标将使用系统默认图标");
            return IntPtr.Zero;
        }

        var icons = new IntPtr[1];
        if (ExtractIconEx(exePath, 0, null, icons, 1) > 0 && icons[0] != IntPtr.Zero)
            return icons[0];

        var handle = LoadImage(IntPtr.Zero, exePath, IMAGE_ICON, GetSystemMetrics(SM_CXSMICON),
            GetSystemMetrics(SM_CYSMICON), 0);
        if (handle == IntPtr.Zero) Console.WriteLine($@"加载托盘图标失败：{exePath}");
        return handle;
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEXW
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpszMenuName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;

        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATAW
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;

        public uint dwState;
        public uint dwStateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;

        public uint uTimeoutOrVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;

        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATAW lpData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEXW lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired,
        int cyDesired, uint fuLoad);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[]? phiconLarge,
        IntPtr[]? phiconSmall, uint nIcons);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
