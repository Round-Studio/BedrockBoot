using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace System
{
    public static class VirtualKeyCodes
    {
        public const uint VK_F1 = 0x70;
        public const uint VK_F2 = 0x71;
        public const uint VK_F3 = 0x72;
        public const uint VK_F4 = 0x73;
        public const uint VK_F5 = 0x74;
        public const uint VK_F6 = 0x75;
        public const uint VK_F7 = 0x76;
        public const uint VK_F8 = 0x77;
        public const uint VK_F9 = 0x78;
        public const uint VK_F10 = 0x79;
        public const uint VK_F11 = 0x7A;
        public const uint VK_F12 = 0x7B;

        public const uint VK_A = 0x41;
        public const uint VK_B = 0x42;
        public const uint VK_C = 0x43;
        public const uint VK_D = 0x44;
        public const uint VK_E = 0x45;
        public const uint VK_F = 0x46;
        public const uint VK_G = 0x47;
        public const uint VK_H = 0x48;
        public const uint VK_I = 0x49;
        public const uint VK_J = 0x4A;
        public const uint VK_K = 0x4B;
        public const uint VK_L = 0x4C;
        public const uint VK_M = 0x4D;
        public const uint VK_N = 0x4E;
        public const uint VK_O = 0x4F;
        public const uint VK_P = 0x50;
        public const uint VK_Q = 0x51;
        public const uint VK_R = 0x52;
        public const uint VK_S = 0x53;
        public const uint VK_T = 0x54;
        public const uint VK_U = 0x55;
        public const uint VK_V = 0x56;
        public const uint VK_W = 0x57;
        public const uint VK_X = 0x58;
        public const uint VK_Y = 0x59;
        public const uint VK_Z = 0x5A;

        public const uint VK_0 = 0x30;
        public const uint VK_1 = 0x31;
        public const uint VK_2 = 0x32;
        public const uint VK_3 = 0x33;
        public const uint VK_4 = 0x34;
        public const uint VK_5 = 0x35;
        public const uint VK_6 = 0x36;
        public const uint VK_7 = 0x37;
        public const uint VK_8 = 0x38;
        public const uint VK_9 = 0x39;
    }
    public static class WindowExtensions
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        public static void BringToFront(this Window window)
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            ShowWindow(hwnd, SW_RESTORE);
            SetForegroundWindow(hwnd);
        }
    }
    public static class HotKeyModifiers
    {
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;
    }
    public class HotKeyService : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;

        private readonly Window _window;
        private readonly Dictionary<int, Action> _hotkeyActions = new();
        private int _currentId = 0;
        private bool _disposed = false;
        private IntPtr _originalWndProc;
        private WndProc _newWndProc;

        public HotKeyService(Window window)
        {
            _window = window;
            var hwnd = WindowNative.GetWindowHandle(window);
            SubclassWindow(hwnd);
        }

        public int RegisterHotKey(uint modifier, uint key, Action action)
        {
            var hwnd = WindowNative.GetWindowHandle(_window);
            var id = ++_currentId;

            if (RegisterHotKey(hwnd, id, modifier, key))
            {
                _hotkeyActions[id] = action;
                return id;
            }

            return -1;
        }

        public void UnregisterHotKey(int id)
        {
            if (_hotkeyActions.ContainsKey(id))
            {
                var hwnd = WindowNative.GetWindowHandle(_window);
                UnregisterHotKey(hwnd, id);
                _hotkeyActions.Remove(id);
            }
        }

        private void SubclassWindow(IntPtr hwnd)
        {
            // 创建新的窗口过程委托
            _newWndProc = new WndProc(WindowProc);

            // 存储委托以防止被垃圾回收
            _wndProcDelegate = _newWndProc;

            // 替换窗口过程并保存原始过程
            _originalWndProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
        }

        private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_HOTKEY)
            {
                var id = wParam.ToInt32();
                if (_hotkeyActions.TryGetValue(id, out var action))
                {
                    action.Invoke();
                    return IntPtr.Zero;
                }
            }

            // 调用原始窗口过程
            return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
        }

        #region Native Interop

        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private WndProc _wndProcDelegate;

        [DllImport("user32.dll", EntryPoint = "CallWindowProc", CharSet = CharSet.Unicode)]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Unicode)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private const int GWLP_WNDPROC = -4;

        #endregion

        public void Dispose()
        {
            
        }
    }
}