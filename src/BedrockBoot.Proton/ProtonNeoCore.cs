using BedrockBoot.Models.Global;

namespace BedrockBoot.Proton;

public class ProtonNeoCore
{
    public static string ProtonRootPath => Path.Combine(PathsList.NeoProtonPath, "proton", "GDK-Proton-xuser");
    public ProtonNeoCore()
    {
        if (OperatingSystem.IsWindows())
            return;
    }

    public static bool IsInstalledKits()
    {
        var rootPath = PathsList.NeoProtonPath;
        var protonPath = ProtonRootPath;
        var umuPath = Path.Combine(rootPath);
        var gameFix = Path.Combine(rootPath, "gameFix");

        return Path.Exists(protonPath) || Path.Exists(umuPath) || Path.Exists(gameFix);
    }
}