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
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Enum.Type;

namespace BedrockBoot.Interface.ModLoader;

public interface IModsManager
{
    public void Init(VersionConfig instance);
    public Action? OnRefresh { get; set; }
    public List<ModItemInfo> GetAllMods();
    public Task AddMod();
    public void Remove(ModItemInfo info);
}

public class ModItemInfo
{
    public string? ModName { get; set; }
    public string? ModDescription { get; set; }
    public string? ModPath { get; set; }
    public string? Version { get; set; }
    public Type? ModLoaderType { get; set; }
    public ModType ModInjectType { get; set; }
    public int InjectDelay { get; set; } = 5000;
}