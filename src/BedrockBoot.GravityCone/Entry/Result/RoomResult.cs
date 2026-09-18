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

namespace BedrockBoot.GravityCone.Entry.Result;


public class StunResult
{
    [JsonPropertyName("udp_nat_type")] public int UdpNatType { get; set; }

    [JsonPropertyName("tcp_nat_type")] public int TcpNatType { get; set; }

    [JsonPropertyName("last_update_time")] public long LastUpdateTime { get; set; }

    [JsonPropertyName("public_ip")] public string[] PublicIp { get; set; } = Array.Empty<string>();

    [JsonPropertyName("min_port")] public int MinPort { get; set; }

    [JsonPropertyName("max_port")] public int MaxPort { get; set; }
}

public class RoomCreateResult
{
    [JsonPropertyName("code")] public string Code { get; set; } = string.Empty;

    [JsonPropertyName("game_port")] public int GamePort { get; set; }

    [JsonPropertyName("protocol")] public string Protocol { get; set; } = string.Empty;

    [JsonPropertyName("sub_protocol")] public string? SubProtocol { get; set; }

    [JsonPropertyName("online_count")] public int OnlineCount { get; set; }

    [JsonPropertyName("players")] public PlayerInfo[] Players { get; set; } = Array.Empty<PlayerInfo>();

    [JsonPropertyName("running")] public bool Running { get; set; }
}

public class RoomJoinResult
{
    [JsonPropertyName("room_code")] public string RoomCode { get; set; } = string.Empty;
    [JsonPropertyName("host_address")] public string HostAddress { get; set; } = string.Empty;
    [JsonPropertyName("game_port")] public int GamePort { get; set; }
    [JsonPropertyName("connected")] public bool Connected { get; set; }
    [JsonPropertyName("online_count")] public int OnlineCount { get; set; }
    [JsonPropertyName("players")] public List<PlayerInfo> Players { get; set; } = new();
    [JsonPropertyName("protocol")] public string Protocol { get; set; } = string.Empty;
    [JsonPropertyName("sub_protocol")] public string? SubProtocol { get; set; }
}