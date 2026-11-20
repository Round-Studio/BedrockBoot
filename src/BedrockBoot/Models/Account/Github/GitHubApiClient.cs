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

using BedrockBoot.Models.Global;
using Octokit;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Account.Github;

public static class GitHubApiClient
{
    public static string? GetUserToken()
    {
        var config = new ConfigEntity<GithubAccountInfo>(PathsList.GithubAccount, false);
        return config.Data?.UserToken;
    }

    public static GitHubClient CreateClient()
    {
        var client = new GitHubClient(new ProductHeaderValue("BedrockBoot"));
        var token = GetUserToken();
        if (!string.IsNullOrEmpty(token))
            client.Credentials = new Credentials(token);

        return client;
    }
}
