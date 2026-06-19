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
