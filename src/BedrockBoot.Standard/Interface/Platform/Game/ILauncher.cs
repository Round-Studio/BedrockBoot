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

using System.Diagnostics;
using BedrockBoot.Standard.Entity.Game;

namespace BedrockBoot.Standard.Interface.Platform.Game;

public abstract class ILauncher
{
    protected ILauncher(VersionConfig options)
    {
        VersionInfo = options;
    }

    public abstract Task LaunchGame();

    public VersionConfig VersionInfo { get; protected set; }

    public Action? OnMigration { get; set; }
    public Action? NoRunTool { get; set; }
    public Action<Process>? Launched { get; set; }
    public Action? LaunchCompleted { get; set; }
    public Action<string, double>? UpdateProgress { get; set; }
    public Action<string>? UpdateProgressText { get; set; }
    public Action<bool>? SetProgressIndeterminate { get; set; }
    public Process? MinecraftProcess { get; protected set; }

    protected static int LaunchingCount { get; set; }

    public static Action? LaunchedBehavior { get; set; }
    public static Action<VersionConfig>? OnGameLaunched { get; set; }
    public static Action<VersionConfig>? OnGameExited { get; set; }
}