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

namespace BedrockBoot.Models.Account.Microsoft;

public static class Constants
{
    public const string MsaClientId = "0000000048183522";
    public const string MsaScope = "service::user.auth.xboxlive.com::MBI_SSL";
    public const string MsaConnectUrl = "https://login.live.com/oauth20_connect.srf";
    public const string MsaTokenUrl = "https://login.live.com/oauth20_token.srf";
    
    public const string XboxUserAuthEndpoint = "https://user.auth.xboxlive.com/user/authenticate";
    public const string XstsAuthEndpoint = "https://xsts.auth.xboxlive.com/xsts/authorize";
    
    public const string PeopleHubEndpoint = "https://peoplehub.xboxlive.com/users/xuid({0})/people/social";
    public const string ProfileEndpoint = "https://profile.xboxlive.com/users/xuid({0})/profile/settings?settings=Gamertag,GameDisplayPicRaw";
    
    public const string SisuAuthorizeEndpoint = "https://sisu.xboxlive.com/authorize";
    public const string SisuRelyingParty = "https://b980a380.minecraft.playfabapi.com/";
    
    public const string RedirectUri = "http://127.0.0.1:58423";
}