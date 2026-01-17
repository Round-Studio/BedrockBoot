using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace BedrockBoot.Models.Helper;

public class ProcessMouseLocker
{
    // 导入WinAPI函数
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

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

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out Rect pvAttribute, int cbAttribute);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // DWM属性常量
    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    // 常量和结构体
    private const int MAX_WINDOW_TEXT = 256;
    private const int MAX_CLASS_NAME = 256;
    private const uint GA_ROOT = 2; // GetRootWindow

    public int BorderMargin { get; set; } = 20; // 边界内边距20px

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
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
    public struct Point
    {
        public int x;
        public int y;
    }

    private readonly int _processId;
    private readonly string _versionName;
    private readonly long _targetTimestamp;
    private bool _isRunning = false;
    private Thread _mouseControlThread;
    private ManualResetEvent _stopEvent = new ManualResetEvent(false);
    private Rect _currentWindowRect;
    private IntPtr _targetWindowHandle = IntPtr.Zero;
    
    // 用于存储窗口信息
    private class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public uint ProcessId { get; set; }
        public string Title { get; set; }
        public string ClassName { get; set; }
        public long? Timestamp { get; set; }
        public long TimestampDifference { get; set; }
    }

    public ProcessMouseLocker(int processId, string versionName = null, long targetTimestamp = 0)
    {
        _processId = processId;
        _versionName = versionName;
        _targetTimestamp = targetTimestamp;
    }

    public void StartMouseLock()
    {
        if (_isRunning) return;

        // 尝试找到目标窗口
        FindTargetWindow();

        if (_targetWindowHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"无法找到进程ID为 {_processId} 的窗口");
        }

        _isRunning = true;
        _stopEvent.Reset();
        _mouseControlThread = new Thread(MouseControlWorker);
        _mouseControlThread.IsBackground = true;
        _mouseControlThread.Start();

        Console.WriteLine($"鼠标锁定已启动，目标进程ID: {_processId}, 窗口句柄: {_targetWindowHandle}");
    }

    public void StartMouseLockWithWait(TimeSpan timeout)
    {
        if (_isRunning) return;

        // 等待窗口出现
        if (!WaitForWindow(timeout))
        {
            throw new TimeoutException($"在 {timeout.TotalSeconds} 秒内未能找到进程ID为 {_processId} 的窗口");
        }

        _isRunning = true;
        _stopEvent.Reset();
        _mouseControlThread = new Thread(MouseControlWorker);
        _mouseControlThread.IsBackground = true;
        _mouseControlThread.Start();

        Console.WriteLine($"鼠标锁定已启动，目标进程ID: {_processId}, 窗口句柄: {_targetWindowHandle}");
    }

    public void StopMouseLock()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _stopEvent.Set();
        _mouseControlThread?.Join(100); // 等待1秒线程退出

        // 恢复鼠标显示和光标限制
        ShowCursor(true);
        ClipCursor(IntPtr.Zero);

        Console.WriteLine("鼠标锁定已停止");
    }

    public bool GetRunningState()
    {
        return _isRunning;
    }

    public IntPtr GetTargetWindowHandle()
    {
        return _targetWindowHandle;
    }

    private void MouseControlWorker()
    {
        while (!_stopEvent.WaitOne(0))
        {
            try
            {
                IntPtr foregroundWindow = GetForegroundWindow();

                // 检查当前前台窗口是否属于目标进程
                if (IsTargetProcessWindow(foregroundWindow) && _targetWindowHandle != IntPtr.Zero)
                {
                    // 隐藏鼠标
                    ShowCursor(false);

                    // 获取窗口位置和大小，优先使用DWM获取扩展帧边界
                    if (!TryGetWindowBounds(_targetWindowHandle, out _currentWindowRect))
                    {
                        // 如果DWM获取失败，回退到传统方法
                        GetWindowRect(_targetWindowHandle, out _currentWindowRect);
                    }

                    // 获取当前鼠标位置
                    Point cursorPos;
                    GetCursorPos(out cursorPos);

                    // 检查鼠标是否在安全区域内，如果不在则移动到最近的安全位置
                    if (!_currentWindowRect.ContainsInSafeArea(cursorPos, BorderMargin))
                    {
                        Point constrainedPos = ConstrainCursorToSafeArea(cursorPos, _currentWindowRect, BorderMargin);
                        SetCursorPos(constrainedPos.x, constrainedPos.y);
                    }

                    // 限制光标在窗口范围内
                    ClipCursor(ref _currentWindowRect);
                }
                else
                {
                    // 如果不是目标窗口，恢复鼠标显示和光标自由
                    ShowCursor(true);
                    ClipCursor(IntPtr.Zero);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"鼠标控制工作线程发生异常: {ex.Message}");
            }
        }

        // 线程结束时恢复鼠标状态
        ShowCursor(true);
        ClipCursor(IntPtr.Zero);
    }

    private bool TryGetWindowBounds(IntPtr hWnd, out Rect bounds)
    {
        bounds = new Rect();
        try
        {
            int result = DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out bounds, Marshal.SizeOf(bounds));
            return result == 0; // S_OK
        }
        catch (Exception)
        {
            return false;
        }
    }

    private Point ConstrainCursorToSafeArea(Point cursorPos, Rect windowRect, int margin)
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

    private bool IsTargetProcessWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
            return false;

        // 获取窗口所属的进程ID
        GetWindowThreadProcessId(windowHandle, out uint processId);

        return processId == _processId;
    }

    private void FindTargetWindow()
    {
        _targetWindowHandle = IntPtr.Zero;

        // 枚举所有可见窗口
        EnumWindows(new EnumWindowsProc((hWnd, lParam) =>
        {
            Thread.Sleep(1);
            // 检查窗口是否可见
            if (!IsWindowVisible(hWnd))
                return true;

            // 获取窗口所属的进程ID
            GetWindowThreadProcessId(hWnd, out uint processId);

            // 检查是否为目标进程ID
            if (processId == _processId)
            {
                // 获取窗口信息
                var windowInfo = GetWindowInfo(hWnd, processId);
                
                // 检查窗口是否符合条件
                if (IsSuitableWindow(windowInfo))
                {
                    _targetWindowHandle = windowInfo.Handle;
                    Console.WriteLine($"找到候选窗口: 句柄={hWnd}, 进程ID={processId}, 标题={windowInfo.Title}, 时间戳={windowInfo.Timestamp}, 差值={windowInfo.TimestampDifference}");
                }
            }

            return true;
        }), IntPtr.Zero);

        if (_targetWindowHandle != IntPtr.Zero)
        {
            Console.WriteLine($"已选择目标窗口: 句柄={_targetWindowHandle}");
        }
    }

    private WindowInfo GetWindowInfo(IntPtr hWnd, uint processId)
    {
        var windowInfo = new WindowInfo
        {
            Handle = hWnd,
            ProcessId = processId
        };

        // 获取窗口类名
        var classNameBuilder = new System.Text.StringBuilder(MAX_CLASS_NAME);
        GetClassName(hWnd, classNameBuilder, MAX_CLASS_NAME);
        windowInfo.ClassName = classNameBuilder.ToString();

        // 获取窗口标题
        var windowTextBuilder = new System.Text.StringBuilder(MAX_WINDOW_TEXT);
        GetWindowText(hWnd, windowTextBuilder, MAX_WINDOW_TEXT);
        windowInfo.Title = windowTextBuilder.ToString();
        
        // 计算时间戳差值
        if (windowInfo.Timestamp.HasValue && _targetTimestamp > 0)
        {
            windowInfo.TimestampDifference = Math.Abs(windowInfo.Timestamp.Value - _targetTimestamp);
        }
        else
        {
            windowInfo.TimestampDifference = long.MaxValue;
        }

        return windowInfo;
    }

    private bool IsSuitableWindow(WindowInfo windowInfo)
    {
        // 排除明显的非游戏窗口
        if (!IsGameWindow(windowInfo.ClassName))
            return false;

        // 如果有版本名称要求，检查窗口标题是否包含版本名称
        if (!string.IsNullOrEmpty(_versionName))
        {
            // 检查窗口标题是否包含版本名称
            if (!windowInfo.Title.Contains(_versionName, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private bool IsGameWindow(string className)
    {
        // 排除明显的非游戏窗口
        if (className.Equals("ConsoleWindowClass", StringComparison.OrdinalIgnoreCase) ||
            className.Equals("Console Host", StringComparison.OrdinalIgnoreCase) ||
            className.Equals("MSCTFIME UI", StringComparison.OrdinalIgnoreCase) ||
            className.StartsWith("IME", StringComparison.OrdinalIgnoreCase) ||
            className.StartsWith("TOOLTIP", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 接受大部分窗口，特别是UWP相关的
        if (className.Contains("ApplicationFrameWindow") ||
            className.Contains("Windows.UI.Core.CoreWindow") ||
            className.Contains("Xaml_Window") ||
            className.Contains("Minecraft") ||
            className.Length > 0) // 接受有类名的窗口
        {
            return true;
        }

        return false;
    }

    public void RefreshTargetWindow()
    {
        FindTargetWindow();
    }

    public bool IsTargetWindowValid()
    {
        return _targetWindowHandle != IntPtr.Zero;
    }

    public Rect GetCurrentWindowRect()
    {
        return _currentWindowRect;
    }

    private bool WaitForWindow(TimeSpan timeout)
    {
        DateTime startTime = DateTime.Now;

        while (DateTime.Now - startTime < timeout)
        {
            FindTargetWindow();

            if (_targetWindowHandle != IntPtr.Zero)
            {
                Console.WriteLine($"窗口已找到，PID：{_processId}");
                return true;
            }

            // 短暂等待后再次检查
            Thread.Sleep(500);
        }

        return false;
    }
}