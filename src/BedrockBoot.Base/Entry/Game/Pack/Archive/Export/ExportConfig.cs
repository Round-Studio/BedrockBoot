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

using BedrockBoot.Base.Enum.Type.Export;

namespace BedrockBoot.Base.Entry.Game.Pack.Archive.Export;

public class ExportConfig
{
    public ArchiveExportType ExportType { get; set; } = ArchiveExportType.World;
    public ArchiveInfo? ArchiveInfo { get; set; } = null;
    
    public bool AllowRandomSeed { get; set; } = false;
    public bool LockTemplateOptions { get; set; } = true;
    public bool PortableBedrockBootConfig { get; set; } = true;
    public string PackVersion { get; set; } = "1.0.0";
    public string PackName { get; set; } = string.Empty;
    public string PackDescription { get; set; } = string.Empty;
}