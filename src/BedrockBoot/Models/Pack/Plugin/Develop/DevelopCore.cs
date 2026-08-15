using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using Round.SDK.Entity;
using Round.SDK.Entry;

namespace BedrockBoot.Models.Pack.Plugin.Develop;

public static class DevelopCore
{
    public static Action<string, string>? OnPluginBuilt { get; set; }

    public static void CreatePluginProject(PackConfig conf, Action<int, string>? progressCallback = null)
    {
        var projectDirectory = conf.PackFolder;
        var basePath = Path.GetDirectoryName(projectDirectory);
        var projectName = conf.PackName;

        if (string.IsNullOrWhiteSpace(basePath))
            throw new ArgumentException("Base path cannot be null or empty.", nameof(basePath));

        progressCallback?.Invoke(0, "正在初始化项目配置...");
        Console.WriteLine($@"[DevelopCore] Creating plugin project at: {projectDirectory}");
        Console.WriteLine($@"[DevelopCore] Project name: {projectName}");

        var confent = new ConfigEntity<PackConfig>(Path.Combine(projectDirectory, "plugin.json"));
        confent.Data = conf;
        confent.Data.BodyFile = $"{projectName}.dll";
        confent.Save();

        progressCallback?.Invoke(15, "正在创建 .NET 类库...");
        CreateDotNetClassLibrary(basePath, projectName,
            (p, msg) => progressCallback?.Invoke(15 + (int)(p * 0.25), msg));

        progressCallback?.Invoke(40, "正在初始化 Git 仓库...");
        InitializeGitRepository(basePath, (p, msg) => progressCallback?.Invoke(40 + (int)(p * 0.15), msg));

        progressCallback?.Invoke(55, "正在添加 Git 子模块...");
        AddGitSubmodule(basePath, (p, msg) => progressCallback?.Invoke(55 + (int)(p * 0.25), msg));

        progressCallback?.Invoke(80, "正在生成解决方案文件...");
        CreateSolutionFile(basePath, projectName);

        progressCallback?.Invoke(85, "正在添加项目引用...");
        AddProjectReferences(projectDirectory, projectName);

        progressCallback?.Invoke(90, "正在提交 Git 初始变更...");
        GitCommit(basePath, projectName, (p, msg) => progressCallback?.Invoke(90 + (int)(p * 0.10), msg));

        progressCallback?.Invoke(100, "插件项目创建完成！");
        Console.WriteLine($@"[DevelopCore] Plugin project created successfully!");

        DevelopProjectManager.AddProject(Path.Combine(projectDirectory), conf);
    }

    public static void DebugPluginProject(string projectDirectory)
    {
        var basePath = Path.GetDirectoryName(projectDirectory);
        var projectName = Path.GetFileName(projectDirectory);

        Console.WriteLine($@"[DebugPluginProject] Building plugin project: {projectDirectory}");
        Console.WriteLine($@"[DebugPluginProject] Project name: {projectName}");

        string outputDirectory = Path.Combine(basePath, "output", "debug");
        Console.WriteLine($@"[DebugPluginProject] Output directory: {outputDirectory}");

        BuildProject(projectDirectory, outputDirectory);

        string dllPath = Path.Combine(outputDirectory, $"{projectName}.dll");
        Console.WriteLine($@"[DebugPluginProject] Looking for DLL at: {dllPath}");

        if (File.Exists(dllPath))
        {
            Console.WriteLine($@"[DebugPluginProject] DLL found: {dllPath}");
            OnPluginBuilt?.Invoke(outputDirectory, $"{projectName}.dll");
        }
        else
        {
            Console.WriteLine($@"[DebugPluginProject] DLL not found at: {dllPath}");
            throw new FileNotFoundException($"Could not find built DLL at: {dllPath}");
        }
    }

    #region 静态私有方法

    private static void RunProcessWithProgress(
        ProcessStartInfo startInfo,
        Action<int, string>? progressCallback,
        Func<string, int?>? parseProgressFunc = null)
    {
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        using Process process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            Console.WriteLine($@"[Process Output] {e.Data}");
            int percentage = parseProgressFunc?.Invoke(e.Data) ?? 50;
            progressCallback?.Invoke(percentage, e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            Console.WriteLine($@"[Process Error] {e.Data}");
            int percentage = parseProgressFunc?.Invoke(e.Data) ?? 50;
            progressCallback?.Invoke(percentage, e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Process '{startInfo.FileName}' failed with exit code {process.ExitCode}");
        }
    }

    private static int? ParseGitProgress(string line)
    {
        if (line.Contains("Receiving objects:") || line.Contains("Resolving deltas:"))
        {
            var parts = line.Split('%');
            if (parts.Length > 1)
            {
                var numStr = new string(parts[0].Where(char.IsDigit).ToArray());
                if (int.TryParse(numStr, out int result))
                {
                    return result;
                }
            }
        }

        return null;
    }

    private static void CreateDotNetClassLibrary(string basePath, string projectName,
        Action<int, string>? progressCallback)
    {
        string projectDirectory = Path.Combine(basePath, projectName);

        if (!Directory.Exists(projectDirectory))
        {
            Directory.CreateDirectory(projectDirectory);
        }

        string csprojPath = Path.Combine(projectDirectory, $"{projectName}.csproj");
        if (File.Exists(csprojPath))
            throw new InvalidOperationException($"Project file already exists: {csprojPath}");

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"new classlib -n \"{projectName}\" -f net10.0 --no-restore -o \"{projectName}\"",
            WorkingDirectory = basePath
        };

        RunProcessWithProgress(startInfo, progressCallback);
    }

