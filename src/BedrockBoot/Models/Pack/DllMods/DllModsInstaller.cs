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
using System.IO;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Core.Models.Pack.Game.Mods;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Search;

namespace BedrockBoot.Models.Pack.DllMods;

public class DllModsInstaller
{
    private readonly VersionConfig _instance;
    private readonly ModFile _modFile;

    public IProgress<DownloadProgress>? Progress { get; set; }

    public DllModsInstaller(VersionConfig instance, ModFile modFile)
    {
        _instance = instance;
        _modFile = modFile;
    }

    public async Task Install()
    {
        var url = _modFile.Url;
        var tmpFile = Path.Combine(_instance.VersionPath!, "config", "BedrockBoot2", "mods", _modFile.FileName);
        var downloader = new GithubFilesDownloader();
        await downloader.DownloadAsync(url, tmpFile, Progress!);

        var isNative = _modFile.Type == "native.dll";

        var modManager = new ModsManager(_instance);
        modManager.AddMod(new()
        {
            File = tmpFile,
            InjectDelay = isNative ? 0 : 20000,
            IsPreLoad = isNative
        });
    }
}