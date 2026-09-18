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
using System.Collections.Generic;
using System.Linq;

namespace BedrockBoot.Service.Protocol;

public class ProtocolRouteRegistry
{
    private readonly Dictionary<string, IProtocolRoute> _routes = new();

    public static ProtocolRouteRegistry Instance { get; } = new();

    public void Register(IProtocolRoute route)
    {
        var key = route.RouteName.ToLower();
        _routes[key] = route;
        Console.WriteLine($@"协议路由已注册: bedrockboot://{key}");
    }

    public void RegisterRange(IEnumerable<IProtocolRoute> routes)
    {
        foreach (var route in routes)
            Register(route);
    }

    public IProtocolRoute? Get(string name)
    {
        _routes.TryGetValue(name.ToLower(), out var route);
        return route;
    }

    public void Unregister(string name)
    {
        _routes.Remove(name.ToLower());
    }

    public void UnregisterAll()
    {
        _routes.Clear();
    }

    public bool Contains(string name)
    {
        return _routes.ContainsKey(name.ToLower());
    }

    public IEnumerable<string> GetRegisteredRouteNames()
    {
        return _routes.Keys.OrderBy(k => k).ToList();
    }
}
