using System;
using System.Collections.Generic;
using BedrockBoot.Models.Pack.DllMods.LeviLamina.Arke;
using BedrockBoot.Models.Pack.Game.Loaders.LoaderInstance;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Interface.Mods;

namespace BedrockBoot.Models.Pack.DllMods;

public class ModSupports
{
    public static List<Type> Supports { get; } = new()
    {
        typeof(ArkeSupportFactory)
    };

    public static List<IModSupport> GetEnableSupports(VersionConfig instance)
    {
        var supports = new List<IModSupport>();
        foreach (var supportType in Supports)
        {
            if (Activator.CreateInstance(supportType) is IModSupportFactory supportFac)
            {
                var support = supportFac.Create(instance);
                if (support.IsEnable())
                {
                    supports.Add(support);
                }
            }
        }
        return supports;
    }
}