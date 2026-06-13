using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Interface;
using Round.SDK.Global;
using Round.SDK.Helper;

namespace BedrockBoot.Models.Pack.Game.ResourcePack;

public class ResourcePackTranslate
{
    private const int MaxConcurrentTranslations = 10; // 最大并发翻译数
    private readonly SemaphoreSlim _translationSemaphore;
    private readonly ITranslationService _translationService;

    public ResourcePackTranslate(ITranslationService translationService)
    {
        _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
        _translationSemaphore = new SemaphoreSlim(MaxConcurrentTranslations, MaxConcurrentTranslations);
    }

    /// <summary>
    ///     翻译完整的资源包，包括子包
    /// </summary>
    /// <param name="packagePath">未解压的主包路径</param>
    /// <param name="targetLanguage">目标语言代码，如 zh_CN</param>
    /// <param name="outputPath">输出路径，如果为空则覆盖原包</param>
    /// <param name="progressCallback">进度回调函数，参数为当前进度百分比和状态描述</param>
    public async Task TranslatePackageAsync(string packagePath, string targetLanguage, string outputPath = null,
        Action<double, string> progressCallback = null)
    {
        if (string.IsNullOrEmpty(packagePath))
            throw new ArgumentException("Package path cannot be null or empty", nameof(packagePath));

        if (string.IsNullOrEmpty(targetLanguage))
            throw new ArgumentException("Target language cannot be null or empty", nameof(targetLanguage));

        progressCallback?.Invoke(0, "开始分析资源包...");

        var analysis = new ResourcePackAnalysis(packagePath);
        var packInfo = analysis.GetPackInfo();
        analysis.ExtractToTemp();

        var tempPath = packInfo.RootPath; // 使用分析类的临时路径
        var finalOutputPath = string.IsNullOrEmpty(outputPath) ? packagePath : outputPath;

        try
        {
            // 确保输出目录存在
            var outputDir = Path.GetDirectoryName(finalOutputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            // 查找所有lang文件
            var langFiles = Directory.GetFiles(tempPath, "*.lang", SearchOption.AllDirectories);
            progressCallback?.Invoke(10, "正在查找语言文件...");

            // 翻译主包语言文件
            await TranslateLangFilesInDirectory(tempPath, targetLanguage, progressCallback);

            // 查找并翻译子包
            var subPackages = FindSubPackages(tempPath);
            if (subPackages.Any())
            {
                var subPackageCount = subPackages.Count;
                for (var i = 0; i < subPackageCount; i++)
                {
                    var subPackagePath = subPackages[i];
                    progressCallback?.Invoke(20 + i * 50.0 / subPackageCount,
                        $"正在翻译子包 {i + 1}/{subPackageCount}: {Path.GetFileName(subPackagePath)}");

                    // 递归翻译子包
                    await TranslateLangFilesInDirectory(subPackagePath, targetLanguage, progressCallback);
                }
            }

            progressCallback?.Invoke(80, "正在更新语言列表...");
            // 更新languages.json（如果存在）
            UpdateLanguagesJson(tempPath, targetLanguage);

            progressCallback?.Invoke(95, "正在重新打包...");

            // 重新打包前确保临时目录存在且不为空
            if (Directory.Exists(tempPath) && Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories).Any())
            {
                // 如果输出文件已存在，先删除
                if (File.Exists(finalOutputPath)) File.Delete(finalOutputPath);

                ZipHelper.CreateZipFile(tempPath, finalOutputPath);

                // 验证文件是否创建成功
                if (File.Exists(finalOutputPath))
                    progressCallback?.Invoke(100, $"翻译完成！文件已保存到：{finalOutputPath}");
                else
                    throw new Exception("打包失败：输出文件未创建");
            }
            else
            {
                throw new Exception("打包失败：临时目录为空");
            }
        }
        catch (Exception ex)
        {
            progressCallback?.Invoke(-1, $"翻译失败：{ex.Message}");
            throw;
        }
        finally
        {
            // 清理临时目录（由ResourcePackAnalysis管理）
            if (Directory.Exists(tempPath))
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch
                {
                    // 忽略清理错误
                }

            _translationSemaphore?.Dispose();
        }
    }

