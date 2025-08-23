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

            public bool ContainsInSafeArea(Point point, int margin)
            {
                return point.x >= Left + margin &&
                       point.x <= Right - margin &&
                       point.y >= Top + margin &&
                       point.y <= Bottom - margin;
            }

            public Rect GetSafeArea(int margin)
            {
                return new Rect
                {
                    Left = Left + margin,
                    Top = Top + margin,
                    Right = Right - margin,
                    Bottom = Bottom - margin
                };
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Point
        {
            public int x;
            public int y;
        }

        private static bool isRunning = false;
        public static ConcurrentBag<string> targetWindowNames = new ConcurrentBag<string>();

        private static Thread mouseControlThread;
        private static ManualResetEvent stopEvent = new ManualResetEvent(false);
        private static Rect currentWindowRect;

        public static void StartMouseLock()
        {
            if (isRunning) return;

            isRunning = true;
            stopEvent.Reset();
            mouseControlThread = new Thread(MouseControlWorker);
            mouseControlThread.IsBackground = true;
            mouseControlThread.Start();

            Console.WriteLine("鼠标锁定已启动");
        }

        public static void StopMouseLock()
        {
            if (!isRunning) return;

            isRunning = false;
            stopEvent.Set();
            mouseControlThread?.Join(100); // 等待1秒线程退出

            // 恢复鼠标显示和光标限制
            ShowCursor(true);
            ClipCursor(IntPtr.Zero);

            Console.WriteLine("鼠标锁定已停止");
        }

        public static bool GetRunningState()
        {
            return isRunning;
        }

        private static void MouseControlWorker()
        {
            while (!stopEvent.WaitOne(0))
            {
                try
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
                        // 使用当前的 BORDER_MARGIN 值
                        if (!currentWindowRect.ContainsInSafeArea(cursorPos, BORDER_MARGIN))
                        {
                            Point constrainedPos = ConstrainCursorToSafeArea(cursorPos, currentWindowRect, BORDER_MARGIN);
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
                }
                catch
                {

                }
            }

            // 线程结束时恢复鼠标状态
            ShowCursor(true);
            ClipCursor(IntPtr.Zero);
        }

        private static Point ConstrainCursorToSafeArea(Point cursorPos, Rect windowRect, int margin)
        {
            Point constrainedPos = cursorPos;
            Rect safeArea = windowRect.GetSafeArea(margin);

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
                // 如果没有目标窗口，直接返回false
                if (targetWindowNames.IsEmpty)
                    return false;

                System.Text.StringBuilder windowText = new System.Text.StringBuilder(MAX_WINDOW_TEXT);
                if (GetWindowText(hWnd, windowText, MAX_WINDOW_TEXT) > 0)
                {
                    string currentWindowName = windowText.ToString();

                    // 检查当前窗口标题是否包含列表中的任何一个字符串
                    foreach (var targetName in targetWindowNames)
                    {
                        if (!string.IsNullOrEmpty(targetName) &&
                            currentWindowName.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0)
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

        // 添加一些辅助方法
        public static void AddTargetWindow(string windowName)
        {
            if (!string.IsNullOrEmpty(windowName) && !targetWindowNames.Contains(windowName))
            {
                targetWindowNames.Add(windowName);
                Console.WriteLine($"已添加目标窗口: {windowName}");
            }
        }

        public static void RemoveTargetWindow(string windowName)
        {
            if (!string.IsNullOrEmpty(windowName))
            {
                var newList = new ConcurrentBag<string>();
                foreach (var name in targetWindowNames)
                {
                    if (!name.Equals(windowName, StringComparison.OrdinalIgnoreCase))
                    {
                        newList.Add(name);
                    }
                }
                targetWindowNames = newList;
                Console.WriteLine($"已移除目标窗口: {windowName}");
            }
        }

        public static void ClearTargetWindows()
        {
            targetWindowNames = new ConcurrentBag<string>();
            Console.WriteLine("已清空所有目标窗口");
        }

        public static void ListTargetWindows()
        {
            Console.WriteLine("当前目标窗口列表:");
            if (targetWindowNames.IsEmpty)
            {
                Console.WriteLine("  (空)");
            }
            else
            {
                foreach (var name in targetWindowNames)
                {
                    Console.WriteLine($"  - {name}");
                }
            }
        }
    }
}