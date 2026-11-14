namespace BedrockBoot.Standard.Entity.Game.Pack.Capes;

public class CapeFileInfo
{
    public string CapeImagePath { get; set; }
    public string CapeName { get; set; }
    public CapeFileType Type { get; set; }
}

public enum CapeFileType
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}