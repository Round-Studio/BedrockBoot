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

namespace BedrockBoot.Base.Entry.Game.Pack.Isolation;

public class MigrationConfig
{
    public bool IsEnableResourcePack { get; set; } = true;
    public bool IsEnableBehaviorPack { get; set; } = true;
    public bool IsEnableArchive { get; set; } = true;
    public bool IsEnableConfig { get; set; } = true;
    public VersionConfig NewVersionConfig { get; set; }
    public VersionConfig OldVersionConfig { get; set; }
}