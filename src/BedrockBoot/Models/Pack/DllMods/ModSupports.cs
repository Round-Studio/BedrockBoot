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
using BedrockBoot.Models.Pack.DllMods.LeviLamina.Arke;
using BedrockBoot.Models.Pack.Game.Loaders.LoaderInstance;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Interface.Mods;

namespace BedrockBoot.Models.Pack.DllMods;

public class ModSupports
{
    public static List<Type> Supports { get; } = new()
    {
        typeof(ArkeSupportFactory)
    };

    public static List<IModSupport> GetEnableSupports(VersionConfig instance)
    {
        var supports = new List<IModSupport>();
        foreach (var supportType in Supports)
        {
            if (Activator.CreateInstance(supportType) is IModSupportFactory supportFac)
            {
                var support = supportFac.Create(instance);
                if (support.IsEnable())
                {
                    supports.Add(support);
                }
            }
        }
        return supports;
    }
}