using System.Collections.Generic;
using System.IO;
using System.IO.Compression; // 添加这个命名空间
using System.Linq;
using System.Text;
using System.Text.Json;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using fNbt;

namespace BedrockBoot.Models.Pack.Game.Archive;

public class ArchiveSerializer : ArchiveInfo
{
    public ArchiveSerializer(string levelPath)
    {
        Path = levelPath;
    }

    public ArchiveInfo LoadInfo()
    {
        var nbtPath = System.IO.Path.Combine(Path, "level.dat");
        
        if (!File.Exists(nbtPath))
            throw new FileNotFoundException("Level.dat file not found", nbtPath);
        
        // 先尝试解压 GZIP
        var nbtData = ReadGzippedFile(nbtPath);
        
        // 使用内存流加载 NBT 数据
        using var ms = new MemoryStream(nbtData);
        var nbt = new NbtFile();
        nbt.LoadFromStream(ms, NbtCompression.AutoDetect);
        
        // 转换为 C# 对象
        object rootObj = TagToObject(nbt.RootTag);
        
        // 生成自定义格式的字符串
        string customJson = ConvertToCustomFormat(rootObj);
        
        File.WriteAllText(System.IO.Path.Combine(Path, "level.json"), customJson);
        
        return this;
    }

    private byte[] ReadGzippedFile(string filePath)
    {
        using var fileStream = File.OpenRead(filePath);
        // 检查是否是 GZIP 文件（前两个字节是 0x1F 0x8B）
        byte[] header = new byte[2];
        fileStream.Read(header, 0, 2);
        fileStream.Seek(0, SeekOrigin.Begin);
        
        if (header[0] == 0x1F && header[1] == 0x8B)
        {
            // 是 GZIP 文件，进行解压
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using var memoryStream = new MemoryStream();
            gzipStream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
        else
        {
            // 不是 GZIP 文件，直接读取
            using var memoryStream = new MemoryStream();
            fileStream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }

    private static object TagToObject(NbtTag tag)
    {
        return tag.TagType switch
        {
            NbtTagType.Compound => CompoundToDict((NbtCompound)tag),
            NbtTagType.List => ListToList((NbtList)tag),
            NbtTagType.ByteArray => ((NbtByteArray)tag).ByteArrayValue,
            NbtTagType.IntArray => ((NbtIntArray)tag).IntArrayValue,
            NbtTagType.LongArray => ((NbtLongArray)tag).LongArrayValue,
            NbtTagType.String => ((NbtString)tag).StringValue,
            NbtTagType.Byte => ((NbtByte)tag).Value,
            NbtTagType.Short => ((NbtShort)tag).Value,
            NbtTagType.Int => ((NbtInt)tag).Value,
            NbtTagType.Long => ((NbtLong)tag).Value,
            NbtTagType.Float => ((NbtFloat)tag).Value,
            NbtTagType.Double => ((NbtDouble)tag).Value,
            NbtTagType.End => null,
            _ => tag.ToString()
        };
    }

    private static Dictionary<string, object> CompoundToDict(NbtCompound c)
    {
        var dict = new Dictionary<string, object>(c.Count);
        foreach (var sub in c)
            dict[sub.Name] = TagToObject(sub);
        return dict;
    }

    private static List<object> ListToList(NbtList l)
    {
        var list = new List<object>(l.Count);
        foreach (var sub in l)
            list.Add(TagToObject(sub));
        return list;
    }

    private string ConvertToCustomFormat(object obj, int indentLevel = 0)
    {
        var sb = new StringBuilder();
        string indent = new string(' ', indentLevel * 2);

        if (obj is Dictionary<string, object> dict)
        {
            sb.AppendLine("{");
            bool first = true;
            foreach (var kvp in dict)
            {
                if (!first)
                    sb.AppendLine(",");
                first = false;
                
                sb.Append(indent + "  " + kvp.Key + ": " + FormatValue(kvp.Value, indentLevel + 1));
            }
            sb.AppendLine();
            sb.Append(indent + "}");
        }
        else if (obj is List<object> list)
        {
            sb.Append("[");
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(FormatValue(list[i], indentLevel));
            }
            sb.Append("]");
        }
        else
        {
            sb.Append(FormatValue(obj, indentLevel));
        }

        return sb.ToString();
    }

    private string FormatValue(object value, int indentLevel)
    {
        if (value == null)
            return "null";

        if (value is byte b)
            return b + "b";
        
        if (value is bool boolVal)
            return boolVal ? "1b" : "0b";
        
        if (value is long l)
            return l + "L";
        
        if (value is float f)
            return f.ToString("0.#############################") + "f";
        
        if (value is double d)
            return d.ToString("0.#############################");
        
        if (value is string str)
        {
            // 如果字符串包含特殊字符或换行，需要转义
            if (str.Contains("\n") || str.Contains("\"") || str.Contains("\\"))
            {
                return JsonSerializer.Serialize(str);
            }
            return str;
        }
        
        if (value is Dictionary<string, object> dict)
            return ConvertToCustomFormat(dict, indentLevel);
        
        if (value is List<object> list)
            return ConvertToCustomFormat(list, indentLevel);
        
        if (value is byte[] byteArray)
            return "[" + string.Join(", ", byteArray) + "]";
        
        if (value is int[] intArray)
            return "[" + string.Join(", ", intArray) + "]";
        
        if (value is long[] longArray)
            return "[" + string.Join(", ", longArray.Select(x => x + "L")) + "]";

        return value.ToString();
    }
}