    /// <summary>
    ///     翻译指定的包，支持多种源语言，包括子包
    /// </summary>
    /// <param name="packagePath">未解压的主包路径</param>
    /// <param name="sourceLanguage">源语言代码，默认为 en_US</param>
    /// <param name="targetLanguage">目标语言代码，如 zh_CN</param>
    /// <param name="outputPath">输出路径，如果为空则覆盖原包</param>
    /// <param name="progressCallback">进度回调函数，参数为当前进度百分比和状态描述</param>
    public async Task TranslatePackageWithSourceAsync(string packagePath, string sourceLanguage, string targetLanguage,
        string outputPath = null, Action<double, string> progressCallback = null)
    {
        if (string.IsNullOrEmpty(packagePath))
            throw new ArgumentException("Package path cannot be null or empty", nameof(packagePath));

        if (string.IsNullOrEmpty(sourceLanguage))
            throw new ArgumentException("Source language cannot be null or empty", nameof(sourceLanguage));

        if (string.IsNullOrEmpty(targetLanguage))
            throw new ArgumentException("Target language cannot be null or empty", nameof(targetLanguage));

        progressCallback?.Invoke(0, "开始分析资源包...");

        var analysis = new ResourcePackAnalysis(packagePath);
        var packInfo = analysis.GetPackInfo();
        analysis.ExtractToTemp();

        var tempPath = packInfo.RootPath; // 使用分析类的临时路径
        var finalOutputPath = string.IsNullOrEmpty(outputPath) ? packagePath : outputPath;

        try
        {
            // 确保输出目录存在
            var outputDir = Path.GetDirectoryName(finalOutputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            // 翻译主包语言文件
            await TranslateLangFilesInDirectory(tempPath, targetLanguage, sourceLanguage, progressCallback);

            // 查找并翻译子包
            var subPackages = FindSubPackages(tempPath);
            if (subPackages.Any())
            {
                var subPackageCount = subPackages.Count;
                for (var i = 0; i < subPackageCount; i++)
                {
                    var subPackagePath = subPackages[i];
                    progressCallback?.Invoke(20 + i * 50.0 / subPackageCount,
                        $"正在翻译子包 {i + 1}/{subPackageCount}: {Path.GetFileName(subPackagePath)}");

                    // 递归翻译子包
                    await TranslateLangFilesInDirectory(subPackagePath, targetLanguage, sourceLanguage,
                        progressCallback);
                }
            }

            progressCallback?.Invoke(80, "正在更新语言列表...");
            // 更新languages.json（如果存在）
            UpdateLanguagesJson(tempPath, targetLanguage);

            progressCallback?.Invoke(95, "正在重新打包...");

            // 重新打包前确保临时目录存在且不为空
            if (Directory.Exists(tempPath) && Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories).Any())
            {
                // 如果输出文件已存在，先删除
                if (File.Exists(finalOutputPath)) File.Delete(finalOutputPath);

                ZipHelper.CreateZipFile(tempPath, finalOutputPath);

                // 验证文件是否创建成功
                if (File.Exists(finalOutputPath))
                    progressCallback?.Invoke(100, $"翻译完成！文件已保存到：{finalOutputPath}");
                else
                    throw new Exception("打包失败：输出文件未创建");
            }
            else
            {
                throw new Exception("打包失败：临时目录为空");
            }
        }
        catch (Exception ex)
        {
            progressCallback?.Invoke(-1, $"翻译失败：{ex.Message}");
            throw;
        }
        finally
        {
            // 清理临时目录（由ResourcePackAnalysis管理）
            if (Directory.Exists(tempPath))
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch
                {
                    // 忽略清理错误
                }
        }
    }

    /// <summary>
    ///     在指定目录中查找子包（子文件夹）
    /// </summary>
    /// <param name="rootPath">根路径</param>
    /// <returns>子包路径列表</returns>
    private List<string> FindSubPackages(string rootPath)
    {
        var subPackages = new List<string>();

        // 查找可能包含子包的目录（通常是嵌套的文件夹结构）
        var directories = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories);

        foreach (var dir in directories)
        {
            // 检查目录是否包含语言文件或其他资源包特征
            var langFiles = Directory.GetFiles(dir, "*.lang", SearchOption.TopDirectoryOnly);
            var manifestFiles = Directory.GetFiles(dir, "manifest.json", SearchOption.TopDirectoryOnly);

            if (langFiles.Length > 0 || manifestFiles.Length > 0) subPackages.Add(dir);
        }

