namespace BedrockBoot.Chunker.Base.Entry.Info;

public class JavaInfo
{ 
    public bool Is64bit { get; init; }
    public string JavaPath { get; init; }
    public string JavaType { get; init; }
    public string JavaVersion { get; init; }
    public int MajorVersion { get; init; }

    public string JavaFolder => Path.GetDirectoryName(JavaPath);

    public override string ToString() {
        return $"{JavaVersion} - {JavaType} - {JavaPath}";
    }
}