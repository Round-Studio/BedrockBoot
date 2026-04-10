using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Server;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Pack.Game.Isolation;

namespace BedrockBoot.Models.Pack.Game.Server;

public class ServerManager
{
    public ServerManager(VersionConfig versionInfo)
    {
        VersionConfig = versionInfo;
    }

    public VersionConfig VersionConfig { get; set; }

    public void AddServer(string user, ServerItemInfo server)
    {
        var configFile = GetServerConfigFilesPath().Where(co => co.Key == user).First().Value;
        var lines = File.Exists(configFile) ? File.ReadAllLines(configFile).ToList() : new List<string>();
        var lineIndex = lines.Count + 1;
        lines.Add($"{lineIndex}:{GetServerConfigLine(server)}");

        if (!Directory.Exists(Path.GetDirectoryName(configFile)))
            Directory.CreateDirectory(Path.GetDirectoryName(configFile));

        File.WriteAllLines(configFile, lines);
    }

    public void DeleteServer(string user, ServerItemInfo info)
    {
        var configFile = GetServerConfigFilesPath().Where(co => co.Key == user).First().Value;
        var lines = File.Exists(configFile) ? File.ReadAllLines(configFile).ToList() : new List<string>();

        lines.RemoveAll(line => line.Split(':')[0].Contains(info.Id.ToString()));

        if (!Directory.Exists(Path.GetDirectoryName(configFile)))
            Directory.CreateDirectory(Path.GetDirectoryName(configFile));

        File.WriteAllLines(configFile, lines);
    }

    public string GetServerConfigLine(ServerItemInfo server)
    {
        return $"{server.ServerName}:{server.ServerAddress}:{server.ServerPort}";
    }

    public Dictionary<string, List<ServerItemInfo>> GetServers()
    {
        var result = new Dictionary<string, List<ServerItemInfo>>();
        GetServerConfigFilesPath().ToList().ForEach(user =>
        {
            var lst = new List<ServerItemInfo>();

            if (File.Exists(user.Value))
            {
                var lines = File.ReadAllLines(user.Value).ToList();
                lines.ForEach(line =>
                {
                    var split = line.Split(':');
                    lst.Add(new ServerItemInfo
                    {
                        ServerName = split[1],
                        ServerAddress = split[2],
                        ServerPort = int.Parse(split[3]),
                        Id = int.Parse(split[0]),
                        VersionConfig = VersionConfig
                    });
                });
            }

            result.Add(user.Key, lst);
        });

        return result;
    }

    private Dictionary<string, string> GetServerConfigFilesPath()
    {
        var result = new Dictionary<string, string>();
        if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
            result.Add("Shared", Path.Combine(
                IsolationCore.GetInstanceFolderPath(VersionConfig, InstanceFolderType.OptionFolder),
                "external_servers.txt"
            ));
        else if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.GDK)
            IsolationCore.GetInstanceUsers(VersionConfig).ForEach(user =>
            {
                result.Add(user, Path.Combine(
                    IsolationCore.GetInstanceFolderPath(VersionConfig, InstanceFolderType.OptionFolder, user),
                    "external_servers.txt"
                ));
            });

        return result;
    }
}