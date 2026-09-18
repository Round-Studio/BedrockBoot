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
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Models.Media;

public class MediaHelper
{
    /// <summary>
    /// 获取当前正在播放媒体的程序名称
    /// </summary>
    public static string? GetPlayingAppName()
    {
        Console.WriteLine(@"获取播放状态");
#if WINDOWS
        if (!SMTCHelper.IsStarted)
        {
            SMTCHelper.Initialize();
        }

        return SMTCHelper.GetSourceAppName();
#else
        Console.WriteLine(@"非 Windows 平台无法获取系统播放状态");
        return null;
#endif
    }

    /// <summary>
    /// 判断系统当前是否有媒体正在播放
    /// </summary>
    public static bool IsNowPlaying()
    {
#if WINDOWS
        if (!SMTCHelper.IsStarted)
        {
            SMTCHelper.Initialize();
        }

        return SMTCHelper.IsPlaying();
#else
        Console.WriteLine(@"非 Windows 平台无法获取系统播放状态");
        return false;
#endif
    }
}