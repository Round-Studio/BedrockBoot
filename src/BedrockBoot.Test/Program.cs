using BedrockBoot.GravityCone;

var client = new GravityConeClient();
await client.StartAsync(@"E:\gravitycone\gravitycone-cli-windows-amd64.exe", new List<string>(){"wss://center.node.1tmc.top"},"BedrockBoot","BedrockBoot 联机房间");
var code = await client.CreatePaperConnectRoomAsync("MinecraftYJQ_");   
Console.WriteLine($@"{code.Code}");

client._process.WaitForExit();