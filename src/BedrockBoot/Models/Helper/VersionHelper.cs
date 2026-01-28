using System;
using System.Collections.Generic;
using System.Linq;
using BedrockBoot.Models.Global;
using BedrockLauncher.Core.VersionJsons;

namespace BedrockBoot.Models.Helper;

public class VersionHelper
{
    public static List<BuildInfo> GetVersions()
    {
        var lst = VersionsHelper
            .GetBuildDatabaseAsync(
                SourceList.VersionDataSources.ToList()[GlobalModel.Config.Data.VersionSourceIndex].Value)
            .Result!.Builds
            .ToListAsync().Result;

        var versionCache = new List<(BuildInfo item, Version? version)>();

        foreach (var item in lst)
        {
            if (string.IsNullOrEmpty(item.Value.ID)) continue;
            if (item.Value.Variations.Count <= 0) continue;

            bool isCon = false;

            foreach (var v in item.Value.Variations)
            {
                if (v.MetaData.Count <= 0) isCon = true;
            }

            if (isCon) continue;

            Version? version = null;
            try
            {
                version = new Version(item.Value.ID);
            }
            catch { }
            
            versionCache.Add((item.Value, version));
        }

        // 使用缓存的 Version 对象进行排序
        versionCache.Sort((x, y) =>
        {
            // 两个都有有效版本号
            if (x.version != null && y.version != null)
            {
                return y.version.CompareTo(x.version); // 降序
            }

            // 只有一个有有效版本号，有效版本号排在前面
            if (x.version != null) return -1;
            if (y.version != null) return 1;

            // 两个都没有有效版本号，按原始字符串排序
            return string.Compare(y.item.ID, x.item.ID, StringComparison.Ordinal);
        });

        // 提取排序后的结果
        var sortedList = versionCache.Select(x => x.item).ToList();
        return sortedList;
    }
}