    private static void InitializeGitRepository(string basePath, Action<int, string>? progressCallback)
    {
        string gitPath = Path.Combine(basePath, ".git");
        if (Directory.Exists(gitPath))
        {
            progressCallback?.Invoke(100, "Git 仓库已存在，跳过初始化。");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "init",
            WorkingDirectory = basePath
        };

        RunProcessWithProgress(startInfo, progressCallback);
    }

    private static void AddGitSubmodule(string basePath, Action<int, string>? progressCallback)
    {
        string modulesDir = Path.Combine(basePath, "modules");
        string submodulePath = Path.Combine(modulesDir, "Round.SDK");

        if (Directory.Exists(submodulePath))
        {
            progressCallback?.Invoke(100, "Git 子模块已存在，跳过添加。");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"submodule add --progress https://github.com/Round-Studio/Round.SDK modules/Round.SDK",
            WorkingDirectory = basePath
        };

        RunProcessWithProgress(startInfo, progressCallback, ParseGitProgress);
    }

    private static void CreateSolutionFile(string basePath, string projectName)
    {
        string slnPath = Path.Combine(basePath, $"{projectName}.slnx");

        if (File.Exists(slnPath)) return;

        string solutionContent = $@"<Solution>
  <Configurations>
    <Platform Name=""Any CPU"" />
    <Platform Name=""x86"" />
  </Configurations>
  <Folder Name=""/Dependence/"">
    <Project Path=""modules/Round.SDK/Round.SDK/Round.SDK.csproj"" />
  </Folder>
  <Folder Name=""/Plugin/"">
    <Project Path=""{projectName}/{projectName}.csproj"" />
  </Folder>
</Solution>";

        File.WriteAllText(slnPath, solutionContent);
    }

    private static void AddProjectReferences(string projectDirectory, string projectName)
    {
        string csprojPath = Path.Combine(projectDirectory, $"{projectName}.csproj");
        if (!File.Exists(csprojPath)) return;

        XDocument doc = XDocument.Load(csprojPath);
        XNamespace ns = doc.Root.GetDefaultNamespace();

        XElement itemGroup = doc.Root.Elements(ns + "ItemGroup")
            .FirstOrDefault(x => x.Elements(ns + "ProjectReference").Any());

        if (itemGroup == null)
        {
            itemGroup = new XElement(ns + "ItemGroup");
            doc.Root.Add(itemGroup);
        }

        string[] references = new[]
        {
            @"..\modules\Round.SDK\Round.SDK\Round.SDK.csproj"
        };

        int addedCount = 0;
        foreach (string refPath in references)
        {
            string fullRefPath = Path.Combine(projectDirectory, refPath);
            if (File.Exists(fullRefPath))
            {
                XElement projectRef = new XElement(ns + "ProjectReference",
                    new XAttribute("Include", refPath));

                if (!itemGroup.Elements(ns + "ProjectReference")
                        .Any(x => (string)x.Attribute("Include") == refPath))
                {
                    itemGroup.Add(projectRef);
                    addedCount++;
                }
            }
        }

        if (addedCount > 0)
        {
            doc.Save(csprojPath);
        }
    }

    private static void GitCommit(string basePath, string projectName, Action<int, string>? progressCallback)
    {
        ProcessStartInfo addStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "add .",
            WorkingDirectory = basePath
        };
        RunProcessWithProgress(addStartInfo, (p, msg) => progressCallback?.Invoke((int)(p * 0.5), msg));

        ProcessStartInfo commitStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"commit -m \"Initial commit for {projectName}\"",
            WorkingDirectory = basePath
        };
        RunProcessWithProgress(commitStartInfo, (p, msg) => progressCallback?.Invoke(50 + (int)(p * 0.5), msg));
    }

    private static void BuildProject(string projectDirectory, string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build -c Debug -o \"{outputDirectory}\"",
            WorkingDirectory = projectDirectory
        };

        RunProcessWithProgress(startInfo, null);
    }

    #endregion
}