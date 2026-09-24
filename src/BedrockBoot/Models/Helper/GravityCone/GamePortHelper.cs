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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.GravityCone.Enum;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Helper.GravityCone;

public class GamePortHelper
{
    public static List<VersionConfig>? RunningGames { get; set; } = new();

    public static void AddInstance(VersionConfig instance)
    {
        var paths = RunningGames?.Select(x => x.VersionPath).ToList();
        if (!paths.Contains(instance.VersionPath))
        {
            RunningGames?.Add(instance);
        }

        if (GlobalModel.CurrentRoomState != null) UpdateAllInstancePortStatus(GlobalModel.CurrentRoomState.RoomType);
    }

    public static void UpdateAllInstancePortStatus(RoomType type)
    {
        if (RunningGames == null) return;
        var paths = RunningGames.Select(x => x.VersionPath);
        paths.ToList().ForEach(path =>
        {
            var file = Path.Combine(path, "config", "BedrockBoot2", ".bb.gcstatus");
            File.WriteAllText(file, type == RoomType.Guest ? "guest" : "host");
        });
        Console.WriteLine("已更新所有实例的端口状态");
    }
}