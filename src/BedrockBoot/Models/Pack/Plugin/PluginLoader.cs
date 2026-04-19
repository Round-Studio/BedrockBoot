using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BedrockBoot.Models.Global;
using BedrockBoot.Plugin;
using Round.SDK.Entry;
using Round.SDK.Plugin.BedrockBoot;

namespace BedrockBoot.Models.Pack.Plugin;

public class PluginLoader
{
    public static readonly List<Assembly> _loadedAssemblies = new();

    public static List<PackConfig> Plugins { get; set; } = new();
    public static Type PluginType { get; } = typeof(IPluginBedrockBoot);

    public static async Task LoadAll()
    {
        Console.WriteLine(@"开始加载插件。");
        if (!Directory.Exists(PathsList.PluginPath))
            Directory.CreateDirectory(PathsList.PluginPath);

        var files = Directory.GetFiles(PathsList.PluginPath);
        Plugins.Clear();

        foreach (var file in files)
        {
            var conf = PluginHelper.ReadPackConfig(file);
            conf.PackFile = file;

            // 逻辑判断
            if (file.EndsWith(".rplck"))
                try
                {
                    LoadDependencies(conf.PackFolder, conf.BodyFile);
                    LoadPluginBody(conf.PackFolder, conf.BodyFile);
                    conf.IsEnable = true; // 正常加载
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"加载插件失败: {file}, 错误: {ex.Message}");
                    conf.IsEnable = false;
                }
            else if (file.EndsWith(".disable"))
                conf.IsEnable = false;
            else
                // 跳过其他无关文件
                continue;

            Plugins.Add(conf);
        }
    }

    public static async Task<bool> Install(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return false;

            var fileName = Path.GetFileName(filePath);
            var targetPath = Path.Combine(PathsList.PluginPath, fileName);

            File.Copy(filePath, targetPath, true);

            var conf = PluginHelper.ReadPackConfig(targetPath);
            conf.PackFile = targetPath;

            var existing = Plugins.FirstOrDefault(p => p.PackName == conf.PackName);
            if (existing != null) Plugins.Remove(existing);

            conf.IsEnable = false;
            Plugins.Add(conf);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"导入插件失败: {ex.Message}");
            return false;
        }
    }

    public static void TogglePlugin(PackConfig config, bool enable)
    {
        if (string.IsNullOrEmpty(config.PackFile)) return;

        var currentPath = config.PackFile;
        string newPath;

        if (enable)
            // 启用：如果以 .disable 结尾，则移除它
            newPath = currentPath.EndsWith(".disable")
                ? currentPath.Substring(0, currentPath.Length - ".disable".Length)
                : currentPath;
        else
            // 禁用：如果没以 .disable 结尾，则加上它
            newPath = currentPath.EndsWith(".disable")
                ? currentPath
                : currentPath + ".disable";

        if (currentPath != newPath)
            try
            {
                if (File.Exists(currentPath))
                {
                    File.Move(currentPath, newPath);
                    config.PackFile = newPath; // 更新路径引用
                    config.IsEnable = enable; // 同步内存状态
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"[Error] 切换插件状态失败: {ex.Message}");
            }
    }

    public static void LoadDependencies(string extractDir, string bodyFile)
    {
        var filesDir = Path.Combine(extractDir, "files");
        if (!Directory.Exists(filesDir)) return;

        var dllFiles = Directory.GetFiles(filesDir, "*.dll")
            .Where(file => !Path.GetFileName(file).Equals(bodyFile, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var loadedNames = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetName().Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var dllPath in dllFiles)
            try
            {
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(dllPath);

                if (loadedNames.Contains(fileNameWithoutExt)) continue;

                var assembly = Assembly.LoadFrom(dllPath);
                _loadedAssemblies.Add(assembly);

                loadedNames.Add(fileNameWithoutExt);

                Console.WriteLine($@"已加载依赖: {Path.GetFileName(dllPath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"加载依赖失败 {dllPath}: {ex.Message}");
            }
    }

    public static void LoadPluginBody(string extractDir, string bodyFile)
    {
        Task.Run(() =>
        {
            var bodyFilePath = Path.Combine(extractDir, "files", bodyFile);
            if (!File.Exists(bodyFilePath)) throw new FileNotFoundException($"插件主体文件不存在: {bodyFilePath}");

            try
            {
                var bodyAssembly = Assembly.LoadFrom(bodyFilePath);
                _loadedAssemblies.Add(bodyAssembly);

                // 查找实现了 IPluginBedrockBoot 的非抽象类
                var pluginType = bodyAssembly.GetTypes()
                    .FirstOrDefault(t => typeof(IPluginBedrockBoot).IsAssignableFrom(t) &&
                                         !t.IsInterface &&
                                         !t.IsAbstract);

                if (pluginType != null)
                {
                    // 1. 创建实例
                    var pluginInstance = Activator.CreateInstance(pluginType);

                    // 2. 强转并执行 Initialize
                    if (pluginInstance is IPluginBedrockBoot bootPlugin)
                        try
                        {
                            bootPlugin.Initialize();

                            Console.WriteLine($@"插件已初始化: {pluginType.FullName}");
                        }
                        catch (Exception loadEx)
                        {
                            Console.WriteLine($@"插件加载错误: {loadEx}");
                        }

                    return pluginInstance;
                }

                throw new InvalidOperationException($"在主体文件中未找到实现 IPluginBedrockBoot 的类: {bodyFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"加载并初始化插件主体失败 {bodyFilePath}: {ex.Message}");
                throw;
            }
        });
    }

    public static bool Delete(PackConfig config)
    {
        try
        {
            if (File.Exists(config.PackFile)) File.Delete(config.PackFile);

            if (Plugins.Contains(config)) Plugins.Remove(config);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"删除插件文件失败: {ex.Message}");
            return false;
        }
    }
}