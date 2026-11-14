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
using System.Threading.Tasks;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Standard.Entity.Account.Microsoft;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Capes;
using BedrockBoot.Standard.Entity.Game.Pack.Capes;

namespace BedrockBoot.Microsoft
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var instance = GameInfoHelper.GetVersionConfig(@"D:\BedrockBoot\bedrock_versions\26.60");
            var manager = new CapesPackManager(instance);
            var pack = manager.LoadPacks()[0];
            pack.LoadCapeFile();
            pack.AddCape(new CapeFileInfo()
            {
                CapeImagePath = @"D:\BedrockBoot\bedrock_versions\26.60\config\BedrockBoot2\capes\aurora-wings\aurora.png",
                CapeName = "aurora",
                Type = CapeFileType.Legendary
            });
        }
    }
}

