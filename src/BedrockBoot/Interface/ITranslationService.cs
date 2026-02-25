using System.Collections.Generic;
using System.Threading.Tasks;

namespace BedrockBoot.Interface;

/// <summary>
/// 翻译服务接口
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// 翻译单个文本
    /// </summary>
    /// <param name="text">待翻译文本</param>
    /// <param name="sourceLanguage">源语言代码</param>
    /// <param name="targetLanguage">目标语言代码</param>
    /// <returns>翻译结果</returns>
    Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage);
}