using System.Reflection;
using System.Text;
using BedrockBoot.LevelNbt.Base.Entry;
using BedrockBoot.LevelNbt.Global;

namespace BedrockBoot.LevelNbt;

public class LevelDatParser : IDisposable
{
    private readonly BinaryReader _reader;
    private bool _disposed;

    public int WorldVersion { get; private set; }
    public int NBTDataSize { get; private set; }
    public LevelWorldData WorldData { get; private set; } = new();
    private Dictionary<string, object> _rawRoot;

    public Dictionary<string, object> GetRawRoot() => _rawRoot;

    // 支持 Stream 初始化
    public LevelDatParser(Stream stream)
    {
        _reader = new BinaryReader(stream, Encoding.UTF8);
        Parse();
    }

    // 支持 文件路径 初始化
    public LevelDatParser(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"文件不存在: {filePath}");

        // 使用 FileShare.ReadWrite 避免游戏运行中文件被锁定无法读取
        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        _reader = new BinaryReader(fileStream, Encoding.UTF8);
        Parse();
    }

    private void Parse()
    {
        try
        {
            if (_reader.BaseStream.CanSeek)
                _reader.BaseStream.Position = 0;

            // 1. 读取头信息 (Little Endian)
            WorldVersion = _reader.ReadInt32();
            NBTDataSize = _reader.ReadInt32();

            // --- 核心修改：将文件头版本同步到数据实体中 ---
            WorldData.HeaderVersion = WorldVersion;

            // 2. 读取 NBT 根 Compound
            var rootTag = ReadCompoundTag();

            // 3. 递归提取数据并自动赋值
            ExtractDataRecursive(rootTag);
    
            _rawRoot = rootTag;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析出错: {ex.Message}");
            throw;
        }
    }

    private void ExtractDataRecursive(Dictionary<string, object> dict)
    {
        var type = typeof(LevelWorldData);

        foreach (var kvp in dict)
        {
            // 匹配映射表
            if (TagMap.TagsMap.TryGetValue(kvp.Key, out string propName))
            {
                var prop = type.GetProperty(propName);
                if (prop != null)
                {
                    try
                    {
                        object val = CastValue(kvp.Value, prop.PropertyType);
                        prop.SetValue(WorldData, val);
                    }
                    catch { /* 自动跳过类型严重不匹配的非法标签 */ }
                }
            }
            // 发现嵌套 Compound (如 abilities, experiments)，继续递归
            else if (kvp.Value is Dictionary<string, object> nestedDict)
            {
                ExtractDataRecursive(nestedDict);
            }
        }
    }

    private object CastValue(object value, Type targetType)
    {
        if (value == null) return null;

        // 处理布尔 (NBT Byte -> C# Bool)
        if (targetType == typeof(bool))
        {
            return value switch
            {
                byte b => b != 0,
                int i => i != 0,
                long l => l != 0,
                _ => Convert.ToBoolean(value)
            };
        }

        // 处理数值转换 (兼容基岩版不稳定的数值长度)
        if (targetType == typeof(int)) return Convert.ToInt32(value);
        if (targetType == typeof(long)) return Convert.ToInt64(value);
        if (targetType == typeof(float)) return Convert.ToSingle(value);
        if (targetType == typeof(double)) return Convert.ToDouble(value);
        if (targetType == typeof(string)) return value.ToString();

        return value;
    }

    // --- NBT 底层读取实现 ---

    private Dictionary<string, object> ReadCompoundTag()
    {
        var compound = new Dictionary<string, object>();
        while (true)
        {
            if (_reader.BaseStream.Position >= _reader.BaseStream.Length) break;
            
            byte typeId = _reader.ReadByte();
            if (typeId == 0) break; // TAG_End

            string name = ReadStringWithLength();
            compound[name] = ReadTagValue(typeId);
        }
        return compound;
    }

    private object ReadTagValue(byte typeId)
    {
        return typeId switch
        {
            1 => _reader.ReadByte(),                 // Byte
            2 => _reader.ReadInt16(),                // Short
            3 => _reader.ReadInt32(),                // Int
            4 => _reader.ReadInt64(),                // Long
            5 => _reader.ReadSingle(),               // Float
            6 => _reader.ReadDouble(),               // Double
            7 => _reader.ReadBytes(_reader.ReadInt32()), // Byte Array
            8 => ReadStringWithLength(),             // String
            9 => ReadListTag(),                      // List
            10 => ReadCompoundTag(),                 // Compound
            11 => ReadIntArray(),                    // Int Array
            _ => null
        };
    }

    private List<object> ReadListTag()
    {
        byte typeId = _reader.ReadByte();
        int count = _reader.ReadInt32();
        var list = new List<object>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(ReadTagValue(typeId));
        }
        return list;
    }

    private int[] ReadIntArray()
    {
        int len = _reader.ReadInt32();
        int[] arr = new int[len];
        for (int i = 0; i < len; i++) arr[i] = _reader.ReadInt32();
        return arr;
    }

    private string ReadStringWithLength()
    {
        ushort len = _reader.ReadUInt16();
        if (len == 0) return string.Empty;
        return Encoding.UTF8.GetString(_reader.ReadBytes(len));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _reader?.Close();
            _reader?.Dispose();
            _disposed = true;
        }
    }
}