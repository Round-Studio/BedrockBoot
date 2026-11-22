/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Standard.Entity;
#if WINDOWS
using BedrockBoot.Windows.Models.Helper;
#endif

namespace BedrockBoot.Models.Media;

/// <summary>
/// SMTC 后台监视服务：启动器启动后持续检测系统媒体播放。
/// 外部音乐播放期间静音启动器自身音频（MediaManager），停止后自动恢复；
/// 启动时已有外部音乐在播放、或运行期间切歌时，发出回调供 UI 弹出胶囊提示。
/// 所有回调均已在 UI 线程上派发。
/// </summary>
public class MediaMonitor
{
    public static MediaMonitor Instance { get; } = new();

    /// <summary>启动检测：启动器启动时已有外部音乐正在播放</summary>
    public event Action<MediaSong?>? ExternalPlaybackStarted;

    /// <summary>外部音乐切歌（标题或歌手变化）</summary>
    public event Action<MediaSong?>? TrackChanged;

    /// <summary>外部音乐停止播放</summary>
    public event Action? ExternalPlaybackStopped;

    private bool _started;
    private bool _externalPlaying;

    // 自动静音状态：静音前记录 MediaManager.Enabled 原值，外部播放结束后还原
    private bool _autoMuted;
    private bool _preMuteEnabled;

    // 去重：仅标题或歌手变化时才触发切歌通知
    private string? _lastTitle;
    private string? _lastArtist;

    private int _polling;

    private MediaMonitor() { }

    /// <summary>
    /// 启动后台监视（幂等，建议在程序启动时调用一次）
    /// </summary>
    public void Start()
    {
        if (_started) return;
        _started = true;

#if WINDOWS
        SMTCHelper.Initialize();
        SMTCHelper.PlaybackStateChanged += OnPlaybackStateChanged;
        SMTCHelper.MediaTrackChanged += OnMediaTrackChanged;

        // 启动检测：SMTC 会话枚举是异步的，轮询一小段时间等焦点会话出现
        Task.Run(async () =>
        {
            for (var i = 0; i < 16; i++)
            {
                await Task.Delay(500);
                if (SMTCHelper.IsPlaying()) break;
            }

            if (SMTCHelper.IsPlaying())
                SetExternalPlaying(true);
        });
#endif
    }

#if WINDOWS
    private void OnPlaybackStateChanged()
    {
        var playing = SMTCHelper.IsPlaying();
        if (playing != _externalPlaying)
        {
            SetExternalPlaying(playing);
        }
        else if (playing)
        {
            // 状态没变但可能换了曲目（如焦点会话切换）
            _ = RefreshTrackAsync();
        }
    }

    private void OnMediaTrackChanged()
    {
        if (_externalPlaying)
            _ = RefreshTrackAsync();
    }

    private async Task RefreshTrackAsync()
    {
        // 避免连续事件并发请求媒体属性
        if (Interlocked.Exchange(ref _polling, 1) == 1) return;
        try
        {
            var song = await SMTCHelper.GetCurrentSongAsync();
            if (song == null) return;
            if (song.Title == _lastTitle && song.Artist == _lastArtist) return;

            _lastTitle = song.Title;
            _lastArtist = song.Artist;

            var handler = TrackChanged;
            Dispatcher.UIThread.Post(() => handler?.Invoke(song));
        }
        finally
        {
            Interlocked.Exchange(ref _polling, 0);
        }
    }
#endif

    private void SetExternalPlaying(bool playing)
    {
        _externalPlaying = playing;

        if (playing)
        {
            // 新一轮播放：清空去重缓存，保证启动通知一定会发出
            _lastTitle = null;
            _lastArtist = null;

            // 真静音启动器自身音频；停止播放后按记录的原值恢复
            _autoMuted = true;
            _preMuteEnabled = MediaManager.Instance.Enabled;
            MediaManager.Instance.Enabled = false;

            _ = NotifyStartedAsync();
        }
        else
        {
            _lastTitle = null;
            _lastArtist = null;

            if (_autoMuted)
            {
                _autoMuted = false;
                MediaManager.Instance.Enabled = _preMuteEnabled;
            }

            var handler = ExternalPlaybackStopped;
            Dispatcher.UIThread.Post(() => handler?.Invoke());
        }
    }

    private async Task NotifyStartedAsync()
    {
        var song = await GetCurrentSongAsync();

        // 记录当前曲目，防止启动通知后紧跟着一条重复的切歌通知
        _lastTitle = song?.Title;
        _lastArtist = song?.Artist;

        var handler = ExternalPlaybackStarted;
        Dispatcher.UIThread.Post(() => handler?.Invoke(song));
    }

    private static Task<MediaSong?> GetCurrentSongAsync()
    {
#if WINDOWS
        return SMTCHelper.GetCurrentSongAsync();
#else
        return Task.FromResult<MediaSong?>(null);
#endif
    }
}
