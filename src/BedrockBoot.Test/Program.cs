using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Models.Translate;

var translationService = new MicrosoftTranslateService();

// 2. 创建翻译器实例
var translator = new ResourcePackTranslate(translationService);

// 3. 翻译资源包 - 从默认英文(en_US)翻译为目标语言
await translator.TranslatePackageAsync(
    packagePath: @"J:\enPack.mcpack",  // 输入包路径
    targetLanguage: "zh_CN",                            // 目标语言
    outputPath: @"J:\zhCNPack.mcpack", // 输出包路径（可选，默认覆盖原包）
    progressCallback: (progress, status) =>
    {
        Console.WriteLine($"进度: {progress:F2}% - {status}");
    }
);
