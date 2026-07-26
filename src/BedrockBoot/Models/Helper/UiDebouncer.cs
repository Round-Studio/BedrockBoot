using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace BedrockBoot.Models.Helper;

/// <summary>
/// 通用的 UI 操作防抖器。
///
/// <para>
/// 用于搜索框一类的高频输入事件：原实现每敲一个字符就立即执行一次完整刷新
/// （全目录扫描 + 逐实例读取 JSON + 重建全部控件），
/// 输入 10 个字符即触发 10 次全量扫描。
/// </para>
///
/// <para>
/// 使用方式：作为字段持有一个实例，在事件处理器中调用 <see cref="Debounce"/>；
/// 在控件卸载时调用 <see cref="Cancel"/>（或 <see cref="Dispose"/>）。
/// 回调总是在 UI 线程上执行。
/// </para>
/// </summary>
public sealed class UiDebouncer : IDisposable
{
    /// <summary>搜索类输入的默认延迟（毫秒）</summary>
    public const int DefaultSearchDelayMs = 300;

    private readonly int _delayMs;
    private CancellationTokenSource? _cts;

    public UiDebouncer(int delayMs = DefaultSearchDelayMs)
    {
        _delayMs = delayMs;
    }

    /// <summary>
    /// 请求执行操作。若在延迟结束前再次调用，则上一次请求被取消。
    /// </summary>
    public void Debounce(Action action)
    {
        Cancel();

        var cts = new CancellationTokenSource();
        _cts = cts;
        var token = cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_delayMs, token).ConfigureAwait(false);
                if (token.IsCancellationRequested) return;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!token.IsCancellationRequested) action();
                });
            }
            catch (OperationCanceledException)
            {
                // 被后续输入取消，属正常路径
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"防抖操作执行失败: {ex.Message}");
            }
        }, token);
    }

    /// <summary>取消尚未执行的请求</summary>
    public void Cancel()
    {
        var old = Interlocked.Exchange(ref _cts, null);
        if (old == null) return;

        try
        {
            old.Cancel();
            old.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // 已释放，忽略
        }
    }

    public void Dispose() => Cancel();
}
