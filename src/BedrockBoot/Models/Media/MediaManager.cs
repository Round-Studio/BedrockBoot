using System;
using NAudio.Wave;

namespace BedrockBoot.Models.Media;

public class MediaManager
{
    public static MediaManager Instance { get; } = new MediaManager();
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _audioFile;
    private float _volume = 1.0f;

    public bool Enabled { get; set; } = false;
    public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;
    public string? CurrentFilePath { get; private set; }
    public bool Loop { get; set; } = false; // 新增循环播放属性

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0.0f, 1.0f);
            if (_audioFile != null)
            {
                _audioFile.Volume = _volume;
            }
        }
    }

    public void Play(string filePath)
    {
        if (!Enabled) return;

        Stop(); // 停止当前播放

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

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (Loop && CurrentFilePath != null && Enabled)
        {
            // 重置音频位置并重新播放
            _audioFile?.Seek(0, System.IO.SeekOrigin.Begin);
            _waveOut?.Play();
        }
    }

    public void Pause()
    {
        if (!Enabled || _waveOut == null) return;

        _waveOut?.Pause();
    }

    public void Resume()
    {
        if (!Enabled || _waveOut == null) return;

        _waveOut?.Play();
    }

    public void Stop()
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
        if (!Enabled) return;

        if (IsPlaying)
        {
            Pause();
        }
        else if (_waveOut != null)
        {
            Resume();
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