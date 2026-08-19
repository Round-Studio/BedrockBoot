using System;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Account.Microsoft;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Network.LAN;

namespace BedrockBoot.Microsoft
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Task.Run(async () =>
            {
                var server = new FakeServer();

                await server.StartAsync();
            });
            
            var discovery = new NetworkDiscovery();

            discovery.DevicesChanged += devices =>
            {
                Console.WriteLine(
                    $"设备数量: {devices.Count}");

                foreach (var device in devices)
                {
                    Console.WriteLine(
                        $"{device.DeviceName} " +
                        $"{device.Address}:{device.Port}");
                }
            };

            await discovery.StartAsync();

            Console.ReadKey();
        }
    }
}

