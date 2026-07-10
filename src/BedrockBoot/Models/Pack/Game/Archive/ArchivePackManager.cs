using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Base.Entry.Game.Pack.Archive.Export;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Pack.Game.Archive;

public class ArchivePackManager
{
    private readonly ArchiveInfo _info;
    public List<ResourcePackManifest>? InstalledPacks { get; private set; }
    public List<ResourcePackManifest>? ActivatedPacks { get; private set; }
    public List<ResourcePackManifest>? UnActivatedPacks { get; private set; }

    public ArchivePackManager(ArchiveInfo info)
    {
        _info = info;
    }

    public void Refresh()
    {
        var manager = new ResourcePackManager(_info.VersionInfo);
        InstalledPacks = manager.GetAllPack();

        if (ActivatedPacks == null)
            ActivatedPacks = new();

        if (UnActivatedPacks == null)
            UnActivatedPacks = new();
        
        ActivatedPacks.Clear();
        UnActivatedPacks.Clear();

        var resConf =
            new ConfigEntity<List<PackItem>>(Path.Combine(_info.Path, "world_resource_packs.json"), false);
        var behConf =
            new ConfigEntity<List<PackItem>>(Path.Combine(_info.Path, "world_behavior_packs.json"), false);

        foreach (var pack in InstalledPacks)
        {
            if (resConf.Data.Select(x => x.PackId).Contains(pack.Header.Uuid) ||
                behConf.Data.Select(x => x.PackId).Contains(pack.Header.Uuid))
            {
                ActivatedPacks.Add(pack);
            }
            else
            {
                UnActivatedPacks.Add(pack);
            }
        }
    }

    public List<ResourcePackManifest> GetActivatedPacks(ResourcePackType type)
    {
        if (type == ResourcePackType.Skin ||
            type == ResourcePackType.WorldTemplate ||
            type == ResourcePackType.Unknown)
        {
            throw new Exception("不支持的资源类型");
        }

        if (ActivatedPacks == null) Refresh();

        return ActivatedPacks!.Where(x => x.PackType == type).ToList();
    }

    public List<ResourcePackManifest> GetUnActivatedPacks(ResourcePackType type)
    {
        if (type == ResourcePackType.Skin ||
            type == ResourcePackType.WorldTemplate ||
            type == ResourcePackType.Unknown)
        {
            throw new Exception("不支持的资源类型");
        }

        if (UnActivatedPacks == null) Refresh();

        return UnActivatedPacks!.Where(x => x.PackType == type).ToList();
    }

    public void UninstallPack(ResourcePackManifest packManifest)
    {
        var uuid = packManifest.Header.Uuid;
        var conf = new ConfigEntity<List<PackItem>>(
            Path.Combine(_info.Path,
                $"world_{(packManifest.PackType == ResourcePackType.Resource ? "resource" : "behavior")}_packs.json"));
        
        conf.Data.RemoveAt(conf.Data.FindIndex(x=>x.PackId == uuid));
        conf.Save();
        
        Refresh();
    }

    public void InstallPack(ResourcePackManifest packManifest)
    {
        var uuid = packManifest.Header.Uuid;
        var conf = new ConfigEntity<List<PackItem>>(
            Path.Combine(_info.Path,
                $"world_{(packManifest.PackType == ResourcePackType.Resource ? "resource" : "behavior")}_packs.json"));

        conf.Data.Add(new()
        {
            PackId = uuid,
            Version = InstalledPacks!.Find(x => x.Header.Uuid == uuid)!.Header.Version.Split('.').Select(int.Parse)
                .ToList()
        });
        conf.Save();
        
        Refresh();
    }
}