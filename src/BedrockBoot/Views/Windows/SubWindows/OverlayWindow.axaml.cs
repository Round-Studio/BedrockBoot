using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Platform;
using Avalonia.Interactivity;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Models.Global;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BedrockBoot.Views.Windows.SubWindows;

public partial class OverlayWindow : Window
{
    // --- Win32 API 导入 ---
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hwnd, ref RECT rectangle);

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    // 枚举窗口相关API
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder strText, int maxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, ref RECT rect);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT point);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

    // 键盘钩子相关
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    // --- Win32 常量 ---
    private const int GWL_EXSTYLE = -20;
    private const int GWL_STYLE = -16;
    private const int WS_CHILD = 0x40000000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SWP_NOMOVE = 0x0002;
    private const int SWP_NOSIZE = 0x0001;
    private const int SWP_NOZORDER = 0x0004;
    private const int SWP_NOACTIVATE = 0x0010;
    private const int HWND_BOTTOM = 1;
    private const int HWND_TOP = 0;
    private const int HWND_TOPMOST = -1;

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104; // 捕捉带 Alt 的组合键，增强稳定性
    private const int VK_SHIFT = 0x10;
    private const int VK_TAB = 0x09;
    private const int VK_ESCAPE = 0x1B;

    // UWP相关常量
    private const uint GA_ROOT = 2;
    private const uint GA_PARENT = 1;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS
    {
        public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    // --- 成员变量 ---
    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookID = IntPtr.Zero;
    private IntPtr _targetHwnd = IntPtr.Zero;
    private IntPtr _myHandle = IntPtr.Zero;
    private bool _isOverlayVisible = false;
    private bool _isEmbedded = false; // 跟踪是否已内嵌
    private DispatcherTimer? _syncTimer;
    private int _originalWidth = 0;
    private int _originalHeight = 0;
    private int _originalX = 0;
    private int _originalY = 0;
    
    // 动画控制
    private volatile bool _isAnimating = false;
    private volatile bool _pendingAction = false; // 存储待执行的操作
    private bool _nextAction = false; // 下一步要执行的动作(true=显示, false=false)
    
    // 目标进程信息
    private Process? _targetProcess = null;
    private string _targetProcessName = string.Empty;
    private string _expectedVersion = string.Empty; // 期望的版本号

    // UWP特定变量
    private IntPtr _uwpHostHwnd = IntPtr.Zero; // UWP宿主窗口句柄
    private bool _isUWPApp = false; // 是否是UWP应用

    public OverlayWindow(Process targetProcess, string expectedVersion)
    {
        InitializeComponent();

        // 保存目标进程和期望版本
        _targetProcess = targetProcess;
        _targetProcessName = targetProcess.ProcessName.ToLower();
        _expectedVersion = expectedVersion;

        VersionBox.Text = $"Game Overlay (Ver.{GlobalModel.BodyVersion})";
        
        _proc = HookCallback;

        // 确保窗口在最上层
        Topmost = true;

        Opened += OnWindowOpened;
        Closed += OnWindowClosed;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        var platformHandle = TryGetPlatformHandle();
        if (platformHandle == null) return;
        _myHandle = platformHandle.Handle;

        // 初始化窗口状态 - 完全隐藏且不嵌入
        InitializeHiddenState();

        // 异步查找目标窗口，因为进程启动后窗口可能需要时间创建
        Task.Run(async () =>
        {
            // 最多重试20次，每次间隔500毫秒
            for (int i = 0; i < 20; i++)
            {
                var foundHwnd = FindWindowByProcessAndVersion(_targetProcess, _expectedVersion);
                
                if (foundHwnd != IntPtr.Zero)
                {
                    // 在UI线程中更新目标窗口句柄
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        _targetHwnd = foundHwnd;
                        
                        // 检查是否是UWP应用
                        _isUWPApp = IsUWPWindow(foundHwnd);
                        
                        // 现在找到了目标窗口，可以继续初始化
                        InitializeOverlay();
                    });
                    break;
                }
                
                await Task.Delay(500); // 等待500毫秒后重试
            }
            
            // 如果最终还是找不到窗口，则显示错误提示或禁用功能
            if (_targetHwnd == IntPtr.Zero)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    Console.WriteLine($"无法找到进程 {_targetProcess?.ProcessName} 版本为 {_expectedVersion} 的窗口");
                });
            }
        });
    }

    private void InitializeHiddenState()
    {
        // 确保窗口初始状态是隐藏的且没有嵌入关系
        _isOverlayVisible = false;
        _isEmbedded = false;
        
        // 设置窗口样式 - 透明且不可激活，不影响底层窗口
        var exStyle = GetWindowLong(_myHandle, GWL_EXSTYLE);
        exStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        exStyle &= ~WS_EX_LAYERED; // 初始时不使用分层窗口
        SetWindowLong(_myHandle, GWL_EXSTYLE, exStyle);

        // 完全隐藏窗口直到需要显示
        ShowWindow(_myHandle, SW_HIDE);
        EnableWindow(_myHandle, false);

        // 在Avalonia层面也设置为不可点击
        IsHitTestVisible = false;
        OverlayRoot.IsHitTestVisible = false;
    }

    private IntPtr FindWindowByProcessAndVersion(Process? process, string expectedVersion)
    {
        if (process == null || process.HasExited)
        {
            return IntPtr.Zero;
        }

        // 首先尝试通过版本号查找窗口
        IntPtr foundHwnd = FindWindowByVersion(process, expectedVersion);
        
        // 如果通过版本号没找到，尝试通过进程查找
        if (foundHwnd == IntPtr.Zero)
        {
            foundHwnd = FindWindowByProcessOnly(process);
        }
        
        return foundHwnd;
    }

    private IntPtr FindWindowByVersion(Process process, string expectedVersion)
    {
        IntPtr foundHwnd = IntPtr.Zero;
        var candidates = new List<WindowInfo>();

        bool EnumWindowCallback(IntPtr hWnd, IntPtr lParam)
        {
            // 检查窗口是否可见
            if (!IsWindowVisible(hWnd))
            {
                return true; // 继续枚举
            }

            // 获取窗口所属进程ID
            GetWindowThreadProcessId(hWnd, out uint windowPid);
            int pid = (int)windowPid;

            // 检查是否是目标进程的窗口
            if (pid == process.Id)
            {
                // 获取窗口标题
                var windowText = GetWindowText(hWnd);

                // 检查窗口标题是否包含版本信息
                if (HasMatchingVersion(windowText, expectedVersion))
                {
                    // 进一步检查窗口类型 - 排除控制台窗口
                    var className = GetClassName(hWnd);

                    // 跳过控制台窗口（ConsoleWindowClass）和一些系统窗口
                    if (className.Contains("Console") || 
                        className.Contains("MSCTFIME UI") || 
                        className.Contains("Shell_TrayWnd"))
                    {
                        return true; // 继续枚举
                    }

                    // 检查窗口矩形是否有效
                    RECT rect = new RECT();
                    if (GetWindowRect(hWnd, ref rect))
                    {
                        int width = rect.Right - rect.Left;
                        int height = rect.Bottom - rect.Top;

                        // 确保窗口有一定大小（不是极小的系统窗口）
                        if (width > 50 && height > 50)
                        {
                            // 获取窗口创建时间（近似方法）
                            long creationTime = GetWindowCreationTime(hWnd);
                            
                            candidates.Add(new WindowInfo 
                            { 
                                Handle = hWnd, 
                                CreationTime = creationTime,
                                Title = windowText
                            });
                        }
                    }
                }
            }

            return true; // 继续枚举
        }

        EnumWindows(EnumWindowCallback, IntPtr.Zero);
        
        // 按创建时间排序，返回最新的窗口
        if (candidates.Any())
        {
            var latestWindow = candidates.OrderByDescending(w => w.CreationTime).First();
            return latestWindow.Handle;
        }

        return IntPtr.Zero;
    }

    private IntPtr FindWindowByProcessOnly(Process process)
    {
        IntPtr foundHwnd = IntPtr.Zero;
        var candidates = new List<WindowInfo>();

        bool EnumWindowCallback(IntPtr hWnd, IntPtr lParam)
        {
            // 检查窗口是否可见
            if (!IsWindowVisible(hWnd))
            {
                return true; // 继续枚举
            }

            // 获取窗口所属进程ID
            GetWindowThreadProcessId(hWnd, out uint windowPid);
            int pid = (int)windowPid;

            // 检查是否是目标进程的窗口
            if (pid == process.Id)
            {
                // 获取窗口标题和类名
                var windowText = GetWindowText(hWnd);
                var className = GetClassName(hWnd);

                // 跳过控制台窗口（ConsoleWindowClass）和一些系统窗口
                if (className.Contains("Console") || 
                    className.Contains("MSCTFIME UI") || 
                    className.Contains("Shell_TrayWnd"))
                {
                    return true; // 继续枚举
                }

                // 检查窗口矩形是否有效
                RECT rect = new RECT();
                if (GetWindowRect(hWnd, ref rect))
                {
                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;

                    // 确保窗口有一定大小（不是极小的系统窗口）
                    if (width > 50 && height > 50)
                    {
                        // 获取窗口创建时间（近似方法）
                        long creationTime = GetWindowCreationTime(hWnd);
                        
                        candidates.Add(new WindowInfo 
                        { 
                            Handle = hWnd, 
                            CreationTime = creationTime,
                            Title = windowText
                        });
                    }
                }
            }

            return true; // 继续枚举
        }

        EnumWindows(EnumWindowCallback, IntPtr.Zero);
        
        // 按创建时间排序，返回最新的窗口
        if (candidates.Any())
        {
            var latestWindow = candidates.OrderByDescending(w => w.CreationTime).First();
            return latestWindow.Handle;
        }

        return IntPtr.Zero;
    }

    private long GetWindowCreationTime(IntPtr hWnd)
    {
        // 尝试通过进程ID获取创建时间，作为近似值
        GetWindowThreadProcessId(hWnd, out uint processId);
        try
        {
            var proc = Process.GetProcessById((int)processId);
            return proc.StartTime.ToFileTime();
        }
        catch
        {
            // 如果无法获取进程信息，返回默认值
            return DateTime.Now.ToFileTime();
        }
    }

    /// <summary>
    /// 检查窗口标题是否包含匹配的版本号
    /// </summary>
    /// <param name="windowTitle">窗口标题</param>
    /// <param name="expectedVersion">期望的版本号</param>
    /// <returns>是否匹配</returns>
    private bool HasMatchingVersion(string windowTitle, string expectedVersion)
    {
        if (string.IsNullOrEmpty(windowTitle) || string.IsNullOrEmpty(expectedVersion))
            return false;

        // 尝试从窗口标题中提取版本号
        var extractedVersions = ExtractVersionNumbers(windowTitle);
        
        foreach (var version in extractedVersions)
        {
            if (AreVersionsEquivalent(version, expectedVersion))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 从字符串中提取所有可能的版本号
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>版本号列表</returns>
    private List<string> ExtractVersionNumbers(string input)
    {
        var versions = new List<string>();
        
        // 匹配版本号的正则表达式模式
        // 支持 x.y.z, x.y, x.y.z.w 等格式
        var regex = new Regex(@"\b(\d+\.?\d*\.?\d*\.?\d*)\b", RegexOptions.IgnoreCase);
        var matches = regex.Matches(input);

        foreach (Match match in matches)
        {
            var version = match.Value.Trim();
            if (!string.IsNullOrEmpty(version) && !versions.Contains(version))
            {
                versions.Add(version);
            }
        }

        return versions;
    }

    /// <summary>
    /// 比较两个版本号是否等效
    /// </summary>
    /// <param name="version1">第一个版本号</param>
    /// <param name="version2">第二个版本号</param>
    /// <returns>是否等效</returns>
    private bool AreVersionsEquivalent(string version1, string version2)
    {
        // 移除可能的额外字符（如 "v", "ver.", "版本" 等）
        var cleanVersion1 = CleanVersionString(version1);
        var cleanVersion2 = CleanVersionString(version2);

        // 如果完全相等，则匹配
        if (cleanVersion1.Equals(cleanVersion2, StringComparison.OrdinalIgnoreCase))
            return true;

        // 尝试解析版本号并比较
        try
        {
            var parts1 = ParseVersionParts(cleanVersion1);
            var parts2 = ParseVersionParts(cleanVersion2);

            // 比较主要版本部分
            for (int i = 0; i < Math.Min(parts1.Count, parts2.Count); i++)
            {
                if (parts1[i] != parts2[i])
                    return false;
            }

            // 如果前面的数字都相同，则认为匹配
            return true;
        }
        catch
        {
            // 解析失败，回退到字符串比较
            return string.Equals(cleanVersion1, cleanVersion2, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 清理版本号字符串，移除非数字和点的字符
    /// </summary>
    /// <param name="version">原始版本号</param>
    /// <returns>清理后的版本号</returns>
    private string CleanVersionString(string version)
    {
        if (string.IsNullOrEmpty(version)) return string.Empty;

        // 移除前缀如 "v", "ver", "version"
        var cleaned = version.Replace("v", "", StringComparison.OrdinalIgnoreCase)
                           .Replace("ver", "", StringComparison.OrdinalIgnoreCase)
                           .Replace("version", "", StringComparison.OrdinalIgnoreCase)
                           .Trim();

        // 移除其他非版本号字符，只保留数字和点
        var result = "";
        bool lastWasDot = false;
        for (int i = 0; i < cleaned.Length; i++)
        {
            char c = cleaned[i];
            if (char.IsDigit(c))
            {
                result += c;
                lastWasDot = false;
            }
            else if (c == '.' && !lastWasDot)
            {
                result += c;
                lastWasDot = true;
            }
            else if (c == ' ')
            {
                // 空格替换为点，以便处理像 "1 20 0" 这样的情况
                if (!lastWasDot && i > 0 && char.IsDigit(cleaned[i - 1]) && 
                    i < cleaned.Length - 1 && char.IsDigit(cleaned[i + 1]))
                {
                    result += '.';
                    lastWasDot = true;
                }
            }
        }

        return result.TrimEnd('.');
    }

    /// <summary>
    /// 解析版本号字符串为数字部分
    /// </summary>
    /// <param name="version">版本号字符串</param>
    /// <returns>数字部分列表</returns>
    private List<int> ParseVersionParts(string version)
    {
        var parts = new List<int>();
        var numberStrings = version.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in numberStrings)
        {
            if (int.TryParse(part, out int num))
            {
                parts.Add(num);
            }
        }

        return parts;
    }

    private bool IsUWPWindow(IntPtr hwnd)
    {
        var className = GetClassName(hwnd);
        var windowText = GetWindowText(hwnd);
        
        // 检查窗口类名是否是UWP相关
        if (className.Contains("ApplicationFrame") || 
            className.Contains("Windows.UI.Core") ||
            className.Contains("Windows.UI.Xaml"))
        {
            return true;
        }
        
        // 检查是否是通过ApplicationFrameHost承载的UWP应用
        GetWindowThreadProcessId(hwnd, out uint pid);
        var process = Process.GetProcessById((int)pid);
        if (process.ProcessName.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        return false;
    }

    private string GetClassName(IntPtr hWnd)
    {
        var sb = new System.Text.StringBuilder(256);
        int length = GetClassName(hWnd, sb, sb.Capacity);
        
        // 检查API调用是否成功
        if (length == 0)
        {
            // 获取最后的错误代码
            int errorCode = Marshal.GetLastWin32Error();
            if (errorCode != 0)
            {
                Console.WriteLine($"GetClassName failed with error code: {errorCode}");
                return string.Empty;
            }
        }
        
        return sb.ToString(0, Math.Min(length, 255));
    }

    private string GetWindowText(IntPtr hWnd)
    {
        var length = GetWindowTextLength(hWnd);
        if (length == 0) return "";

        var sb = new System.Text.StringBuilder(length + 1);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private void InitializeOverlay()
    {
        // 3. 挂载全局键盘钩子
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule?.ModuleName), 0);
        }

        // 4. 启动同步计时器 (30 FPS 左右)
        _syncTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(32) };
        _syncTimer.Tick += (s, ev) => SyncSize();
        _syncTimer.Start();

        // 确保初始状态为隐藏（已在InitializeHiddenState中设置）
        SetOverlayState(false);
    }

    private void SetOverlayState(bool visible)
    {
        // 如果当前正在动画中，记录下一个动作，稍后执行
        if (_isAnimating)
        {
            _pendingAction = true;
            _nextAction = visible;
            return;
        }

        // 如果状态相同，无需操作
        if (_isOverlayVisible == visible)
        {
            return;
        }

        // 开始动画
        StartAnimation(visible);
    }

    private async void StartAnimation(bool visible)
    {
        _isAnimating = true;

        if (visible)
        {
            // 显示操作：直接显示，然后执行淡入动画
            ShowOverlayImmediate();
            // 执行淡入动画
            await AnimateOpacity(0, 1, 100); // 100ms淡入
        }
        else
        {
            // 隐藏操作：先执行淡出动画，然后隐藏
            await AnimateOpacity(1, 0, 150); // 150ms淡出
            HideOverlayInternal();
        }

        _isAnimating = false;

        // 检查是否有待处理的动作
        if (_pendingAction)
        {
            _pendingAction = false;
            // 递归调用，执行待处理的动作
            SetOverlayState(_nextAction);
        }
    }

    private async Task AnimateOpacity(double startOpacity, double endOpacity, int durationMs)
    {
        const int steps = 10;
        int stepDuration = durationMs / steps;
        double stepValue = (endOpacity - startOpacity) / steps;

        OverlayRoot.Opacity = startOpacity;

        for (int i = 1; i <= steps; i++)
        {
            // 检查是否在动画过程中被中断
            if (!_isAnimating)
            {
                break;
            }

            double newOpacity = startOpacity + (stepValue * i);
            if (newOpacity < 0) newOpacity = 0;
            if (newOpacity > 1) newOpacity = 1;

            OverlayRoot.Opacity = newOpacity;
            await Task.Delay(stepDuration);
        }

        OverlayRoot.Opacity = endOpacity;
    }

    private void ShowOverlayImmediate()
    {
        _isOverlayVisible = true;

        if (_targetHwnd != IntPtr.Zero)
        {
            // 对于UWP应用，使用特殊处理
            if (_isUWPApp)
            {
                ShowUWPOverlay();
            }
            else
            {
                // 对于普通应用，使用原来的逻辑
                SetParent(_myHandle, _targetHwnd);
                _isEmbedded = true;

                var clientRect = new RECT();
                if (GetClientRect(_targetHwnd, ref clientRect))
                {
                    _originalWidth = clientRect.Right - clientRect.Left;
                    _originalHeight = clientRect.Bottom - clientRect.Top;
                    _originalX = clientRect.Left;
                    _originalY = clientRect.Top;
                }

                // 设置为子窗口样式
                var style = GetWindowLong(_myHandle, GWL_STYLE);
                SetWindowLong(_myHandle, GWL_STYLE, style | WS_CHILD);

                // 恢复窗口大小
                Width = _originalWidth;
                Height = _originalHeight;
                MoveWindow(_myHandle, _originalX, _originalY, _originalWidth, _originalHeight, true);

                // 设置窗口样式（移除穿透）
                var exStyle = GetWindowLong(_myHandle, GWL_EXSTYLE);
                exStyle &= ~WS_EX_TRANSPARENT;
                exStyle &= ~WS_EX_NOACTIVATE;
                exStyle |= WS_EX_LAYERED; // 保留分层窗口样式
                SetWindowLong(_myHandle, GWL_EXSTYLE, exStyle);

                // 显示窗口
                ShowWindow(_myHandle, SW_SHOW);
                EnableWindow(_myHandle, true);

                // 在Avalonia层面允许点击
                IsHitTestVisible = true;
                OverlayRoot.IsHitTestVisible = true;

                // 激活并置顶焦点
                SetForegroundWindow(_myHandle);
                SetFocus(_myHandle);
            }
        }
    }

    private void ShowUWPOverlay()
    {
        // UWP应用的特殊处理逻辑
        // 由于UWP使用DirectComposition等现代渲染技术，我们不能直接SetParent
        // 而是需要跟随UWP窗口的位置和大小
        
        _isEmbedded = true;

        // 设置窗口样式（移除穿透，添加分层）
        var exStyle = GetWindowLong(_myHandle, GWL_EXSTYLE);
        exStyle &= ~WS_EX_TRANSPARENT;
        exStyle &= ~WS_EX_NOACTIVATE;
        exStyle |= WS_EX_LAYERED; // 使用分层窗口
        SetWindowLong(_myHandle, GWL_EXSTYLE, exStyle);

        // 获取UWP窗口位置和大小
        RECT windowRect = new RECT();
        if (GetWindowRect(_targetHwnd, ref windowRect))
        {
            _originalWidth = windowRect.Right - windowRect.Left;
            _originalHeight = windowRect.Bottom - windowRect.Top;
            _originalX = windowRect.Left;
            _originalY = windowRect.Top;

            // 调整窗口大小
            Width = _originalWidth;
            Height = _originalHeight;
            
            // 将叠层窗口放置在UWP窗口之上，但不激活它
            SetWindowPos(_myHandle, _targetHwnd, 
                _originalX, _originalY, 
                _originalWidth, _originalHeight,
                SWP_NOZORDER | SWP_NOACTIVATE);
            
            // 确保叠层窗口在UWP窗口之上
            SetWindowPos(_myHandle, HWND_TOP, 0, 0, 0, 0, 
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        // 显示窗口
        ShowWindow(_myHandle, SW_SHOW);
        EnableWindow(_myHandle, true);

        // 在Avalonia层面允许点击
        IsHitTestVisible = true;
        OverlayRoot.IsHitTestVisible = true;

        // 激活并置顶焦点
        SetForegroundWindow(_myHandle);
        SetFocus(_myHandle);
    }

    private void HideOverlayInternal()
    {
        _isOverlayVisible = false;

        // 在Avalonia层面也设置为不可点击
        IsHitTestVisible = false;
        OverlayRoot.IsHitTestVisible = false;

        // 设置完全透明（虽然动画已经完成了淡出）
        OverlayRoot.Opacity = 0;

        // 设置窗口穿透样式以确保不影响目标窗口的鼠标事件
        var exStyle = GetWindowLong(_myHandle, GWL_EXSTYLE);
        exStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        // 移除可能导致干扰的样式
        exStyle &= ~WS_EX_LAYERED;
        SetWindowLong(_myHandle, GWL_EXSTYLE, exStyle);

        // 移除内嵌关系，让窗口完全脱离目标窗口
        if (_isEmbedded)
        {
            SetParent(_myHandle, IntPtr.Zero); // 设置父窗口为桌面
            _isEmbedded = false;

            // 恢复原始窗口样式
            var style = GetWindowLong(_myHandle, GWL_STYLE);
            SetWindowLong(_myHandle, GWL_STYLE, style & ~WS_CHILD);
        }

        // 焦点彻底交还给游戏
        if (_targetHwnd != IntPtr.Zero)
        {
            SetForegroundWindow(_targetHwnd);
            SetFocus(_targetHwnd);
        }

        // 完全隐藏窗口
        ShowWindow(_myHandle, SW_HIDE);
        EnableWindow(_myHandle, false);
    }

    private void SyncSize()
    {
        // 只有在显示状态下且已内嵌时才同步大小
        if (!_isOverlayVisible || !_isEmbedded || _targetHwnd == IntPtr.Zero || _myHandle == IntPtr.Zero) return;

        if (_isUWPApp)
        {
            // UWP应用的特殊同步逻辑
            SyncUWPSizes();
        }
        else
        {
            // 普通应用的同步逻辑
            var clientRect = new RECT();
            if (GetClientRect(_targetHwnd, ref clientRect))
            {
                var w = clientRect.Right - clientRect.Left;
                var h = clientRect.Bottom - clientRect.Top;

                // 只有在尺寸变化时才调用 MoveWindow，减少开销
                if (Width != w || Height != h)
                {
                    Width = w;
                    Height = h;
                    MoveWindow(_myHandle, 0, 0, w, h, true);
                }
            }
        }
    }

    private void SyncUWPSizes()
    {
        // UWP应用的同步逻辑
        RECT windowRect = new RECT();
        if (GetWindowRect(_targetHwnd, ref windowRect))
        {
            var w = windowRect.Right - windowRect.Left;
            var h = windowRect.Bottom - windowRect.Top;

            // 只有在尺寸变化时才调整叠层窗口
            if (Width != w || Height != h)
            {
                Width = w;
                Height = h;
                
                // 更新叠层窗口位置，不改变Z顺序和激活状态
                SetWindowPos(_myHandle, _targetHwnd, 
                    windowRect.Left, windowRect.Top, 
                    w, h,
                    SWP_NOZORDER | SWP_NOACTIVATE);
            }
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            var vkCode = Marshal.ReadInt32(lParam);

            // 检测 Shift + Tab (Toggle显示/隐藏)
            var isShiftDown = (GetKeyState(VK_SHIFT) & 0x8000) != 0;

            if (vkCode == VK_TAB && isShiftDown)
                {
                    var foreground = GetForegroundWindow();
                    // 仅当游戏窗口或叠层窗口在前端时响应
                    if (foreground == _targetHwnd || foreground == _myHandle)
                    {
                        // 异步切换 UI 状态，避免阻塞钩子链
                        Dispatcher.UIThread.Post(() => SetOverlayState(!_isOverlayVisible));

                        // 返回 1 吞掉按键，防止游戏内弹出多余菜单
                        return (IntPtr)1;
                    }
                }

            // 检测 ESC (仅用于退出显示状态，不触发显示)
            if (vkCode == VK_ESCAPE)
            {
                var foreground = GetForegroundWindow();
                // 仅当叠层窗口在前端时响应ESC
                if (foreground == _myHandle && _isOverlayVisible)
                {
                    // 异步隐藏叠层，避免阻塞钩子链
                    Dispatcher.UIThread.Post(() => SetOverlayState(false));

                    // 返回 1 吞掉按键，防止游戏响应ESC
                    return (IntPtr)1;
                }
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _syncTimer?.Stop();
        if (_hookID != IntPtr.Zero) UnhookWindowsHookEx(_hookID);
    }

    private void CloseBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        SetOverlayState(false);
    }

    // 内部类用于存储窗口信息
    private class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public long CreationTime { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}