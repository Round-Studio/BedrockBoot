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

namespace Windows.Management.Deployment;

public struct DeploymentProgress
{
    /// <summary>
    /// 获取整个部署操作过程完成的百分比。
    /// </summary>
    /// <returns>
    /// 一个 0 到 100 之间的值，表示完成的百分比。
    /// </returns>
    public uint percentage { get; }
        
    /// <summary>
    /// 获取部署状态的当前可读状态消息。
    /// </summary>
    /// <returns>
    /// 一个字符串，包含部署操作的当前状态消息。
    /// </returns>
    public string stateText { get; }
}