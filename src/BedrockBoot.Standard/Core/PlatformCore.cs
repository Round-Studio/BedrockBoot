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

using BedrockBoot.Standard.Entity.Account.Microsoft;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Interface.Platform;
using BedrockBoot.Standard.Interface.Platform.Game;

namespace BedrockBoot.Standard.Core;

public class PlatformCore
{
    public static Func<MsUserConfig?>? GetMsAccountConfig;
    public static Func<MsUserConfig, Task<MsUserConfig>>? OnRefreshAccount { get; set; }
    public static ICoreInit? CoreInit { get; private set; } = null;
    private static PlatformInitFrame? _platformInitFrame;
    public static async Task InstallAsync(PlatformInitFrame platformInitFrame)
    {
        _platformInitFrame = platformInitFrame;
        CoreInit = platformInitFrame.CoreInit;

        await CoreInit.InitializeAsync();
    }
    public static ILauncher CreateLauncher(VersionConfig options)
    {
        if (_platformInitFrame == null)
            throw new InvalidOperationException("平台未初始化");

        var launcher = Activator.CreateInstance(
            _platformInitFrame.LauncherType,
            options) as ILauncher;

        if (launcher == null)
            throw new InvalidOperationException("无法创建启动器实例");
        return launcher;
    }
}

public readonly struct LauncherType
{
    public Type Value { get; }
    public LauncherType(Type type)
    {
        if (!typeof(ILauncher).IsAssignableFrom(type))
            throw new ArgumentException("非 ILauncher 类型", nameof(type));
        Value = type;
    }
    public static implicit operator Type(LauncherType t) => t.Value;
    public static implicit operator LauncherType(Type t) => new(t);
}

public class PlatformInitFrame
{
    public required ICoreInit CoreInit { get; set; }
    public required LauncherType LauncherType { get; set; }
}