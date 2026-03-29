using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Diagnostics;
using BedrockBoot.Models.Helper.Notice;

namespace BedrockBoot.Models.Helper;

public class ProcessMouseLocker
{
    // --- Win32 API 导入 ---
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool ClipCursor(ref Rect lpRect);

    [DllImport("user32.dll")]
    private static extern bool ClipCursor(IntPtr lpRect);

    [DllImport("user32.dll")]
    private static extern bool ShowCursor(bool bShow);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out Rect pvAttribute, int cbAttribute);
    
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // --- 常量定义 ---
    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12; // Alt 键

    // --- 成员变量 ---
    private readonly int _targetPid;
    private readonly DateTime _targetStartTime;
    private IntPtr _targetHwnd = IntPtr.Zero;
    private bool _isRunning = false;
    private bool _isManuallyUnlocked = false; 
    private bool _wasWindowFound = false; // 用于控制通知只发一次
    private Thread _monitorThread;

    public int BorderMargin { get; set; } = 20;

    public ProcessMouseLocker(int processId)
    {
        _targetPid = processId;
        try
        {
            using var p = Process.GetProcessById(processId);
            _targetStartTime = p.StartTime;
        }
        catch 
        { 
            _targetStartTime = DateTime.Now; 
        }
    }

    /// <summary>
    /// 开启鼠标锁定监控逻辑
    /// </summary>
    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _wasWindowFound = false;
        _targetHwnd = IntPtr.Zero;

        _monitorThread = new Thread(MonitorLoop) 
        { 
            IsBackground = true, 
            Name = "MouseLockerMonitor" 
        };
        _monitorThread.Start();
    }

    /// <summary>
    /// 停止监控并释放鼠标
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        UnlockMouseInternal();
    }

    private void MonitorLoop()
    {
        while (_isRunning)
        {
            // 1. 热键检测 (Ctrl + Alt)
            if (IsHotkeyPressed())
            {
                if (!_isManuallyUnlocked)
                {
                    _isManuallyUnlocked = true;
                    UnlockMouseInternal();
                    Thread.Sleep(300); // 防止热键重复触发
                }
            }

            // 2. 窗口有效性检查与持续搜索
            bool isCurrentWindowValid = _targetHwnd != IntPtr.Zero && IsWindowVisible(_targetHwnd);
            
            if (!isCurrentWindowValid)
            {
                _targetHwnd = SearchForTargetWindow();
                
                if (_targetHwnd != IntPtr.Zero)
                {
                    // 刚找到窗口时发送通知
                    if (!_wasWindowFound)
                    {
                        try {
                            // 调用你的通知组件
                            NoticeHelper.SentNotice("游戏已捕获", "鼠标锁定已就绪 (Ctrl+Alt 解锁 或 Win 解锁)");
                        } catch { }
                        _wasWindowFound = true;
                    }
                }
                else
                {
                    // 没找到窗口，重置状态并继续等待
                    _wasWindowFound = false;
                    UnlockMouseInternal();
                    Thread.Sleep(500); 
                    continue;
                }
            }

            // 3. 焦点判断与锁定逻辑
            IntPtr foregroundHwnd = GetForegroundWindow();
            bool isOurWindowFocused = IsWindowBelongsToTarget(foregroundHwnd);

            if (isOurWindowFocused)
            {
                // 如果回到了游戏窗口，自动恢复锁定状态
                if (_isManuallyUnlocked)
                {
                    _isManuallyUnlocked = false;
                }

                LockMouseInternal();
            }
            else
            {
                // 窗口失去焦点（Alt+Tab等），自动释放鼠标
                UnlockMouseInternal();
            }

            Thread.Sleep(15); // 约 60Hz 频率检查
        }
    }

    private bool IsHotkeyPressed()
    {
        return (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0 && 
               (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
    }

    private void LockMouseInternal()
    {
        if (_isManuallyUnlocked) return;

        ShowCursor(false);
        if (TryGetWindowBounds(_targetHwnd, out Rect rect))
        {
            ClipCursor(ref rect);
        }
    }

    private void UnlockMouseInternal()
    {
        ShowCursor(true);
        ClipCursor(IntPtr.Zero);
    }

    private bool IsWindowBelongsToTarget(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        if (hwnd == _targetHwnd) return true;

        GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == _targetPid) return true;

        // 处理 UWP 框架窗口焦点判定
        return CheckIfUwpFrameMatches(hwnd);
    }

    private IntPtr SearchForTargetWindow()
    {
        IntPtr foundHandle = IntPtr.Zero;

        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindowVisible(hWnd)) return true;

            StringBuilder sbClass = new StringBuilder(256);
            GetClassName(hWnd, sbClass, 256);
            string className = sbClass.ToString();

            // 排除控制台窗口，防止误锁
            if (className.Contains("ConsoleWindowClass") || className.Contains("Ghost")) 
                return true;

            GetWindowThreadProcessId(hWnd, out uint pid);

            // 路径 1: GDK/普通窗口直接匹配 PID
            if (pid == _targetPid)
            {
                foundHandle = hWnd;
                return false;
            }

            // 路径 2: UWP 窗口 (ApplicationFrameHost.exe)
            if (className == "ApplicationFrameWindow")
            {
                if (CheckIfUwpFrameMatches(hWnd))
                {
                    foundHandle = hWnd;
                    return false;
                }
            }

            return true;
        }, IntPtr.Zero);

        return foundHandle;
    }

    private bool CheckIfUwpFrameMatches(IntPtr frameHwnd)
    {
        bool isMatch = false;

        // 核心：检查 Frame 内部是否包含目标进程的子窗口
        EnumChildWindows(frameHwnd, (childHwnd, l) =>
        {
            GetWindowThreadProcessId(childHwnd, out uint childPid);
            if (childPid == _targetPid)
            {
                isMatch = true;
                return false;
            }
            return true;
        }, IntPtr.Zero);

        // 辅助：如果子窗口枚举不可用，尝试时间戳关联
        if (!isMatch)
        {
            try
            {
                GetWindowThreadProcessId(frameHwnd, out uint framePid);
                using var p = Process.GetProcessById((int)framePid);
                // 判断 ApplicationFrameHost 启动时间是否与目标应用接近
                double diff = Math.Abs((p.StartTime - _targetStartTime).TotalSeconds);
                if (diff < 10) isMatch = true;
            }
            catch { }
        }

        return isMatch;
    }

    private bool TryGetWindowBounds(IntPtr hWnd, out Rect bounds)
    {
        // 优先使用 DWM 获取视觉边界，避免 ClipCursor 锁定到透明阴影区
        if (DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out bounds, Marshal.SizeOf(typeof(Rect))) == 0)
            return true;

        return GetWindowRect(hWnd, out bounds);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect 
    { 
        public int Left; 
        public int Top; 
        public int Right; 
        public int Bottom; 
    }
}