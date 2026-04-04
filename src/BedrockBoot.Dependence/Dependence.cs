using System.Reflection;

namespace BedrockBoot.Dependence;

public class Dependence
{
    /// <summary>
    /// 从当前程序集获取嵌入式资源
    /// </summary>
    /// <param name="fileName">资源名称</param>
    /// <returns>资源字节数组</returns>
    public static byte[] GetResource(string fileName)
    {
        return GetResource(Assembly.GetExecutingAssembly(), fileName);
    }

    /// <summary>
    /// 从指定程序集获取嵌入式资源
    /// </summary>
    /// <param name="assembly">要从中加载资源的程序集</param>
    /// <param name="fileName">资源名称</param>
    /// <returns>资源字节数组</returns>
    public static byte[] GetResource(Assembly assembly, string fileName)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));
        
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为空", nameof(fileName));

        var resources = assembly.GetManifestResourceNames();
        Console.WriteLine($@"Available embedded resources in {assembly.FullName}:");
        foreach (var res in resources)
        {
            Console.WriteLine($@"  - {res}");
        }

        Console.WriteLine($@"Looking for: {fileName}");

        using (var stream = assembly.GetManifestResourceStream(fileName))
        {
            if (stream == null)
            {
                throw new InvalidOperationException(
                    $"Resource '{fileName}' not found in assembly '{assembly.FullName}'. Available resources: {string.Join(", ", resources)}");
            }

            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }

    /// <summary>
    /// 尝试从多个程序集中查找并获取资源
    /// </summary>
    /// <param name="assemblies">要搜索的程序集列表</param>
    /// <param name="fileName">资源名称</param>
    /// <returns>资源字节数组</returns>
    public static byte[] GetResourceFromAssemblies(IEnumerable<Assembly> assemblies, string fileName)
    {
        if (assemblies == null)
            throw new ArgumentNullException(nameof(assemblies));

        foreach (var assembly in assemblies)
        {
            try
            {
                var resources = assembly.GetManifestResourceNames();
                if (resources.Contains(fileName))
                {
                    return GetResource(assembly, fileName);
                }
            }
            catch
            {
                // 继续尝试下一个程序集
                continue;
            }
        }

        throw new InvalidOperationException($"Resource '{fileName}' not found in any of the provided assemblies.");
    }
}