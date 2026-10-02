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
using BedrockBoot.Standard.Entity.Progress;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;

namespace BedrockBoot.Standard.Interface.Platform.Game;

public interface IDownload
{
    BuildInfo BuildInfo { get; set; }
    string InstallFolder { get; set; }
    string GameName { get; set; }
    bool IsUsePack { get; set; }

    Action<string, DownloadProgressInfo> DownloadProgress { get; set; }
    Action<string, double> MergeProgress { get; set; }
    Action<string, double> ExtractionProgress { get; set; }
    Action<string, DeploymentProgressInfo> DeploymentProgress { get; set; }
    Action<string> StatusText { get; set; }
    Action<InstallStates> InstallStateChanged { get; set; }
    Action<string, string, Exception> ErrorOccurred { get; set; }
    Action<VersionConfig> Completed { get; set; }

    bool IsCanInstall { get; set; }

    VersionConfig GameConfig { get; }

    string? LastComputedMd5 { get; }

    Task InstallAsync(string url, CancellationToken token = default, bool receiveDownloadLock = false);

    Task<bool> CheckMD5(string file, CancellationToken token = default, bool showError = true);
}