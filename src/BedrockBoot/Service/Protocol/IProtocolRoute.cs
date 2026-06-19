using System.Collections.Generic;
using System.Threading.Tasks;

namespace BedrockBoot.Service.Protocol;

public interface IProtocolRoute
{
    string RouteName { get; }

    Task ExecuteAsync(string[] segments, IReadOnlyDictionary<string, string> queryParams);
}
