namespace BedrockBoot.Base.Enum.Language;

[AttributeUsage(AttributeTargets.Field)]
public class LanguageResourceAttribute : Attribute
{
    public string Uri { get; }
    
    public LanguageResourceAttribute(string uri)
    {
        Uri = uri;
    }
}

public enum LanguageEnum
{
    [LanguageResource("avares://BedrockBoot/I18n/zh_CN.axaml")]
    Chinese,
    
    [LanguageResource("avares://BedrockBoot/I18n/en_US.axaml")]
    English,
}