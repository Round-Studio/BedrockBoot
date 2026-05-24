using BedrockBoot.Base.Enum.Type;

namespace BedrockBoot.Models.Pack.System.DropFile;

public class DropFileCheck
{
    /// <summary>
    /// 获取文件类型
    /// </summary>
    /// <param name="file">文件路径</param>
    /// <returns></returns>
    public static DropFileType CheckFile(string file)
    {
        return file.ToLower() switch
        {
            { } s when s.EndsWith(".zip") => DropFileType.Zip,
            { } s when s.EndsWith(".exe") => DropFileType.Exe,
            { } s when s.EndsWith(".dll") => DropFileType.Dll,
            { } s when s.EndsWith(".mcpack") => DropFileType.McPack,
            { } s when s.EndsWith(".mcaddon") => DropFileType.McAddon,
            { } s when s.EndsWith(".mcworld") => DropFileType.McWorld,
            { } s when s.EndsWith(".appx") => DropFileType.Appx,
            { } s when s.EndsWith(".rplck") => DropFileType.Rplck,
            _ => DropFileType.None
        };
    }

    /// <summary>
    /// 获取对应类型文件的名字
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns></returns>
    public static string GetFileTypeName(DropFileType type) => type switch
    {
        DropFileType.Zip => "压缩文件",
        DropFileType.Exe => "Windows 可执行文件",
        DropFileType.Dll => "动态链接库",
        DropFileType.McPack => "资源包",
        DropFileType.McAddon => "资源包",
        DropFileType.McWorld => "存档包",
        DropFileType.Appx => "应用包",
        DropFileType.Rplck => "插件包",
        _ => "不支持的文件"
    };
}