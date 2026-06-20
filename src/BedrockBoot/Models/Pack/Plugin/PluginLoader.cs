using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Models.Global;
using BedrockBoot.Plugin;
using Round.SDK.Entry;
using Round.SDK.Plugin.BedrockBoot;

namespace BedrockBoot.Models.Pack.Plugin;

public class PluginLoader
{
    // 使用线程安全的集合
    public static readonly ConcurrentBag<Assembly> _loadedAssemblies = new();
    
    // 用于同步的锁对象
    private static readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    private static readonly object _pluginListLock = new();
    private static readonly ReaderWriterLockSlim _assemblyLock = new();
    
    // 已加载的程序集名称缓存（线程安全）
    private static readonly ConcurrentDictionary<string, byte> _loadedAssemblyNames = new(StringComparer.OrdinalIgnoreCase);
    
    public static List<PackConfig> Plugins { get; set; } = new();
    public static Type PluginType { get; } = typeof(IPluginBedrockBoot);

    public static async Task LoadAll()
    {
        Console.WriteLine(@"开始加载插件。");
        if (!Directory.Exists(PathsList.PluginPath))
            Directory.CreateDirectory(PathsList.PluginPath);

        var files = Directory.GetFiles(PathsList.PluginPath);
        
        // 清空现有插件列表（线程安全）
        lock (_pluginListLock)
        {
            Plugins.Clear();
        }

        // 并发加载任务列表
        var loadTasks = new List<Task>();
        
        foreach (var file in files)
        {
            // 跳过非插件文件
            if (!file.EndsWith(".rplck") && !file.EndsWith(".disable"))
                continue;
                
            loadTasks.Add(LoadPluginAsync(file));
        }
        
        // 等待所有插件加载完成
        await Task.WhenAll(loadTasks);
        
        Console.WriteLine($@"插件加载完成，共加载 {Plugins.Count} 个插件。");
    }

