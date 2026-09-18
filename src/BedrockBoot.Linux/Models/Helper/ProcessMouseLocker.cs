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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Diagnostics;
using BedrockBoot.Models.Helper.Notice;

namespace BedrockBoot.Models.Helper;

public class ProcessMouseLocker
{
    public ProcessMouseLocker(int processId)
    {
        Console.WriteLine("Linux 中无需使用鼠标锁模块");
    }

    /// <summary>
    /// 开启鼠标锁定监控逻辑
    /// </summary>
    public void Start()
    {
        Console.WriteLine("Linux 中无需使用鼠标锁模块");
    }

    /// <summary>
    /// 停止监控并释放鼠标
    /// </summary>
    public void Stop()
    {
        Console.WriteLine("Linux 中无需使用鼠标锁模块");
    }
}