        return subPackages;
    }

    /// <summary>
    ///     翻译指定目录中的所有语言文件
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <param name="targetLanguage">目标语言</param>
    /// <param name="progressCallback">进度回调</param>
    private async Task TranslateLangFilesInDirectory(string directoryPath, string targetLanguage,
        Action<double, string> progressCallback = null)
    {
        var langFiles = Directory.GetFiles(directoryPath, "*.lang", SearchOption.AllDirectories);

        if (langFiles.Length == 0)
            return;

        // 默认源语言为en_US
        await ProcessLangFiles(langFiles, targetLanguage, "en_US", progressCallback);
    }

    /// <summary>
    ///     翻译指定目录中的所有语言文件（带源语言）
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <param name="targetLanguage">目标语言</param>
    /// <param name="sourceLanguage">源语言</param>
    /// <param name="progressCallback">进度回调</param>
    private async Task TranslateLangFilesInDirectory(string directoryPath, string targetLanguage, string sourceLanguage,
        Action<double, string> progressCallback = null)
    {
        var langFiles = Directory.GetFiles(directoryPath, "*.lang", SearchOption.AllDirectories);

        if (langFiles.Length == 0)
            return;

        await ProcessLangFiles(langFiles, targetLanguage, sourceLanguage, progressCallback);
    }

    /// <summary>
    ///     处理语言文件列表
    /// </summary>
    /// <param name="langFiles">语言文件列表</param>
    /// <param name="targetLanguage">目标语言</param>
    /// <param name="sourceLanguage">源语言</param>
    /// <param name="progressCallback">进度回调</param>
    private async Task ProcessLangFiles(string[] langFiles, string targetLanguage, string sourceLanguage,
        Action<double, string> progressCallback = null)
    {
        // 查找指定源语言文件
        var sourceLangFile = langFiles.FirstOrDefault(f =>
            Path.GetFileName(f).Equals($"{sourceLanguage}.lang", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(sourceLangFile))
        {
            // 如果没找到指定的源语言文件，尝试查找其他英文语言文件
            sourceLangFile = langFiles.FirstOrDefault(f =>
                Path.GetFileName(f).Equals("en_US.lang", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(f).Equals("en_GB.lang", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(f).Contains("en_"));

            if (string.IsNullOrEmpty(sourceLangFile))
            {
                // 尝试使用任意一个可用的语言文件作为源
                sourceLangFile = langFiles.FirstOrDefault();
                if (string.IsNullOrEmpty(sourceLangFile)) return; // 没有任何语言文件可以翻译
            }
        }

        // 读取源语言文件内容
        var sourceEntries = ReadLangFile(sourceLangFile);

        if (sourceEntries.Count == 0)
            return;

        // 翻译内容
        var translatedEntries = await TranslateEntriesAsync(sourceEntries, targetLanguage, progressCallback);

        // 创建目标语言文件
        var targetLangFile = Path.Combine(Path.GetDirectoryName(sourceLangFile), $"{targetLanguage}.lang");
        WriteLangFile(translatedEntries, targetLangFile);
    }

    /// <summary>
    ///     读取lang文件内容
    /// </summary>
    /// <param name="filePath">lang文件路径</param>
    /// <returns>键值对字典</returns>
    private Dictionary<string, string> ReadLangFile(string filePath)
    {
        var entries = new Dictionary<string, string>();

        if (!File.Exists(filePath))
            return entries;

        var lines = File.ReadAllLines(filePath, Encoding.UTF8);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // 跳过空行和注释行
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith("##"))
                continue;

            // 查找第一个 '=' 字符
            var separatorIndex = trimmedLine.IndexOf('=');
            if (separatorIndex > 0)
            {
                var key = trimmedLine.Substring(0, separatorIndex).Trim();
                var valuePart = trimmedLine.Substring(separatorIndex + 1);

                // 提取值部分（在 # 之前的部分）
                var commentIndex = valuePart.IndexOf('#');
                var value = commentIndex >= 0 ? valuePart.Substring(0, commentIndex).Trim() : valuePart.Trim();

                // 处理值中的转义字符
                value = UnescapeValue(value);

                entries[key] = value;
            }
        }

        return entries;
    }

    /// <summary>
    ///     写入lang文件
    /// </summary>
    /// <param name="entries">翻译条目</param>
    /// <param name="filePath">输出文件路径</param>
    private void WriteLangFile(Dictionary<string, string> entries, string filePath)
    {
        var lines = new List<string>();

        // 添加文件头注释
        lines.Add("# Translated resource pack language file");
        lines.Add($"# Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        lines.Add("");

        foreach (var entry in entries)
        {
            var escapedValue = EscapeValue(entry.Value);
            lines.Add($"{entry.Key}={escapedValue}	#");
        }

        File.WriteAllLines(filePath, lines, Encoding.UTF8);
    }

    /// <summary>
    ///     更新languages.json文件，添加新语言
    /// </summary>
    /// <param name="rootPath">包根路径</param>
    /// <param name="newLanguage">新增的语言代码</param>
    private void UpdateLanguagesJson(string rootPath, string newLanguage)
    {
        var textsDir = Path.Combine(rootPath, "texts");
        if (!Directory.Exists(textsDir))
            return;

        var languagesJsonPath = Path.Combine(textsDir, "languages.json");

        var existingLanguages = new List<string>();

        // 读取现有语言列表
        if (File.Exists(languagesJsonPath))
        {
            var content = File.ReadAllText(languagesJsonPath);
            existingLanguages = JsonSerializer.Deserialize<List<string>>(content, JsonSerializerOption.Options) ??
                                new List<string>();
        }

        // 添加新语言（如果不存在）
        if (!existingLanguages.Contains(newLanguage))
        {
            existingLanguages.Add(newLanguage);
            existingLanguages.Sort(); // 排序以保持一致性

            var updatedContent = JsonSerializer.Serialize(existingLanguages, JsonSerializerOption.Options);
            File.WriteAllText(languagesJsonPath, updatedContent);
        }
    }

    /// <summary>
    ///     翻译条目集合（使用批处理方式控制并发）
    /// </summary>
    private async Task<Dictionary<string, string>> TranslateEntriesAsync(
        Dictionary<string, string> entries,
        string targetLanguage,
        Action<double, string> progressCallback = null)
    {
        var translatedEntries = new Dictionary<string, string>();
        var lockObject = new object();

        var totalEntries = entries.Count;
        if (totalEntries == 0)
        {
            progressCallback?.Invoke(100, "无需翻译任何条目");
            return translatedEntries;
        }

        var keys = entries.Keys.ToList();
        var sourceLangCode = "en";
        var targetLangCode = GetLanguageCodeFromFullCode(targetLanguage);

        progressCallback?.Invoke(30, $"开始翻译 {totalEntries} 个条目，从 {sourceLangCode} 到 {targetLangCode}...");

        var processedCount = 0;
        var batchSize = MaxConcurrentTranslations; // 每批处理的数量等于最大并发数

        // 分批处理，确保同时最多只有 MaxConcurrentTranslations 个任务
        for (var i = 0; i < keys.Count; i += batchSize)
        {
            var batchKeys = keys.Skip(i).Take(batchSize).ToList();
            var batchTasks = new List<Task>();

            foreach (var key in batchKeys)
                batchTasks.Add(Task.Run(async () =>
                {
                    var value = entries[key];

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        lock (lockObject)
                        {
                            translatedEntries[key] = value;
                        }

                        var currentProgress = Interlocked.Increment(ref processedCount);
                        var progress = 30 + (double)currentProgress / totalEntries * 50;
                        progressCallback?.Invoke(progress, $"跳过占位符条目 ({currentProgress}/{totalEntries})");
                        return;
                    }

                    try
                    {
                        var translatedValue =
                            await _translationService.TranslateAsync(value, sourceLangCode, targetLangCode);

                        lock (lockObject)
                        {
                            translatedEntries[key] = translatedValue;
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (lockObject)
                        {
                            translatedEntries[key] = value;
                        }

                        Console.WriteLine($@"翻译失败 [{key}]: {ex.Message}");
                    }
                    finally
                    {
                        var currentProgress = Interlocked.Increment(ref processedCount);
                        var progress = 30 + (double)currentProgress / totalEntries * 50;
                        progressCallback?.Invoke(progress, $"正在翻译 ({currentProgress}/{totalEntries})");
                    }
                }));

            // 等待当前批次的所有任务完成
            await Task.WhenAll(batchTasks);
        }

        var translatedCount = translatedEntries.Count(kv => kv.Value != entries[kv.Key]);
        progressCallback?.Invoke(80, $"翻译完成，成功翻译 {translatedCount}/{totalEntries} 个条目");

        return translatedEntries;
    }

    /// <summary>
    ///     从完整语言代码中提取基础语言代码
    ///     例如：zh_CN -> zh, en_US -> en
    /// </summary>
    /// <param name="fullCode">完整语言代码</param>
    /// <returns>基础语言代码</returns>
    private string GetLanguageCodeFromFullCode(string fullCode)
    {
        var parts = fullCode.Split('_');
        return parts.Length > 0 ? parts[0] : fullCode;
    }

    /// <summary>
    ///     反转义字符串值
    /// </summary>
    /// <param name="value">原始值</param>
    /// <returns>反转义后的值</returns>
    private string UnescapeValue(string value)
    {
        return value
            .Replace("\\n", "\n")
            .Replace("\\t", "\t")
            .Replace("\\r", "\r")
            .Replace("\\\"", "\"")
            .Replace("\\'", "'");
    }

    /// <summary>
    ///     转义字符串值
    /// </summary>
    /// <param name="value">原始值</param>
    /// <returns>转义后的值</returns>
    private string EscapeValue(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t")
            .Replace("\r", "\\r")
            .Replace("\"", "\\\"");
    }
}