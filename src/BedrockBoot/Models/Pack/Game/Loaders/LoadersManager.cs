using System;
using System.Collections.Generic;
using BedrockBoot.Interface;
using BedrockBoot.Models.Pack.Game.Loaders.LoaderInstance;

namespace BedrockBoot.Models.Pack.Game.Loaders;

public class LoadersManager
{
    public static List<Type> ModsLoaders { get; } = new List<Type>
    {
        typeof(PreLoaderNet),
        typeof(LoaderInstance.LeviLamina)
    };
}