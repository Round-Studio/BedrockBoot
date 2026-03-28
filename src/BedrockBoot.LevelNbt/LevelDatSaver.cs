using System.Reflection;
using System.Text;
using BedrockBoot.LevelNbt.Base.Entry;
using BedrockBoot.LevelNbt.Global;

namespace BedrockBoot.LevelNbt;

public static class LevelDatSaver
{
    /// <summary>
    /// 调用方式：LevelDatSaver.Save(path, data, data.HeaderVersion);
    /// </summary>
    public static void Save(string filePath, LevelWorldData data, int headerVersion)
    {
        Dictionary<string, object> root;

        // 1. 内部先读取原始 NBT 结构，确保不丢失未定义的标签
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var parser = new LevelDatParser(fs))
        {
            root = parser.GetRawRoot();
        }

        // 2. 将 LevelWorldData 的值通过反射回写到字典中
        ApplyDataToDictionary(root, data);

        // 3. 执行二进制写入
        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new BinaryWriter(fs, Encoding.UTF8))
        {
            // 写入头版本
            writer.Write(headerVersion);

            // 写入 NBT 大小占位符
            long sizePos = fs.Position;
            writer.Write(0);

            // 写入 NBT 内容
            long nbtStart = fs.Position;
            WriteCompound(writer, root);
            long nbtEnd = fs.Position;

            // 回填大小
            fs.Seek(sizePos, SeekOrigin.Begin);
            writer.Write((int)(nbtEnd - nbtStart));
        }
    }

    private static void ApplyDataToDictionary(Dictionary<string, object> dict, LevelWorldData data)
    {
        var type = typeof(LevelWorldData);
        foreach (var entry in TagMap.TagsMap)
        {
            var prop = type.GetProperty(entry.Value);
            if (prop == null) continue;

            object value = prop.GetValue(data);
            if (value != null) UpdateRecursive(dict, entry.Key, value);
        }
    }

    private static bool UpdateRecursive(Dictionary<string, object> dict, string key, object value)
    {
        if (dict.ContainsKey(key))
        {
            dict[key] = value is bool b ? (byte)(b ? 1 : 0) : value;
            return true;
        }
        foreach (var v in dict.Values)
        {
            if (v is Dictionary<string, object> nested && UpdateRecursive(nested, key, value)) 
                return true;
        }
        return false;
    }

    // --- 修正后的写入逻辑，解决了你提到的“模式无法访问”问题 ---
    private static void WriteCompound(BinaryWriter writer, Dictionary<string, object> dict)
    {
        foreach (var kvp in dict)
        {
            byte typeId = GetTagType(kvp.Value);
            writer.Write(typeId);
            WriteString(writer, kvp.Key);
            WriteTagValue(writer, typeId, kvp.Value);
        }
        writer.Write((byte)0); // TAG_End
    }

    private static void WriteTagValue(BinaryWriter writer, byte typeId, object value)
    {
        switch (typeId)
        {
            case 1: writer.Write((byte)value); break;
            case 2: writer.Write((short)value); break;
            case 3: writer.Write((int)value); break;
            case 4: writer.Write((long)value); break;
            case 5: writer.Write((float)value); break;
            case 6: writer.Write((double)value); break;
            case 7: // Byte Array
                byte[] bArr = (byte[])value;
                writer.Write(bArr.Length);
                writer.Write(bArr);
                break;
            case 8: WriteString(writer, (string)value); break;
            case 9: // List
                var list = (System.Collections.IList)value;
                if (list.Count > 0)
                {
                    byte innerType = GetTagType(list[0]);
                    writer.Write(innerType);
                    writer.Write(list.Count);
                    foreach (var item in list) WriteTagValue(writer, innerType, item);
                }
                else
                {
                    writer.Write((byte)0);
                    writer.Write(0);
                }
                break;
            case 10: WriteCompound(writer, (Dictionary<string, object>)value); break;
            case 11: // Int Array
                int[] iArr = (int[])value;
                writer.Write(iArr.Length);
                foreach (int i in iArr) writer.Write(i);
                break;
        }
    }

    private static void WriteString(BinaryWriter writer, string s)
    {
        byte[] b = Encoding.UTF8.GetBytes(s);
        writer.Write((ushort)b.Length);
        writer.Write(b);
    }

    private static byte GetTagType(object val) => val switch
    {
        byte => 1,
        short => 2,
        int => 3,
        long => 4,
        float => 5,
        double => 6,
        byte[] => 7,  // 数组必须放在 IList 之前
        int[] => 11,
        string => 8,
        System.Collections.IList => 9,
        Dictionary<string, object> => 10,
        _ => 0
    };
}