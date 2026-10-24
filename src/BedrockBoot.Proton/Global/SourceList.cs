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

using BedrockBoot.Proton.Enum;
using Octokit;

namespace BedrockBoot.Proton.Global;

public class SourceList
{
    private static GitHubClient _client = new GitHubClient(new ProductHeaderValue("BedrockBoot.Linux"));
    public static Dictionary<ProtonSource, Task<IReadOnlyList<Release>>> ProtonRepository { get; } = new()
    {
        {
            ProtonSource.WeatherOS,
            _client.Repository.Release.GetAll("Weather-OS", "GDK-Proton")
        },
        {
            ProtonSource.LukasPAH,
            _client.Repository.Release.GetAll("LukasPAH", "GDK-Proton-Custom")
        }
    };

    public static string GameFixUrl =>
        "https://github.com/RoundMCDev/ProtonGDK-Release/releases/download/Release10-32/GameRunningFixKit.tar.gz";
    public static string ProtonXUserUrl =>
        "https://github.com/RoundMCDev/ProtonGDK-Release/releases/download/Release10-32/GDK-Proton-xuser.tar.gz";
    public static string ProtonLauncher =>
        "https://github.com/RoundMCDev/ProtonGDK-Release/releases/download/Release10-32/Proton-Launch-umu.tar.gz";
    public static string GamePatchUrl =>
        "https://github.com/RoundMCDev/ProtonGDK-Release/releases/download/Release10-32/GamePatch.zip";
}