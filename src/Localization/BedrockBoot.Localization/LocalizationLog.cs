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

namespace BedrockBoot.Localization;

/// <summary>
///     极简日志出口。宿主可接管 <see cref="Sink" /> 把本地化问题写进自己的日志系统。
/// </summary>
public static class LocalizationLog
{
    /// <summary>日志接收器；为 null 时退回控制台。</summary>
    public static Action<string>? Sink { get; set; }

    /// <summary>输出一条警告。</summary>
    public static void Warn(string message) => Write($"[Localization] {message}");

    /// <summary>输出一条普通信息。</summary>
    public static void Info(string message) => Write($"[Localization] {message}");

    private static void Write(string message)
    {
        var sink = Sink;
        if (sink is null)
            Console.WriteLine(message);
        else
            sink(message);
    }
}
