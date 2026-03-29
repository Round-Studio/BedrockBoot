using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BedrockBoot.LevelNbt.Base.Entry;
using Round.SDK.Helper;

namespace BedrockBoot.Base.Entry.Game.Pack.Archive;

public class ArchiveInfo
{
    private string _uuid = string.Empty;
    
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("path")] public string Path { get; set; }
    [JsonPropertyName("iconPath")] public string IconPath { get; set; }
    [JsonPropertyName("isProject")] public bool IsProject { get; set; } = false;
    [JsonPropertyName("levelWorldData")] public LevelWorldData LevelWorldData { get; set; }
    [JsonIgnore] public VersionConfig VersionInfo { get; set; }
    
    [JsonPropertyName("uuid")]
    public string Uuid 
    { 
        get
        {
            // 如果内存中没有 UUID，尝试从文件获取或新建
            if (string.IsNullOrEmpty(_uuid))
            {
                string bbDir = System.IO.Path.Combine(Path ?? "", ".bb");
                string uuidFilePath = System.IO.Path.Combine(bbDir, "uuid");

                // 1. 首选：从文件读取
                if (!string.IsNullOrEmpty(Path) && File.Exists(uuidFilePath))
                {
                    try
                    {
                        string content = File.ReadAllText(uuidFilePath).Trim();
                        if (!string.IsNullOrEmpty(content))
                        {
                            _uuid = content;
                            return _uuid;
                        }
                    }
                    catch { /* 读取失败则进入下一步生成逻辑 */ }
                }

                // 2. 备选：生成新 UUID 并立即保存
                _uuid = Guid.NewGuid().ToString();
                
                if (!string.IsNullOrEmpty(Path))
                {
                    try
                    {
                        if (!Directory.Exists(bbDir)) Directory.CreateDirectory(bbDir);
                        File.WriteAllText(uuidFilePath, _uuid);
                    }
                    catch (Exception ex) 
                    { 
                        Console.WriteLine($"自动保存新UUID失败: {ex.Message}"); 
                    }
                }
            }
            return _uuid;
        }
        set
        {
            if (_uuid != value)
            {
                _uuid = value;
                // 当外部赋值（如反序列化或手动设置）时，同步更新本地文件
                if (!string.IsNullOrEmpty(Path) && !string.IsNullOrEmpty(_uuid))
                {
                    try
                    {
                        string bbDir = System.IO.Path.Combine(Path, ".bb");
                        if (!Directory.Exists(bbDir)) Directory.CreateDirectory(bbDir);
                        File.WriteAllText(System.IO.Path.Combine(bbDir, "uuid"), _uuid);
                    }
                    catch { }
                }
            }
        }
    }

    /// <summary>
    /// 保存存档
    /// </summary>
    public void Save(string saveFile)
    {
        // 访问 Uuid 属性会自动触发“文件读取/自动生成”逻辑
        if (string.IsNullOrEmpty(Uuid)) 
            throw new Exception("UUID 状态异常，无法保存");
            
        ZipHelper.CreateZipFile(Path, saveFile);
    }
    
    /// <summary>
    /// 反序列化后调用，确保 UUID 状态正确
    /// </summary>
    public void OnDeserialized()
    {
        // 仅仅触发一次 get 访问器即可完成所有逻辑
        _ = Uuid;
    }
}