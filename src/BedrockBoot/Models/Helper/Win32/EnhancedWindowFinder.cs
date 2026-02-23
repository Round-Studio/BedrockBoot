using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BedrockBoot.Models.Helper.Win32;

public class EnhancedWindowFinder
{
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, ref RECT rect);

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool
        GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, int nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    // 访问权限常量
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_VM_READ = 0x0010;

    /// <summary>
    /// 根据进程获取其可执行文件路径
    /// </summary>
    /// <param name="process">目标进程</param>
    /// <returns>进程的可执行文件路径</returns>
    public static string GetProcessPath(Process process)
    {
        try
        {
            // 尝试直接从Process对象获取路径（.NET 4.5+）
            if (!string.IsNullOrEmpty(process.MainModule?.FileName))
            {
                return process.MainModule.FileName;
            }
        }
        catch
        {
            // 如果直接访问失败，使用Win32 API
        }

        IntPtr processHandle = IntPtr.Zero;
        try
        {
            processHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, process.Id);
            if (processHandle != IntPtr.Zero)
            {
                var fileNameBuilder = new StringBuilder(1024);
                if (GetModuleFileNameEx(processHandle, IntPtr.Zero, fileNameBuilder, fileNameBuilder.Capacity))
                {
                    return fileNameBuilder.ToString();
                }
            }
        }
        finally
        {
            if (processHandle != IntPtr.Zero)
            {
                CloseHandle(processHandle);
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// 根据进程路径查找所有具有相同源文件的窗口
    /// </summary>
    /// <param name="targetProcess">目标进程</param>
    /// <returns>匹配的窗口句柄列表</returns>
    public static List<IntPtr> FindWindowsByProcessPath(Process targetProcess)
    {
        var targetPath = GetProcessPath(targetProcess);
        if (string.IsNullOrEmpty(targetPath))
        {
            return new List<IntPtr>();
        }

        var matchingWindows = new List<IntPtr>();
        var processedPids = new HashSet<int>(); // 避免重复处理同一进程

        bool EnumWindowCallback(IntPtr hWnd, IntPtr lParam)
        {
            if (!IsWindowVisible(hWnd))
            {
                return true; // 继续枚举
            }

            GetWindowThreadProcessId(hWnd, out uint windowPid);
            int pid = (int)windowPid;

            // 如果这个进程ID已经处理过，跳过
            if (processedPids.Contains(pid))
            {
                return true;
            }

            try
            {
                var process = Process.GetProcessById(pid);
                var windowPath = GetProcessPath(process);

                // 比较路径是否相同（忽略大小写）
                if (!string.IsNullOrEmpty(windowPath) &&
                    string.Equals(windowPath, targetPath, StringComparison.OrdinalIgnoreCase))
                {
                    // 找到匹配的进程，枚举其所有窗口
                    FindAllWindowsOfProcess(process, matchingWindows);
                    processedPids.Add(pid);
                }
            }
            catch
            {
                // 可能没有权限访问进程或进程已退出
            }

            return true; // 继续枚举
        }

        EnumWindows(EnumWindowCallback, IntPtr.Zero);
        return matchingWindows;
    }

    /// <summary>
    /// 查找指定进程的所有窗口
    /// </summary>
    /// <param name="process">目标进程</param>
    /// <param name="windowsList">存储找到的窗口句柄</param>
    private static void FindAllWindowsOfProcess(Process process, List<IntPtr> windowsList)
    {
        bool EnumWindowCallback(IntPtr hWnd, IntPtr lParam)
        {
            GetWindowThreadProcessId(hWnd, out uint windowPid);
            if ((int)windowPid == process.Id)
            {
                if (IsWindowVisible(hWnd))
                {
                    // 额外验证窗口是否有效（非系统窗口）
                    var className = GetClassName(hWnd);
                    var windowText = GetWindowText(hWnd);

                    // 排除常见的系统窗口类
                    if (!className.Contains("Console") &&
                        !className.Contains("MSCTFIME UI") &&
                        !className.Contains("Shell_TrayWnd"))
                    {
                        // 验证窗口大小，排除极小的系统窗口
                        RECT rect = new RECT();
                        if (GetWindowRect(hWnd, ref rect))
                        {
                            int width = rect.Right - rect.Left;
                            int height = rect.Bottom - rect.Top;

                            if (width > 50 && height > 50)
                            {
                                windowsList.Add(hWnd);
                            }
                        }
                    }
                }
            }

            return true; // 继续枚举
        }

        EnumWindows(EnumWindowCallback, IntPtr.Zero);
    }

    /// <summary>
    /// 查找与目标进程具有相同源文件的第一个可见窗口
    /// </summary>
    /// <param name="targetProcess">目标进程</param>
    /// <returns>匹配的窗口句柄，如果未找到则返回IntPtr.Zero</returns>
    public static IntPtr FindFirstWindowByProcessPath(Process targetProcess)
    {
        var windows = FindWindowsByProcessPath(targetProcess);
        return windows.Count > 0 ? windows[0] : IntPtr.Zero;
    }

    /// <summary>
    /// 获取窗口的类名
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <returns>窗口类名</returns>
    private static string GetClassName(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        int length = GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString(0, Math.Min(length, 255));
    }

    /// <summary>
    /// 获取窗口的标题文本
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <returns>窗口标题文本</returns>
    private static string GetWindowText(IntPtr hWnd)
    {
        var length = GetWindowTextLength(hWnd);
        if (length == 0) return "";

        var sb = new StringBuilder(length + 1);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }
}