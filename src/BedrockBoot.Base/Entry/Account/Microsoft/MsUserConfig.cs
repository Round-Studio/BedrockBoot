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

using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Account.Microsoft;

public class MsUserConfig
{
    [JsonPropertyName("buid")] public string BUID { get; set; } = Guid.NewGuid().ToString("N");
    [JsonPropertyName("msAuth")] public XboxAuthEntry.AuthResult? AuthResult { get; set; }
    [JsonPropertyName("userName")] public string? UserName { get; set; }
    [JsonPropertyName("userIconUrl")] public string? UserIconUrl { get; set; }
    [JsonPropertyName("isDefault")] public bool IsDefault { get; set; } = false;
}

public class MsUserConfigRoot
{
    [JsonPropertyName("accounts")] public List<MsUserConfig> Accounts { get; set; } = new();
    [JsonPropertyName("selectUserBuid")] public string? SelectUserBUID { get; set; }
}