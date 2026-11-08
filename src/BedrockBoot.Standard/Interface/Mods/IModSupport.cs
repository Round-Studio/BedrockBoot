using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Interface.ModLoader;

namespace BedrockBoot.Standard.Interface.Mods;

public interface IModSupport
{
    string ModName { get; }
    string Tag { get; }
    string SupportName { get; }
    string ModType { get; }
    object? SupportPage { get; }
    string Glyph { get; }
    VersionConfig Instance { get; set; }
    bool IsEnable();
}

public interface IModSupportFactory
{
    IModSupport Create(VersionConfig instance);
}