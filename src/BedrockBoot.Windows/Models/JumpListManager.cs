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
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Global;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Taskbar;

public class JumpListManager
{
    public static void ConfigureJumpList()
    {
        var jumpList = JumpList.CreateJumpList();
        if (!BedrockBoot.Core.Global.GlobalModel.Config.Data.IsTaskBarJumpItem)
        {
            jumpList.ClearAllUserTasks();
            jumpList.Refresh();
            return;
        }
        try
        {
            var versions = GameInfoHelper.GetVersionConfigs(BedrockBoot.Core.Global.GlobalModel.Config.Data
                .GameFolders[BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolderSelIndex].GameFolderPath);

           var tasks = versions.Select(v =>
               new JumpListLink(Process.GetCurrentProcess().MainModule!.FileName, v.Info.VersionName)
               {
                   Arguments = $"-jump \"{v.VersionPath}\"",
                    IconReference = new IconReference(Process.GetCurrentProcess().MainModule!.FileName,
                        SourceList.MinecraftIconID)
               });

           Console.WriteLine($@"添加跳转列表快捷启动项");

 		   jumpList.AddUserTasks(tasks.ToArray());
           jumpList.Refresh();
		}
        catch
        {
        }
    }
}