    private static async Task LoadPluginAsync(string file)
    {
        try
        {
            var conf = PluginHelper.ReadPackConfig(file);
            conf.PackFile = file;

            if (file.EndsWith(".rplck"))
            {
                // 使用信号量确保依赖加载的顺序性
                await LoadDependenciesAsync(conf.PackFolder, conf.BodyFile);
                
                // 加载插件主体
                await LoadPluginBodyAsync(conf.PackFolder, conf.BodyFile);
                
                conf.IsEnable = true;
            }
            else if (file.EndsWith(".disable"))
            {
                conf.IsEnable = false;
            }

            // 线程安全地添加到插件列表
            lock (_pluginListLock)
            {
                Plugins.Add(conf);
            }
            
            Console.WriteLine($@"插件加载成功: {conf.PackName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"加载插件失败: {file}, 错误: {ex.Message}");
            
            // 即使失败也添加到列表，标记为已禁用
            var conf = PluginHelper.ReadPackConfig(file);
            conf.PackFile = file;
            conf.IsEnable = false;
            
            lock (_pluginListLock)
            {
                Plugins.Add(conf);
            }
        }
    }

    public static async Task<bool> Install(string filePath)
    {
        await _loadSemaphore.WaitAsync();
        try
        {
            if (!File.Exists(filePath)) return false;

            var fileName = Path.GetFileName(filePath);
            var targetPath = Path.Combine(PathsList.PluginPath, fileName);

            File.Copy(filePath, targetPath, true);

            var conf = PluginHelper.ReadPackConfig(targetPath);
            conf.PackFile = targetPath;

            lock (_pluginListLock)
            {
                var existing = Plugins.FirstOrDefault(p => p.PackName == conf.PackName);
                if (existing != null) Plugins.Remove(existing);

                conf.IsEnable = true;
                Plugins.Add(conf);
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"导入插件失败: {ex.Message}");
            return false;
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }

    public static void TogglePlugin(PackConfig config, bool enable)
    {
        lock (_pluginListLock)
        {
            if (string.IsNullOrEmpty(config.PackFile)) return;

            var currentPath = config.PackFile;
            string newPath;

            if (enable)
                newPath = currentPath.EndsWith(".disable")
                    ? currentPath.Substring(0, currentPath.Length - ".disable".Length)
                    : currentPath;
            else
                newPath = currentPath.EndsWith(".disable")
                    ? currentPath
                    : currentPath + ".disable";

            if (currentPath != newPath)
                try
                {
                    if (File.Exists(currentPath))
                    {
                        File.Move(currentPath, newPath);
                        config.PackFile = newPath;
                        config.IsEnable = enable;
                        
                        GlobalModel.MainWindow.SetReboot();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"切换插件状态失败: {ex.Message}");
                }
        }
    }

    public static async Task LoadDependenciesAsync(string extractDir, string bodyFile)
    {
        var filesDir = Path.Combine(extractDir, "files");
        if (!Directory.Exists(filesDir)) return;

        var dllFiles = Directory.GetFiles(filesDir, "*.dll")
            .Where(file => !Path.GetFileName(file).Equals(bodyFile, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var loadTasks = new List<Task>();
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount); // 限制并发加载数量

        foreach (var dllPath in dllFiles)
        {
            await semaphore.WaitAsync();
            
            loadTasks.Add(Task.Run(async () =>
            {
                try
                {
                    await LoadAssemblyAsync(dllPath);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(loadTasks);
    }

    private static async Task LoadAssemblyAsync(string dllPath)
    {
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(dllPath);

        // 检查是否已加载（线程安全）
        if (_loadedAssemblyNames.ContainsKey(fileNameWithoutExt))
            return;

        // 使用读写锁保护程序集加载
        _assemblyLock.EnterWriteLock();
        try
        {
            // 双重检查
            if (_loadedAssemblyNames.ContainsKey(fileNameWithoutExt))
                return;

            var assembly = Assembly.LoadFrom(dllPath);
            _loadedAssemblies.Add(assembly);
            _loadedAssemblyNames.TryAdd(fileNameWithoutExt, 1);

            Console.WriteLine($@"已加载依赖: {Path.GetFileName(dllPath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"加载依赖失败 {dllPath}: {ex.Message}");
        }
        finally
        {
            _assemblyLock.ExitWriteLock();
        }
    }

    public static async Task<object> LoadPluginBodyAsync(string extractDir, string bodyFile)
    {
        return await Task.Run(() =>
        {
            var bodyFilePath = Path.Combine(extractDir, "files", bodyFile);
            if (!File.Exists(bodyFilePath)) 
                throw new FileNotFoundException($"插件主体文件不存在: {bodyFilePath}");

            try
            {
                var bodyAssembly = Assembly.LoadFrom(bodyFilePath);
                _loadedAssemblies.Add(bodyAssembly);
                _loadedAssemblyNames.TryAdd(Path.GetFileNameWithoutExtension(bodyFilePath), 1);

                // 查找实现了 IPluginBedrockBoot 的非抽象类
                var pluginType = bodyAssembly.GetTypes()
                    .FirstOrDefault(t => typeof(IPluginBedrockBoot).IsAssignableFrom(t) &&
                                         !t.IsInterface &&
                                         !t.IsAbstract);

                if (pluginType != null)
                {
                    var pluginInstance = Activator.CreateInstance(pluginType);

                    if (pluginInstance is IPluginBedrockBoot bootPlugin)
                        try
                        {
                            bootPlugin.Initialize();
                            Console.WriteLine($@"插件已初始化: {pluginType.FullName}");
                            return pluginInstance;
                        }
                        catch (Exception loadEx)
                        {
                            Console.WriteLine($@"插件初始化错误: {loadEx}");
                            throw;
                        }
                }

                throw new InvalidOperationException($"在主体文件中未找到实现 IPluginBedrockBoot 的类: {bodyFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"加载并初始化插件主体失败 {bodyFilePath}: {ex}");
                throw;
            }
        });
    }

    public static bool Delete(PackConfig config)
    {
        lock (_pluginListLock)
        {
            try
            {
                if (File.Exists(config.PackFile)) 
                    File.Delete(config.PackFile);

                if (Plugins.Contains(config)) 
                    Plugins.Remove(config);
                    
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"删除插件文件失败: {ex.Message}");
                return false;
            }
        }
    }
    
    public static string? FindInstalledPackageFile(string originalFileName)
    {
        lock (_pluginListLock)
        {
            var pluginPath = PathsList.PluginPath;
            var searchPattern = $"*({originalFileName}).rplck";
        
            // 查找匹配模式的文件
            var matchedFiles = Directory.GetFiles(pluginPath, searchPattern);
        
            if (matchedFiles.Length > 0)
            {
                return matchedFiles[0]; // 返回第一个匹配的完整路径
            }
        
            // 也检查 .disable 的
            var disabledPattern = $"*({originalFileName}).rplck.disable";
            var disabledFiles = Directory.GetFiles(pluginPath, disabledPattern);
        
            return disabledFiles.Length > 0 ? disabledFiles[0] : null;
        }
    }
    
    // 清理资源
    public static void Dispose()
    {
        _loadSemaphore?.Dispose();
        _assemblyLock?.Dispose();
    }
}