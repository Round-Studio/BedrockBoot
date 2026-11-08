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

using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Interface.ModLoader;

namespace BedrockBoot.Standard.Interface.Mods;

public interface IModSupport
{
    string ModName { get; }
    string Tag { get; }
    string SupportName { get; }
    string ModType { get; }
    object? SupportPage { get; }
    string Glyph { get; }
    VersionConfig Instance { get; set; }
    bool IsEnable();
}

public interface IModSupportFactory
{
    IModSupport Create(VersionConfig instance);
}