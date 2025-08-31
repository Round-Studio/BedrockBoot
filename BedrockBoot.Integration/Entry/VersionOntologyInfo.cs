namespace BedrockBoot.Integration.Entry;

public class VersionOntologyInfo
{
    public string FolderPath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BasePath => Path.Combine(FolderPath, "bedrock_versions", Name);
}