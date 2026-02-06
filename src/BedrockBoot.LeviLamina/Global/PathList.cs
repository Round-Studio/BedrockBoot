namespace BedrockBoot.LeviLamina.Global;

public class PathList
{
    public readonly static string LeviLaminaSourceFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RoundStudio",
        "BedrockBoot2", "BedrockBoot.LeviLamina", "Source");
    public readonly static string LeviLaminaTempFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RoundStudio",
        "BedrockBoot2", "BedrockBoot.LeviLamina", "Temp");
}