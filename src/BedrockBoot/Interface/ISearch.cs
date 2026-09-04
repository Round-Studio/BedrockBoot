using System.Collections.Generic;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Search;

namespace BedrockBoot.Interface;

public interface ISearch
{
    Task<List<SearchResultItemInfo>> SearchAsync(string keyword);
    Task<List<SearchResultItemInfo>> SearchAsync(string keyword, int page, int pageSize);
    SearchResourceType SearchType { get; }
    bool SupportsPagination { get; }
    void SetExtraParameter(object parameter);
    object GetExtraParameter();
}