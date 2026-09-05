using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Entry.Info.Download;

namespace BedrockBoot.Interface.Download;

public interface IDownloadResult
{
    public SearchResultItemInfo SearchInfo { get; set; }
    public bool IsHasManyFiles { get; }
    public Task<List<Control>?> DescriptionControls();
    public Task<uint> GetDownloadCount();
    public Task<bool> IsInstalled();
    public Task Install();
    public Task ReInstall();
    public void Delete();
    public Task<List<ResourceFileInfo>> GetFiles();
}