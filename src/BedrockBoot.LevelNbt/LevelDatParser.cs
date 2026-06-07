using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
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

    // 反射属性缓存：避免每个 tag 触发 GetProperty + SetValue
    private static readonly Dictionary<string, PropertyInfo> _propCache = new(StringComparer.Ordinal);

    // UTF-8 字符串解析复用的解码器，避免每个字符串 tag 都 new 一个
    private static readonly UTF8Encoding _utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    public LevelDatParser(Stream stream)
    {
        _reader = new BinaryReader(stream, _utf8);
        Parse();
    }

    public LevelDatParser(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"文件不存在: {filePath}");

        // 使用 FileShare.ReadWrite 避免游戏运行中文件被锁定无法读取
        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        _reader = new BinaryReader(fileStream, _utf8);
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

            WorldData.HeaderVersion = WorldVersion;

            // 2. 读取 NBT 根 Compound
            var rootTag = ReadCompoundTag();

            // 3. 递归提取数据并自动赋值
            ExtractDataRecursive(rootTag);

            _rawRoot = rootTag;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"解析出错: {ex.Message}");
            throw;
        }
    }

    private static PropertyInfo? GetCachedProperty(string propName)
    {
        if (_propCache.TryGetValue(propName, out var cached)) return cached;
        var prop = typeof(LevelWorldData).GetProperty(propName);
        if (prop != null) _propCache[propName] = prop;
        return prop;
    }

    private void ExtractDataRecursive(Dictionary<string, object> dict)
    {
        foreach (var kvp in dict)
        {
            // 匹配映射表
            if (TagMap.TagsMap.TryGetValue(kvp.Key, out var propName))
            {
                var prop = GetCachedProperty(propName);
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        var val = CastValue(kvp.Value, prop.PropertyType);
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

    private static object? CastValue(object value, Type targetType)
    {
        if (value == null) return null;

        // 优先匹配具体目标类型，避免 Convert.* 走 object 路径产生装箱
        if (targetType == typeof(bool))
        {
            return value switch
            {
                byte b => b != 0,
                int i => i != 0,
                long l => l != 0,
                sbyte sb => sb != 0,
                short s => s != 0,
                float f => f != 0,
                double d => d != 0,
                _ => Convert.ToBoolean(value)
            };
        }

        if (targetType == typeof(int))
        {
            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is short s) return s;
            if (value is byte b) return b;
            if (value is double d) return (int)d;
            if (value is float f) return (int)f;
            return Convert.ToInt32(value);
        }

        if (targetType == typeof(long))
        {
            if (value is long l) return l;
            if (value is int i) return (long)i;
            if (value is short s) return (long)s;
            if (value is byte b) return (long)b;
            return Convert.ToInt64(value);
        }

        if (targetType == typeof(float))
        {
            if (value is float f) return f;
            if (value is double d) return (float)d;
            return Convert.ToSingle(value);
        }

        if (targetType == typeof(double))
        {
            if (value is double d) return d;
            if (value is float f) return (double)f;
            return Convert.ToDouble(value);
        }

        if (targetType == typeof(string))
        {
            return value.ToString();
        }

        return value;
    }

    // --- NBT 底层读取实现 ---

    private Dictionary<string, object> ReadCompoundTag()
    {
        // 预读 N 个键后能更准确估计初始容量，但 NBT 没法在读取前知道大小；
        // 16 是一个常见小 Compound 的合理初始值，能减少 hashtable resize
        var compound = new Dictionary<string, object>(16, StringComparer.Ordinal);
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
        // 预分配容量，避免 List 内部多次扩容
        var list = new List<object>(count);
        for (int i = 0; i < count; i++)
            list.Add(ReadTagValue(typeId));
        return list;
    }

    private int[] ReadIntArray()
    {
        int len = _reader.ReadInt32();
        if (len < 0) return Array.Empty<int>();
        int[] arr = new int[len];
        // 用 Span/Array 批量读取，比逐元素 ReadInt32 减少 virtual call 开销
        var bytes = _reader.ReadBytes(len * 4);
        if (bytes.Length < len * 4) return arr;
        Buffer.BlockCopy(bytes, 0, arr, 0, len * 4);
        return arr;
    }

    private string ReadStringWithLength()
    {
        ushort len = _reader.ReadUInt16();
        if (len == 0) return string.Empty;
        var bytes = _reader.ReadBytes(len);
        if (bytes.Length == 0) return string.Empty;
        // 使用复用的 UTF8Encoding，避免每个 string 都 new 一个 decoder
        return _utf8.GetString(bytes);
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
