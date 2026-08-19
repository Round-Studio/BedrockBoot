namespace BedrockBoot.Network.LAN;

public static class NetworkIdentity
{
    public static string DeviceId { get; } =
        Guid.NewGuid().ToString("N");
}