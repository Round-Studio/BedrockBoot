using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Classes.Helper
{
    class MouseHelper
    {
        // 导入WinAPI函数
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool ClipCursor(ref Rect lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClipCursor(IntPtr lpRect);

        [DllImport("user32.dll")]
        private static extern bool ShowCursor(bool bShow);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);

        // 常量和结构体
        private const int MAX_WINDOW_TEXT = 256;
        public static int BORDER_MARGIN = 20; // 边界内边距20px

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;

            public bool ContainsInSafeArea(Point point)
            {
                return point.x >= Left + BORDER_MARGIN &&
                       point.x <= Right - BORDER_MARGIN &&
                       point.y >= Top + BORDER_MARGIN &&
                       point.y <= Bottom - BORDER_MARGIN;
            }

            public Rect GetSafeArea()
            {
                return new Rect
                {
                    Left = Left + BORDER_MARGIN,
                    Top = Top + BORDER_MARGIN,
                    Right = Right - BORDER_MARGIN,
                    Bottom = Bottom - BORDER_MARGIN
                };
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Point
        {
            public int x;
            public int y;
        }

        private static bool isEnabled = true;
        public static ConcurrentBag<string> targetWindowNames = new ConcurrentBag<string>
        { };

        private static Thread mouseControlThread;
        private static ManualResetEvent stopEvent = new ManualResetEvent(false);
        private static Rect currentWindowRect;

        public static void StartMouseLock()
        {
            stopEvent.Reset();
            mouseControlThread = new Thread(MouseControlWorker);
            mouseControlThread.IsBackground = true;
            mouseControlThread.Start();
        }

        public static void StopMouseLock()
        {
            stopEvent.Set();
            mouseControlThread?.Join();

            // 恢复鼠标显示和光标限制
            ShowCursor(true);
            ClipCursor(IntPtr.Zero);
        }
        public static bool GetRunningState()
        {
            return isEnabled;
        }
        private static void MouseControlWorker()
        {
            while (!stopEvent.WaitOne(0))
            {
                IntPtr foregroundWindow = GetForegroundWindow();
                if (foregroundWindow != IntPtr.Zero && IsTargetWindow(foregroundWindow))
                {
                    // 隐藏鼠标
                    ShowCursor(false);

                    // 获取窗口位置和大小
                    GetWindowRect(foregroundWindow, out currentWindowRect);

                    // 获取当前鼠标位置
                    Point cursorPos;
                    GetCursorPos(out cursorPos);

                    // 检查鼠标是否在安全区域内，如果不在则移动到最近的安全位置
                    if (!currentWindowRect.ContainsInSafeArea(cursorPos))
                    {
                        Point constrainedPos = ConstrainCursorToSafeArea(cursorPos, currentWindowRect);
                        SetCursorPos(constrainedPos.x, constrainedPos.y);
                    }

                    // 限制光标在窗口范围内
                    ClipCursor(ref currentWindowRect);
                }
                else
                {
                    // 如果不是目标窗口，恢复鼠标显示和光标自由
                    ShowCursor(true);
                    ClipCursor(IntPtr.Zero);
                }

                Thread.Sleep(10);
            }

            // 线程结束时恢复鼠标状态
            ShowCursor(true);
            ClipCursor(IntPtr.Zero);
        }

        private static Point ConstrainCursorToSafeArea(Point cursorPos, Rect windowRect)
        {
            Point constrainedPos = cursorPos;
            Rect safeArea = windowRect.GetSafeArea();

            // 水平方向约束到安全区域
            if (cursorPos.x < safeArea.Left)
            {
                constrainedPos.x = safeArea.Left;
            }
            else if (cursorPos.x > safeArea.Right)
            {
                constrainedPos.x = safeArea.Right;
            }

            // 垂直方向约束到安全区域
            if (cursorPos.y < safeArea.Top)
            {
                constrainedPos.y = safeArea.Top;
            }
            else if (cursorPos.y > safeArea.Bottom)
            {
                constrainedPos.y = safeArea.Bottom;
            }

            return constrainedPos;
        }

        private static bool IsTargetWindow(IntPtr hWnd)
        {
            try
            {
                System.Text.StringBuilder windowText = new System.Text.StringBuilder(MAX_WINDOW_TEXT);
                if (GetWindowText(hWnd, windowText, MAX_WINDOW_TEXT) > 0)
                {
                    string currentWindowName = windowText.ToString();

                    // 检查当前窗口标题是否包含列表中的任何一个字符串
                    foreach (var targetName in targetWindowNames)
                    {
                        if (currentWindowName.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}