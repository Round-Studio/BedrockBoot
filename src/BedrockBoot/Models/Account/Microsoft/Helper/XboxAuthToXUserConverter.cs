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
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Account.Microsoft;

namespace BedrockBoot.Models.Account.Microsoft.Helper;

public class XboxAuthToXUserConverter
{
    private readonly XboxAuthClient _authClient = new();

    public async Task<(byte[] privateKey, string token)> ConvertToXUserCredentials(XboxAuthEntry.AuthResult authResult)
    {
        byte[] privateKey = GenerateP256PrivateKey();

        string? xboxUserToken = await _authClient.GetXboxUserTokenAsync(authResult.AccessToken);
        if (string.IsNullOrEmpty(xboxUserToken))
            throw new Exception("获取 Xbox User Token 失败");

        var (xstsToken, userHash, xuid) = await _authClient.GetXstsTokenAsync(xboxUserToken);
        if (string.IsNullOrEmpty(xstsToken))
            throw new Exception("获取 XSTS Token 失败");

        string token = BuildXUserToken(xboxUserToken, xstsToken, userHash, xuid);

        return (privateKey, token);
    }

    private byte[] GenerateP256PrivateKey()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return ecdsa.ExportPkcs8PrivateKey();
    }

    private string BuildXUserToken(string xboxUserToken, string xstsToken, string? userHash, string? xuid)
    {
        var tokenData = new
        {
            xbox_user_token = xboxUserToken,
            xsts_token = xstsToken,
            user_hash = userHash,
            xuid = xuid,
            device_token = "",
            expires_at = DateTime.UtcNow.AddHours(1).ToString("o")
        };

        return JsonSerializer.Serialize(tokenData);
    }
}