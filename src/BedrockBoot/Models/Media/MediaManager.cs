using System;
using System.IO;
using System.Threading;
using NAudio.Wave;

namespace BedrockBoot.Models.Media;

public class MediaManager
{
    public static MediaManager Instance { get; } = new MediaManager();

    /// <summary>
    /// 保护播放状态的锁。
    /// Play 由 UpdateTheme 的多个 Task.Run 并发调用，
    /// 无锁时"检查 Enabled → 创建播放器"之间存在竞态：
    /// 用户关闭音频后，先前已通过检查的主题包播放任务仍会开始播放。
    /// </summary>
    private readonly object _gate = new();

    private WaveOutEvent? _waveOut;
    private AudioFileReader? _audioFile;
    private float _volume = 1.0f;
    private bool _enabled = true;

    /// <summary>
    /// 是否启用音频。
    /// 置为 false 时会立即停止当前播放（包括主题包音频），
    /// 而不是等下一次 Play 或整曲播完。
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            lock (_gate)
            {
                _enabled = value;
                if (!value) StopInternal();
            }
        }
    }

    public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;
    public string? CurrentFilePath { get; private set; }
    public bool Loop { get; set; } = true; // 循环播放属性

    public float Volume
    {
        get => _volume;
        set
        {
            lock (_gate)
            {
                _volume = Math.Clamp(value, 0.0f, 1.0f);
                if (_audioFile != null)
                {
                    _audioFile.Volume = _volume;
                }
            }
        }
    }

    public void Play(string filePath)
    {
        lock (_gate)
        {
            StopInternal(); // 停止当前播放

            // Enabled 检查必须与播放器创建处于同一临界区，
            // 否则关闭音频与开始播放之间存在竞态
            if (!_enabled) return;
            if (!File.Exists(filePath)) return;

            try
            {
                _audioFile = new AudioFileReader(filePath)
                {
                    Volume = _volume
                };

                _waveOut = new WaveOutEvent();
                _waveOut.PlaybackStopped += OnPlaybackStopped; // 添加播放完成事件监听
                _waveOut.Init(_audioFile);
                _waveOut.Play();

                CurrentFilePath = filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"播放音乐出错: {ex.Message}");
                DisposeResources();
            }
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        // 使用 TryEnter 而非 lock：
        // 该回调在 NAudio 播放线程上触发，若此时另一线程正持锁执行
        // Stop/Dispose（其内部会等待播放线程退出），普通 lock 会死锁。
        // 拿不到锁说明正在停止/切歌，跳过循环重播即可。
        if (!Monitor.TryEnter(_gate, 50)) return;
        try
        {
            if (Loop && CurrentFilePath != null && _enabled)
            {
                // 重置音频位置并重新播放
                _audioFile?.Seek(0, SeekOrigin.Begin);
                _waveOut?.Play();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"循环播放出错: {ex.Message}");
        }
        finally
        {
            Monitor.Exit(_gate);
        }
    }

    public void Pause()
    {
        lock (_gate)
        {
            if (!_enabled || _waveOut == null) return;
            _waveOut.Pause();
        }
    }

    public void Resume()
    {
        lock (_gate)
        {
            if (!_enabled || _waveOut == null) return;
            _waveOut.Play();
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            StopInternal();
        }
    }

    /// <summary>停止并释放播放器。必须在持有 _gate 的情况下调用。</summary>
    private void StopInternal()
    {
        if (_waveOut != null)
        {
            _waveOut.PlaybackStopped -= OnPlaybackStopped; // 移除事件监听
            _waveOut.Stop();
            DisposeResources();
            CurrentFilePath = null;
        }
    }

    public void TogglePlayPause()
    {
        lock (_gate)
        {
            if (!_enabled) return;

            if (_waveOut?.PlaybackState == PlaybackState.Playing)
            {
                _waveOut.Pause();
            }
            else if (_waveOut != null)
            {
                _waveOut.Play();
            }
        }
    }

    private void DisposeResources()
    {
        _waveOut?.Dispose();
        _waveOut = null;

        _audioFile?.Dispose();
        _audioFile = null;
    }

    ~MediaManager()
    {
        DisposeResources();
    }
}
