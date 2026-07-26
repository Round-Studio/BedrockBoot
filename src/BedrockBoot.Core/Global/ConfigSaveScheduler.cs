using System;
using System.Threading;

namespace BedrockBoot.Core.Global;

/// <summary>
/// 配置保存的防抖调度器。
///
/// <para>
/// <see cref="Round.SDK.Entity.ConfigEntity{T}.Save"/> 每次调用都会把整个对象图序列化并整文件重写，
/// 同时触发 AfterSave 回调链（在本项目中会引发 UI 刷新与磁盘重扫）。
/// 该方法被直接绑定在 TextChanged / ValueChanged 等高频事件上时，
/// 每敲一个键、滑块每移动一像素都会产生一次完整的磁盘写入。
/// </para>
///
/// <para>
/// 本调度器把短时间内的多次保存请求合并为一次，在空闲指定时间后才真正落盘。
/// 用法：把高频事件里的 <c>GlobalModel.Config.Save()</c> 换成 <c>ConfigSaveScheduler.RequestSave()</c>。
/// 低频且需要立即生效的场景（如切换目录、关闭窗口）继续直接调用 Save()。
/// </para>
/// </summary>
public static class ConfigSaveScheduler
{
    /// <summary>默认防抖间隔（毫秒）</summary>
    private const int DefaultDelayMs = 400;

    /// <summary>
    /// 可选的调度委托。防抖 Timer 的回调运行在线程池线程上，而 Save() 触发的
    /// AfterSave 回调链可能访问 UI 控件；主程序应在启动时设置本属性把保存动作
    /// 调度回 UI 线程，例如：
    /// <c>ConfigSaveScheduler.Dispatcher = a => Avalonia.Threading.Dispatcher.UIThread.Post(a);</c>
    /// 未设置时直接在当前线程执行。
    /// </summary>
    public static Action<Action>? Dispatcher { get; set; }

    private static readonly object Gate = new();

    private static Timer? _timer;

    /// <summary>是否存在尚未落盘的修改</summary>
    private static bool _pending;

    /// <summary>
    /// 请求保存配置。多次调用会被合并，只在最后一次调用后的空闲期结束时写入一次。
    /// </summary>
    public static void RequestSave(int delayMs = DefaultDelayMs)
    {
        lock (Gate)
        {
            _pending = true;

            if (_timer == null)
            {
                _timer = new Timer(_ => Commit(), null, delayMs, Timeout.Infinite);
            }
            else
            {
                // 每次新请求都重新计时，实现「空闲后写入」
                _timer.Change(delayMs, Timeout.Infinite);
            }
        }
    }

    /// <summary>
    /// 立即把待写入的修改落盘。
    /// 应在程序退出、进入游戏等关键节点调用，避免丢失尚未写入的修改。
    /// </summary>
    public static void Flush()
    {
        lock (Gate)
        {
            if (!_pending) return;
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _pending = false;
        }

        // Flush 用于程序退出等关键节点，必须同步落盘，不经过 Dispatcher 异步调度
        SaveCore();
    }

    private static void Commit()
    {
        lock (Gate)
        {
            if (!_pending) return;
            _pending = false;
        }

        var dispatcher = Dispatcher;
        if (dispatcher != null)
        {
            dispatcher(SaveCore);
        }
        else
        {
            SaveCore();
        }
    }

    private static void SaveCore()
    {
        try
        {
            GlobalModel.Config?.Save();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"防抖保存配置失败: {ex.Message}");
        }
    }
}
