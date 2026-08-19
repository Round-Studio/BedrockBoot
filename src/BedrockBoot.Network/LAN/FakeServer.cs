using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace BedrockBoot.Network.LAN;

public sealed class FakeServer : IDisposable
{
    private const int Port = 37123;

    private readonly UdpClient _client;
    private readonly CancellationTokenSource _cts = new();

    public FakeServer()
    {
        _client = new UdpClient
        {
            EnableBroadcast = true
        };
    }

    public async Task StartAsync()
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(2));

        while (await timer.WaitForNextTickAsync(_cts.Token))
        {
            await BroadcastAsync();
        }
    }

    private async Task BroadcastAsync()
    {
        var packet = new
        {
            Type = "BedrockBoot",
            Version = 1,
            DeviceId = NetworkIdentity.DeviceId,
            DeviceName = Environment.MachineName,
            Port
        };

        var data = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(packet));

        await _client.SendAsync(
            data,
            data.Length,
            new IPEndPoint(
                IPAddress.Broadcast,
                Port));
    }

    public void Dispose()
    {
        _cts.Cancel();
        _client.Dispose();
        _cts.Dispose();
    }
}