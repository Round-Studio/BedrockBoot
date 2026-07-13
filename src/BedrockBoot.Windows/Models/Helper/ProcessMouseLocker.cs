using BedrockBoot.Core.Global;
using BedrockBoot.Models.Helper.Notice;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;



namespace BedrockBoot.Models.Helper;

public class ProcessMouseLocker
{
    // --- Win32 API 导入 ---
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();


    /* 
     *    ClipCursor(IntPtr.Zero) 似乎不需要重复执行
     *    这样别的应用万一自带裁剪区域操作，就被我们给干掉了
     *    闹
     */
    [DllImport("user32.dll")]
    private static extern bool ClipCursor(ref Rect lpRect);

    [DllImport("user32.dll")]
    private static extern bool ClipCursor(IntPtr lpRect);

    /* 
     *   ShowCursor 搞不太清楚，
     *   问了一下 GPT，解释是这个并不是布尔开关，
     *   Windows 内部维护：DisplayCounter
     *   例如：初始 = 0
     *   调用：ShowCursor(false);
     *   变成：-1，此时为隐藏
     *   再调用一次：ShowCursor(false);
     *   变成：-2，此时还是隐藏
     *   此时如果调用ShowCursor(true);
     *   变成：-1，此时仍然隐藏
    */
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

 

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // --- 常量定义 ---
    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;


    // --- 成员变量 ---
    private readonly int _targetPid;
    private readonly DateTime _targetStartTime;
    private IntPtr _targetHwnd = IntPtr.Zero;
    private bool _isRunning = false;
    private bool _isManuallyUnlocked = false;
    private bool _isMouseCurrentlyLocked;
    private bool _wasWindowFound = false; // 用于控制通知只发一次
    private Thread _monitorHotkeyThread;
    private Thread _monitorForegroundThread;
    private HotKey _hotKey = HotKey.Parse(GlobalModel.Config.Data.MouseLockHotkey);
    private Rect? _lastClipRect;

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


        // 监视热键和监视焦点窗口分为两个，热键需要高频率监视，但是焦点窗口不需要
        _monitorHotkeyThread = new Thread(MonitorHotkeyLoop)
        {
            IsBackground = true,
            Name = "MouseLockerHotkeyMonitor"
        };
        _monitorHotkeyThread.Start();

