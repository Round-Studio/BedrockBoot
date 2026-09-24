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