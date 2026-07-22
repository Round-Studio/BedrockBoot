namespace BedrockBoot.Lip.Global;

public class PathMappings
{
    public List<(string src, string dest)> PathMappingsList { get; } = new()
    {
        ("mods/", "config/BedrockBoot2/preload/"),
        ("mods/LeviLamina/", "config/BedrockBoot2/preload/")
    };
    
    public List<string> DontInstallDeepsList { get; } = new()
    {
        "github.com/LiteLDev/PeEditor",
        "github.com/LiteLDev/PreLoader"
    };
}