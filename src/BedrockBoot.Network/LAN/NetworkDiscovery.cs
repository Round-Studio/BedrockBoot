using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace BedrockBoot.Network.LAN;

public sealed class NetworkDiscovery : IDisposable
{
    private const int Port = 37123;

    private readonly UdpClient _client;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, NetworkDevice> _devices = new();

    public event Action<IReadOnlyList<NetworkDevice>>? DevicesChanged;

    public IReadOnlyList<NetworkDevice> Devices =>
        _devices.Values.ToList();

    public NetworkDiscovery()
    {
        _client = new UdpClient();

        _client.EnableBroadcast = true;

        _client.Client.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.ReuseAddress,
            true);

        _client.Client.Bind(
            new IPEndPoint(
                IPAddress.Any,
                Port));
    }

    public async Task StartAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var result =
                    await _client.ReceiveAsync(
                        _cts.Token);

                ProcessPacket(
                    result.Buffer,
                    result.RemoteEndPoint);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    private void ProcessPacket(
        byte[] data,
        IPEndPoint endpoint)
    {
        NetworkPacket? packet;

        try
        {
            packet =
                JsonSerializer.Deserialize<NetworkPacket>(
                    Encoding.UTF8.GetString(data));
        }
        catch
        {
            return;
        }

        if (packet == null ||
            packet.Type != "BedrockBoot" ||
            string.IsNullOrWhiteSpace(packet.DeviceId))
        {
            return;
        }

        if (packet.DeviceId == NetworkIdentity.DeviceId)
            return;

        var device = new NetworkDevice(
            packet.DeviceId,
            packet.DeviceName ?? "Unknown",
            endpoint.Address,
            packet.Port);

        _devices.AddOrUpdate(
            packet.DeviceId,
            device,
            (_, _) => device);

        DevicesChanged?.Invoke(
            _devices.Values.ToList());
    }

    public void Dispose()
    {
        _cts.Cancel();
        _client.Dispose();
        _cts.Dispose();
    }

    private sealed class NetworkPacket
    {
        public string? Type { get; set; }

        public int Version { get; set; }

        public string? DeviceId { get; set; }

        public string? DeviceName { get; set; }

        public int Port { get; set; }
    }
}

public sealed record NetworkDevice(
    string DeviceId,
    string DeviceName,
    IPAddress Address,
    int Port);