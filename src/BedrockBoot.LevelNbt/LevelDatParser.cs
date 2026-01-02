using System.Text;
using BedrockBoot.LevelNbt.Base.Entry;

namespace BedrockBoot.LevelNbt;

public class LevelDatParser
{
    /// <summary>
    /// 用于解析基岩版Minecraft level.dat文件的类（修正数据结构版本）
    /// </summary>
    public class LevelDatReader : IDisposable
    {
        private BinaryReader _reader;
        private bool _disposed;
        
        public int WorldVersion { get; private set; }
        public int NBTDataSize { get; private set; }
        public LevelWorldData WorldData { get; private set; }

        public LevelDatReader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            _reader = new BinaryReader(stream, Encoding.UTF8);
            ParseFileHeader();
            ParseNBTData();
        }

        public LevelDatReader(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"文件不存在: {filePath}");

            var fileStream = File.OpenRead(filePath);
            _reader = new BinaryReader(fileStream, Encoding.UTF8);
            ParseFileHeader();
            ParseNBTData();
        }

        private void ParseFileHeader()
        {
            try
            {
                if (_reader.BaseStream.CanSeek)
                    _reader.BaseStream.Position = 0;
                
                WorldVersion = ReadInt32LittleEndian();
                NBTDataSize = ReadInt32LittleEndian();
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException("文件太小，无法读取完整文件头", ex);
            }
        }

        /// <summary>
        /// 修正：正确解析NBT数据结构
        /// </summary>
        private void ParseNBTData()
        {
            try
            {
                // 读取根CompoundTag
                var rootTag = ReadCompoundTag();
                
                // 关键修正：根据调试输出，世界数据存储在根标签内部的一个嵌套结构中
                WorldData = ExtractWorldDataFromRoot(rootTag);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析NBT时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 修正：从根标签中正确提取世界数据
        /// 根据你的调试输出，LevelName等标签直接位于根标签中
        /// </summary>
        private LevelWorldData ExtractWorldDataFromRoot(Dictionary<string, object> rootTag)
        {
            var worldData = new LevelWorldData();
            
            Console.WriteLine($"调试: 根标签包含 {rootTag.Count} 个子标签");
            Console.WriteLine($"调试: 根标签的键: {string.Join(", ", rootTag.Keys.Take(10))}...");
            
            // 直接尝试从根标签读取
            ExtractWorldDataFromDictionary(rootTag, worldData);
            
            // 如果没有找到关键数据，尝试在嵌套结构中查找
            if (string.IsNullOrEmpty(worldData.LevelName))
            {
                Console.WriteLine("调试: 尝试在嵌套结构中查找世界数据...");
                
                // 查找可能的嵌套CompoundTag
                foreach (var kvp in rootTag)
                {
                    if (kvp.Value is Dictionary<string, object> nestedDict)
                    {
                        Console.WriteLine($"调试: 检查嵌套标签 '{kvp.Key}'");
                        ExtractWorldDataFromDictionary(nestedDict, worldData);
                        
                        // 如果找到关键数据就停止
                        if (!string.IsNullOrEmpty(worldData.LevelName))
                            break;
                    }
                }
            }
            
            return worldData;
        }

        /// <summary>
        /// 从字典中提取世界数据
        /// </summary>
        private void ExtractWorldDataFromDictionary(Dictionary<string, object> dict, LevelWorldData worldData)
        {
            // 根据文章提到的标签提取关键信息
            if (dict.TryGetValue("LevelName", out object levelNameObj))
            {
                if (levelNameObj is string levelName)
                {
                    worldData.LevelName = levelName;
                    Console.WriteLine($"调试: 找到 LevelName = {levelName}");
                }
            }
            
            if (dict.TryGetValue("RandomSeed", out object seedObj))
            {
                if (seedObj is long seedLong)
                {
                    worldData.RandomSeed = seedLong;
                    Console.WriteLine($"调试: 找到 RandomSeed = {seedLong}");
                }
                else if (seedObj is int seedInt)
                {
                    worldData.RandomSeed = seedInt;
                    Console.WriteLine($"调试: 找到 RandomSeed = {seedInt}");
                }
                else if (seedObj is byte seedByte)
                {
                    worldData.RandomSeed = seedByte;
                }
            }
            
            if (dict.TryGetValue("GameType", out object gameTypeObj))
            {
                if (gameTypeObj is int gameTypeInt)
                {
                    worldData.GameType = gameTypeInt;
                    Console.WriteLine($"调试: 找到 GameType = {gameTypeInt}");
                }
                else if (gameTypeObj is byte gameTypeByte)
                {
                    worldData.GameType = gameTypeByte;
                }
            }
            
            if (dict.TryGetValue("cheatsEnabled", out object cheatsEnabledObj))
            {
                if (cheatsEnabledObj is byte cheatsByte)
                {
                    worldData.CheatsEnabled = cheatsByte != 0;
                    Console.WriteLine($"调试: 找到 cheatsEnabled = {cheatsByte != 0}");
                }
            }
            
            if (dict.TryGetValue("commandsEnabled", out object commandsEnabledObj))
            {
                if (commandsEnabledObj is byte commandsByte)
                {
                    worldData.CommandsEnabled = commandsByte != 0;
                    Console.WriteLine($"调试: 找到 commandsEnabled = {commandsByte != 0}");
                }
            }
            
            if (dict.TryGetValue("isHardcore", out object isHardcoreObj))
            {
                if (isHardcoreObj is byte hardcoreByte)
                {
                    worldData.IsHardCore = hardcoreByte != 0;
                    Console.WriteLine($"调试: 找到 isHardcore = {hardcoreByte != 0}");
                }
            }
            // 也尝试用驼峰命名的版本
            else if (dict.TryGetValue("IsHardcore", out object IsHardcoreObj))
            {
                if (IsHardcoreObj is byte hardcoreByte)
                {
                    worldData.IsHardCore = hardcoreByte != 0;
                    Console.WriteLine($"调试: 找到 IsHardcore = {hardcoreByte != 0}");
                }
            }
            
            // FlatWorldLayers (超平坦预设)
            if (dict.TryGetValue("FlatWorldLayers", out object flatWorldLayersObj) && flatWorldLayersObj is string flatWorldLayers)
            {
                worldData.FlatWorldLayers = flatWorldLayers;
                Console.WriteLine($"调试: 找到 FlatWorldLayers (长度={flatWorldLayers.Length})");
            }
            
            // 提取更多有用的标签
            if (dict.TryGetValue("SpawnX", out object spawnXObj))
            {
                if (spawnXObj is int spawnX)
                {
                    worldData.SpawnX = spawnX;
                    Console.WriteLine($"调试: 找到 SpawnX = {spawnX}");
                }
            }
            
            if (dict.TryGetValue("SpawnY", out object spawnYObj))
            {
                if (spawnYObj is int spawnY)
                {
                    worldData.SpawnY = spawnY;
                    Console.WriteLine($"调试: 找到 SpawnY = {spawnY}");
                }
            }
            
            if (dict.TryGetValue("SpawnZ", out object spawnZObj))
            {
                if (spawnZObj is int spawnZ)
                {
                    worldData.SpawnZ = spawnZ;
                    Console.WriteLine($"调试: 找到 SpawnZ = {spawnZ}");
                }
            }
            
            if (dict.TryGetValue("Time", out object timeObj))
            {
                if (timeObj is long timeLong)
                {
                    worldData.Time = timeLong;
                    Console.WriteLine($"调试: 找到 Time = {timeLong}");
                }
            }
            
            if (dict.TryGetValue("LastPlayed", out object lastPlayedObj))
            {
                if (lastPlayedObj is long lastPlayedLong)
                {
                    worldData.LastPlayed = lastPlayedLong;
                    Console.WriteLine($"调试: 找到 LastPlayed = {lastPlayedLong}");
                }
            }
        }

        /// <summary>
        /// 读取CompoundTag类型 - 保持原有实现
        /// </summary>
        private Dictionary<string, object> ReadCompoundTag()
        {
            var compound = new Dictionary<string, object>();
            
            while (true)
            {
                if (_reader.BaseStream.Position >= _reader.BaseStream.Length)
                    break;
                
                byte tagType = _reader.ReadByte();
                
                if (tagType == 0x00)
                    break;
                
                ushort nameLength = ReadUInt16LittleEndian();
                byte[] nameBytes = _reader.ReadBytes(nameLength);
                if (nameBytes.Length < nameLength)
                    throw new EndOfStreamException("无法读取完整的标签名");
                    
                string tagName = Encoding.UTF8.GetString(nameBytes);
                
                object tagValue = ReadTagValue(tagType, tagName);
                
                compound[tagName] = tagValue;
            }
            
            return compound;
        }

        private object ReadTagValue(byte tagType, string tagName = "")
        {
            return tagType switch
            {
                0x01 => _reader.ReadByte(),      // ByteTag
                0x02 => ReadInt16LittleEndian(), // ShortTag
                0x03 => ReadInt32LittleEndian(), // IntTag
                0x04 => ReadInt64LittleEndian(), // LongTag
                0x05 => ReadSingleLittleEndian(),// FloatTag
                0x06 => ReadDoubleLittleEndian(),// DoubleTag
                0x07 => ReadByteArrayTag(),      // ByteArrayTag
                0x08 => ReadStringTag(),         // StringTag
                0x09 => ReadListTag(),           // ListTag
                0x0A => ReadCompoundTag(),       // CompoundTag
                0x0B => ReadIntArrayTag(),       // IntArrayTag
                0x0C => ReadLongArrayTag(),      // LongArrayTag
                _ => throw new InvalidDataException($"未知的NBT标签类型: 0x{tagType:X2} (标签: {tagName})")
            };
        }

        // 以下是各种数据类型的读取方法（保持原有实现）
        private int ReadInt32LittleEndian()
        {
            byte[] bytes = _reader.ReadBytes(4);
            if (bytes.Length < 4)
                throw new EndOfStreamException("无法读取4字节整数");
            return bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24);
        }

        private short ReadInt16LittleEndian()
        {
            byte[] bytes = _reader.ReadBytes(2);
            if (bytes.Length < 2)
                throw new EndOfStreamException("无法读取2字节整数");
            return (short)(bytes[0] | (bytes[1] << 8));
        }

        private ushort ReadUInt16LittleEndian()
        {
            byte[] bytes = _reader.ReadBytes(2);
            if (bytes.Length < 2)
                throw new EndOfStreamException("无法读取2字节无符号整数");
            return (ushort)(bytes[0] | (bytes[1] << 8));
        }

        private long ReadInt64LittleEndian()
        {
            byte[] bytes = _reader.ReadBytes(8);
            if (bytes.Length < 8)
                throw new EndOfStreamException("无法读取8字节整数");
            long result = 0;
            for (int i = 0; i < 8; i++)
                result |= ((long)bytes[i] << (8 * i));
            return result;
        }

        private float ReadSingleLittleEndian()
        {
            byte[] bytes = _reader.ReadBytes(4);
            if (bytes.Length < 4)
                throw new EndOfStreamException("无法读取4字节浮点数");
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        private double ReadDoubleLittleEndian()
        {
            byte[] bytes = _reader.ReadBytes(8);
            if (bytes.Length < 8)
                throw new EndOfStreamException("无法读取8字节双精度浮点数");
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }

        private string ReadStringTag()
        {
            ushort length = ReadUInt16LittleEndian();
            if (length == 0)
                return string.Empty;
            byte[] bytes = _reader.ReadBytes(length);
            if (bytes.Length < length)
                throw new EndOfStreamException("无法读取完整的字符串");
            return Encoding.UTF8.GetString(bytes);
        }

        private List<object> ReadListTag()
        {
            byte listTagType = _reader.ReadByte();
            int listLength = ReadInt32LittleEndian();
            
            var list = new List<object>(listLength);
            
            if (listLength == 0)
                return list;
            
            for (int i = 0; i < listLength; i++)
                list.Add(ReadTagValue(listTagType, $"List[{i}]"));
            
            return list;
        }

        private byte[] ReadByteArrayTag()
        {
            int length = ReadInt32LittleEndian();
            if (length == 0)
                return Array.Empty<byte>();
            byte[] array = _reader.ReadBytes(length);
            if (array.Length < length)
                throw new EndOfStreamException("无法读取完整的字节数组");
            return array;
        }

        private int[] ReadIntArrayTag()
        {
            int length = ReadInt32LittleEndian();
            if (length == 0)
                return Array.Empty<int>();
            int[] array = new int[length];
            for (int i = 0; i < length; i++)
                array[i] = ReadInt32LittleEndian();
            return array;
        }

        private long[] ReadLongArrayTag()
        {
            int length = ReadInt32LittleEndian();
            if (length == 0)
                return Array.Empty<long>();
            long[] array = new long[length];
            for (int i = 0; i < length; i++)
                array[i] = ReadInt64LittleEndian();
            return array;
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
}