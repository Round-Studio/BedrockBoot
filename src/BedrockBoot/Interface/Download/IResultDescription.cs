using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Documents;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Info;

namespace BedrockBoot.Interface.Download;

public interface IDownloadResult
{
    public SearchResultItemInfo SearchInfo { get; set; }
    public bool IsHasManyFiles { get; }
    public Task<List<Control>?> DescriptionControls();
    public Task<uint> GetDownloadCount();
    public Task<bool> IsInstalled();
}