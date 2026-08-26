using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Documents;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Info;

namespace BedrockBoot.Interface.Download;

public interface IDownloadResult
{
    SearchResultItemInfo SearchInfo { get; set; }
    Task<List<Control>?> DescriptionControls();
    Task<uint> GetDownloadCount();
}