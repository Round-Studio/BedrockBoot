using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class MouseLocker : IDisposable
{
    #region Windows API 声明

    [DllImport("user32.dll")]
    private static extern bool ClipCursor(ref Rectangle rect);

    [DllImport("user32.dll")]
    private static extern bool ClipCursor(IntPtr rect);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out Rectangle lpRect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    #endregion

    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; }
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
    }

    private IntPtr _targetWindowHandle;
    private CancellationTokenSource _cts;
    private bool _isLocked = false;

    /// <summary>
    /// 获取所有匹配的窗口
    /// </summary>
    public List<WindowInfo> FindWindowsByProcess(string processName)
    {
        var windows = new List<WindowInfo>();
        Process[] processes = Process.GetProcessesByName(processName);

        foreach (Process process in processes)
        {
            try
            {
                if (process.MainWindowHandle != IntPtr.Zero && IsWindowVisible(process.MainWindowHandle))
                {
                    string title = GetWindowTitle(process.MainWindowHandle);
                    windows.Add(new WindowInfo
                    {
                        Handle = process.MainWindowHandle,
                        Title = title,
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName
                    });
                }
            }
            catch
            {
                // 忽略访问被拒绝的进程
            }
        }
        return windows;
    }

    /// <summary>
    /// 获取所有包含指定标题的窗口
    /// </summary>
    public List<WindowInfo> FindWindowsByTitle(string titlePattern)
    {
        var windows = new List<WindowInfo>();
        Process[] processes = Process.GetProcesses();

        foreach (Process process in processes)
        {
            try
            {
                if (!string.IsNullOrEmpty(process.MainWindowTitle) &&
                    process.MainWindowTitle.Contains(titlePattern) &&
                    process.MainWindowHandle != IntPtr.Zero &&
                    IsWindowVisible(process.MainWindowHandle))
                {
                    windows.Add(new WindowInfo
                    {
                        Handle = process.MainWindowHandle,
                        Title = process.MainWindowTitle,
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName
                    });
                }
            }
            catch
            {
                // 忽略访问被拒绝的进程
            }
        }
        return windows;
    }

    /// <summary>
    /// 锁定到指定窗口句柄
    /// </summary>
    public bool LockToWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero || !IsWindowVisible(windowHandle))
            return false;

        _targetWindowHandle = windowHandle;
        return LockMouseToCurrentWindow();
    }

    private bool LockMouseToCurrentWindow()
    {
        if (_targetWindowHandle == IntPtr.Zero)
            return false;

        Rectangle clientRect = GetWindowClientRect(_targetWindowHandle);
        if (clientRect.IsEmpty)
            return false;

        bool result = ClipCursor(ref clientRect);
        if (result)
        {
            _isLocked = true;
            StartWindowTracking();
        }
        return result;
    }

    private Rectangle GetWindowClientRect(IntPtr hWnd)
    {
        if (!GetClientRect(hWnd, out Rectangle clientRect))
            return Rectangle.Empty;

        Point topLeft = new Point(clientRect.Left, clientRect.Top);
        Point bottomRight = new Point(clientRect.Right, clientRect.Bottom);

        ClientToScreen(hWnd, ref topLeft);
        ClientToScreen(hWnd, ref bottomRight);

        return new Rectangle(
            topLeft.X,
            topLeft.Y,
            bottomRight.X - topLeft.X,
            bottomRight.Y - topLeft.Y
        );
    }

    private string GetWindowTitle(IntPtr hWnd)
    {
        int length = GetWindowTextLength(hWnd);
        if (length == 0) return "无标题";

        StringBuilder sb = new StringBuilder(length + 1);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private void StartWindowTracking()
    {
        _cts = new CancellationTokenSource();
        Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested && _isLocked)
            {
                try
                {
                    if (_targetWindowHandle == IntPtr.Zero || !IsWindowVisible(_targetWindowHandle))
                    {
                        UnlockMouse();
                        break;
                    }

                    Rectangle newRect = GetWindowClientRect(_targetWindowHandle);
                    if (!newRect.IsEmpty)
                    {
                        ClipCursor(ref newRect);
                    }

                    await Task.Delay(100, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, _cts.Token);
    }

    public void UnlockMouse()
    {
        ClipCursor(IntPtr.Zero);
        _isLocked = false;

        if (_cts != null)
        {
            _cts.Cancel();
            _cts = null;
        }
    }

    public bool IsLocked => _isLocked;

    public void Dispose()
    {
        UnlockMouse();
        _cts?.Cancel();
        _cts?.Dispose();
    }
}