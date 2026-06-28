using System.IO;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Pack.System.DropFile;

public class DropFileCheck
{
    /// <summary>
    /// 获取对应类型文件的名字
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns></returns>
    public static string GetFileTypeName(string file) => GlobalKeys.DropOverTypesOfSupport[Path.GetExtension(file)].Name;
    public static SupportedFileType GetFileType(string file) => GlobalKeys.DropOverTypesOfSupport[Path.GetExtension(file)].Type;
}