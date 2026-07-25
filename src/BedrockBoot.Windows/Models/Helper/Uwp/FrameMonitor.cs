using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Helper.Uwp
{
    public sealed class FrameMonitor : IDisposable
    {
        #region WinEvent

        private delegate void WinEventDelegate(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime);


        private const uint EVENT_OBJECT_CREATE = 0x8000;
        private const uint WINEVENT_OUTOFCONTEXT = 0;


        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(
            uint eventMin,
            uint eventMax,
            IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc,
            uint idProcess,
            uint idThread,
            uint dwFlags);


        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(
            IntPtr hWinEventHook);


        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(
            IntPtr hWnd,
            out uint processId);


        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(
            IntPtr hWnd,
            StringBuilder lpString,
            int nMaxCount);


        #endregion


        #region Message Loop

        private const uint WM_QUIT = 0x0012;


        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public UIntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }


        [DllImport("user32.dll")]
        private static extern bool GetMessage(
            out MSG lpMsg,
            IntPtr hWnd,
            uint min,
            uint max);


        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(
            ref MSG lpMsg);


        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(
            ref MSG lpMsg);


        [DllImport("user32.dll")]
        private static extern bool PostThreadMessage(
            uint idThread,
            uint msg,
            UIntPtr wParam,
            IntPtr lParam);


        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();


        #endregion


        public sealed class FrameInfo
        {
            public IntPtr Hwnd { get; init; }

            public string Title { get; init; } = "";

            public DateTime Created { get; init; }
        }

        private readonly ConcurrentDictionary<IntPtr, FrameInfo> _frames = new();


        private WinEventDelegate? _callback;

        private IntPtr _hook;


        private Thread? _monitorThread;

        private uint _monitorThreadId;


        private CancellationTokenSource? _cts;

        private readonly object _logLock = new();


        private bool _isCompleted = false;
        private readonly object _completionLock = new();

        public string GameName { get; set; } = "";


        public IReadOnlyDictionary<IntPtr, FrameInfo> Frames => _frames;

        private TaskCompletionSource<IntPtr>? _hwndSource;
        public Task<IntPtr> StartFrameMonitorAsync(int timeoutSeconds = 300)
        {
            StopFrameMonitor();

            _frames.Clear();

            _isCompleted = false;

            _hwndSource = new TaskCompletionSource<IntPtr>(
                TaskCreationOptions.RunContinuationsAsynchronously);


            _monitorThread = new Thread(() =>
            {
                _monitorThreadId = GetCurrentThreadId();

                _callback = WinEventProc;

                _hook = SetWinEventHook(
                    EVENT_OBJECT_CREATE,
                    EVENT_OBJECT_CREATE,
                    IntPtr.Zero,
                    _callback,
                    0,
                    0,
                    WINEVENT_OUTOFCONTEXT);


                if (_hook == IntPtr.Zero)
                {
                    _hwndSource.TrySetException(
                        new Exception("SetWinEventHook failed"));
                    return;
                }


                MSG msg;

                while (GetMessage(
                    out msg,
                    IntPtr.Zero,
                    0,
                    0))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }

            });


            _monitorThread.IsBackground = true;
            _monitorThread.Start();


            _cts = new CancellationTokenSource();

            _ = AutoStop(timeoutSeconds, _cts.Token);


            return _hwndSource.Task;
        }

        public void StopFrameMonitor()
        {
            _cts?.Cancel();

            _cts?.Dispose();

            _cts = null;

            lock (_completionLock)
            {
                _isCompleted = true;
            }

            if (_hook != IntPtr.Zero)
            {
                UnhookWinEvent(_hook);

                _hook = IntPtr.Zero;
            }


            if (_monitorThreadId != 0)
            {
                PostThreadMessage(
                    _monitorThreadId,
                    WM_QUIT,
                    UIntPtr.Zero,
                    IntPtr.Zero);

                _monitorThreadId = 0;
            }


            Console.WriteLine("停止监视 ApplicationFrameHost");
        }


        private async Task AutoStop(
            int seconds,
            CancellationToken token)
        {
            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(seconds),
                    token);


                StopFrameMonitor();
            }
            catch (TaskCanceledException)
            {
            }
        }

        public bool CheckTimestamp(IntPtr hwnd)
        {
            return _frames.ContainsKey(hwnd);
        }

        public bool TryGetFrameInfo(
            IntPtr hwnd,
            out FrameInfo info)
        {
            return _frames.TryGetValue(
                hwnd,
                out info!);
        }


        private void WinEventProc(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime)
        {
            
            if (hwnd == IntPtr.Zero)
                return;
            if (idObject != 0)
                return;

            GetWindowThreadProcessId(
                hwnd,
                out uint pid);

            Process process;
            try
            {
                process = Process.GetProcessById(
                    (int)pid);
            }
            catch
            {
                return;
            }


            if (!process.ProcessName.Equals(
                "ApplicationFrameHost",
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            _ = RecordFrameAsync(hwnd);
        }


        private async Task RecordFrameAsync(
            IntPtr hwnd)
        {
            await Task.Delay(500);

            if (_frames.ContainsKey(hwnd))
                return;

            string title = GetWindowTitle(hwnd);

            if (string.IsNullOrWhiteSpace(title))
                return;
            bool shouldRecord = false;

            // 检查标题是否包含 "Minecraft"（不区分大小写）
            if (title.Contains("Minecraft", StringComparison.OrdinalIgnoreCase))
            {
                shouldRecord = true;
            }

            // 检查标题是否等于 GameName（区分大小写）
            if (!string.IsNullOrEmpty(GameName) && title.Equals(GameName, StringComparison.Ordinal))
            {
                shouldRecord = true;
            }

            // 如果不符合条件，则不记录
            if (!shouldRecord)
                return;
            var info = new FrameInfo
            {
                Hwnd = hwnd,
                Title = title,
                Created = DateTime.Now
            };

            if (_frames.TryAdd(hwnd, info))
            {
                _hwndSource?.TrySetResult(hwnd);

                lock (_logLock)
                {
                    Console.WriteLine("--------------------------------");
                    Console.WriteLine("New Frame");
                    Console.WriteLine($"HWND : 0x{hwnd.ToInt64():X}/ {hwnd}");
                    Console.WriteLine($"Title: {title}");
                    Console.WriteLine($"Time : {info.Created:HH:mm:ss.fff}");
                    Console.WriteLine();

                    StopFrameMonitor();

                }
            }
        }

        private static string GetWindowTitle(
            IntPtr hwnd)
        {
            StringBuilder sb = new(512);

            GetWindowText(
                hwnd,
                sb,
                sb.Capacity);


            return sb.ToString();
        }


        public void Dispose()
        {
            StopFrameMonitor();
        }
    }
}