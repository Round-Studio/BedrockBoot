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

using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Entry.Info.Download;

namespace BedrockBoot.Interface.Download;

public interface IDownloadResult
{
    public SearchResultItemInfo SearchInfo { get; set; }
    public bool IsHasManyFiles { get; }
    public Task<List<Control>?> DescriptionControls();
    public Task<uint> GetDownloadCount();
    public Task<bool> IsInstalled();
    public Task Install();
    public Task ReInstall();
    public void Delete();
    public Task<List<ResourceFileInfo>> GetFiles();
}