        _monitorForegroundThread = new Thread(MonitorForegroundLoop)
        {
            IsBackground = true,
            Name = "MouseLockerForegroundMonitor"
        };
        _monitorForegroundThread.Start();
    }

    /// <summary>
    /// 停止监控并释放鼠标
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        UnlockMouseInternal();
    }

    private void MonitorHotkeyLoop()
    {
        while (_isRunning)
        {
            // 1. 热键检测 (Ctrl + Alt)

            // 原来的写法有大问题啊。原来检测到了热键暂时释放了之后，由于检测到了焦点依然在游戏上，所以又给锁上了，并且还标记 _isManuallyUnlocked = false，那这样这个标记还有什么意义，去掉了也没有区别
            // 导致热键必须得一直按住才能临时解锁
            // 而且，热键的 CD 是 300ms，锁上的 CD 是 15ms，就算保持按住热键，移动鼠标的时候还是卡顿的

            // 因为用户解锁鼠标，往往目的是把焦点改变为其他应用，如果在此之后焦点又回来了，就可以自动重新锁上
            // 所以，检测到热键之后，解锁，并且标记 _isManuallyUnlocked 为 true 之后
            // 正确的做法应该是在检测到焦点切换到其他窗口的时候才标记 _isManuallyUnlocked 为 false
            // 当然，如果在解锁期间再按一次热键也可以锁回去（（
            if (IsHotkeyPressed())
            {
                if (!_isManuallyUnlocked)
                {
                    _isManuallyUnlocked = true;
                    UnlockMouseInternal();
                    Thread.Sleep(300); // 防止热键重复触发
                }
                // 如果在临时释放期间再按一次热键，那就再锁回去吧 xwx
                else
                {
                    _isManuallyUnlocked = false;
                    LockMouseInternal();
                    Thread.Sleep(300);
                }
            }
            Thread.Sleep(15); // 约 60Hz 频率检查

        }
    }
    private void MonitorForegroundLoop()
    {
        while (_isRunning)
        {
            

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
                        try
                        {
                            // 调用你的通知组件
                            NoticeHelper.SentNotice("游戏已捕获", "鼠标锁定已就绪 (按 "+ _hotKey.ToString() + " 或 Win 解锁)");
                        }
                        catch { }
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

                // 前面解释了，不应该在这个时候将 _isManuallyUnlocked 赋值为 false
                if (_isManuallyUnlocked && BedrockBoot.Core.Global.GlobalModel.Config.Data.IsMouseLockReserve)
                {
                    _isManuallyUnlocked = false;
                }

                LockMouseInternal();
            }
            else
            {
                // 窗口失去焦点（Alt+Tab等），自动释放鼠标
                UnlockMouseInternal();

                // 正确的将 _isManuallyUnlocked 赋值为 false 的时机应该在这里
                if (_isManuallyUnlocked && !BedrockBoot.Core.Global.GlobalModel.Config.Data.IsMouseLockReserve)
                {
                    _isManuallyUnlocked = false;
                }
            }
            if (BedrockBoot.Core.Global.GlobalModel.Config.Data.IsMouseLockReserve)
            {
                Thread.Sleep(15); // 约 60Hz 频率检查
            }
            else
            {
                // 其实不用那么高频率检查的，占 CPU 是一回事（划掉）
                // 因为 ClipCursor() 之后，除非被你释放了，要不然是一直保持着的（
                // 换句话说，ClipCursor() 的原理是限制光标的移动范围。
                // 范围已被限制的前提下只需低频率检查是否失去焦点来执行一次释放即可。
                // 我们默认没有其他应用和我们抢夺。
                Thread.Sleep(500);


            }
        }
    }

    private bool IsHotkeyPressed()
    {
        return _hotKey.IsPressed();
    }

    private void LockMouseInternal()
    {
        if (_isManuallyUnlocked) return;


        // ShowCursor() 搞不太清楚其实，虽然能跑就别动，但是我还是留了个开关 ，万一出问题了呢
        if (BedrockBoot.Core.Global.GlobalModel.Config.Data.IsMouseLockReserve)
        {
            ShowCursor(false);

        }
        if (TryGetWindowBounds(_targetHwnd, out Rect rect))
        {
			// 本来写了判断游戏窗口是否和监视器的边缘重合的判断
			// 但是想了想还是算了，没啥必要，反正全局抠掉一点点也没啥影响
			if (BedrockBoot.Core.Global.GlobalModel.Config.Data.IsMouseLockFullScreen)
            {
                rect.Left += 2;
                rect.Top += 2;
                rect.Right -= 2;
                rect.Bottom -= 2;
            }
            // 如果裁切不需要改变就不重新进行裁切
            if (_lastClipRect.HasValue && _lastClipRect.Value.Equals(rect) && !BedrockBoot.Core.Global.GlobalModel.Config.Data.IsMouseLockReserve) return;

            ClipCursor(ref rect);
            _lastClipRect = rect;

        }
        _isMouseCurrentlyLocked = true;

    }

    private void UnlockMouseInternal()
    {
        // 如果当前是释放的情况下，就不需要重复多次释放了
        // 否则别的鼠标锁定本来是正常的游戏，例如 GDK 版的 Minecraft，或者原神、PUBG 等，就被我们的反复释放操作给干掉了
        if (!_isMouseCurrentlyLocked && !BedrockBoot.Core.Global.GlobalModel.Config.Data.IsMouseLockReserve) return;


        if (BedrockBoot.Core.Global.GlobalModel.Config.Data.IsMouseLockReserve)
        {
            ShowCursor(true);

        }
        ClipCursor(IntPtr.Zero);
		_lastClipRect = null;
        _isMouseCurrentlyLocked = false;

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
                // 正常情况下 GDK 窗口是不需要锁的呜
                if (BedrockBoot.Core.Global.GlobalModel.Config.Data.IsMouseLockForGdk)
                {
                    foundHandle = hWnd;
                    return false;
                }
                return true;
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