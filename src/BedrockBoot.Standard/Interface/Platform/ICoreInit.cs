namespace BedrockBoot.Standard.Interface.Platform;

public interface ICoreInit
{
    Task InitializeAsync();
    void UpdateUseHardwareDecode(bool isUse);
}