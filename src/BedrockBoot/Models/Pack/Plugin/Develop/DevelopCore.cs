using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

namespace BedrockBoot.Models.Pack.Plugin.Develop;

public static class DevelopCore
{
    public static Action<string, string>? OnPluginBuilt { get; set; }

    public static void CreatePluginProject(string projectDirectory)
    {
        var basePath = Path.GetDirectoryName(projectDirectory);
        var projectName = Path.GetFileName(projectDirectory);
        
        Console.WriteLine($@"[DevelopCore] Creating plugin project at: {projectDirectory}");
        Console.WriteLine($@"[DevelopCore] Project name: {projectName}");
        
        CreateDotNetClassLibrary(basePath, projectName);
        InitializeGitRepository(basePath);
        AddGitSubmodule(basePath);
        CreateSolutionFile(basePath, projectName);
        AddProjectReferences(projectDirectory, projectName);
        GitCommit(basePath, projectName);
        
        Console.WriteLine($@"[DevelopCore] Plugin project created successfully!");
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

    private static void CreateDotNetClassLibrary(string basePath, string projectName)
    {
        Console.WriteLine($@"[CreateDotNetClassLibrary] Creating class library...");
        Console.WriteLine($@"[CreateDotNetClassLibrary] Base path: {basePath}");
        
        if (string.IsNullOrWhiteSpace(basePath))
            throw new ArgumentException("Base path cannot be null or empty.", nameof(basePath));

        string projectDirectory = Path.Combine(basePath, projectName);
        Console.WriteLine($@"[CreateDotNetClassLibrary] Project directory: {projectDirectory}");

        if (!Directory.Exists(projectDirectory))
        {
            Console.WriteLine($@"[CreateDotNetClassLibrary] Creating directory: {projectDirectory}");
            Directory.CreateDirectory(projectDirectory);
        }

        string csprojPath = Path.Combine(projectDirectory, $"{projectName}.csproj");
        Console.WriteLine($@"[CreateDotNetClassLibrary] CSProj path: {csprojPath}");
        
        if (File.Exists(csprojPath))
            throw new InvalidOperationException($"Project file already exists: {csprojPath}");

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"new classlib -n \"{projectName}\" -f net10.0 --no-restore -o \"{projectName}\"",
            WorkingDirectory = basePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Console.WriteLine($@"[CreateDotNetClassLibrary] Executing: dotnet {startInfo.Arguments}");

        using Process process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Console.WriteLine($@"[CreateDotNetClassLibrary] Exit code: {process.ExitCode}");
            if (!string.IsNullOrEmpty(output))
                Console.WriteLine($@"[CreateDotNetClassLibrary] Output: {output}");
            if (!string.IsNullOrEmpty(error))
                Console.WriteLine($@"[CreateDotNetClassLibrary] Error: {error}");

            if (process.ExitCode != 0)
                throw new Exception($"dotnet new failed: {error}");
            
            Console.WriteLine($@"[CreateDotNetClassLibrary] Class library created successfully!");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new Exception($"Failed to execute dotnet new: {ex.Message}", ex);
        }
    }

    private static void InitializeGitRepository(string basePath)
    {
        Console.WriteLine($@"[InitializeGitRepository] Initializing git repository...");
        Console.WriteLine($@"[InitializeGitRepository] Directory: {basePath}");

        string gitPath = Path.Combine(basePath, ".git");
        if (Directory.Exists(gitPath))
        {
            Console.WriteLine($@"[InitializeGitRepository] Git repository already exists, skipping.");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "init",
            WorkingDirectory = basePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Console.WriteLine($@"[InitializeGitRepository] Executing: git init");

        using Process process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Console.WriteLine($@"[InitializeGitRepository] Exit code: {process.ExitCode}");
            if (!string.IsNullOrEmpty(output))
                Console.WriteLine($@"[InitializeGitRepository] Output: {output}");
            if (!string.IsNullOrEmpty(error))
                Console.WriteLine($@"[InitializeGitRepository] Error: {error}");

            if (process.ExitCode != 0)
                throw new Exception($"Git init failed: {error}");
            
            Console.WriteLine($@"[InitializeGitRepository] Git repository initialized successfully!");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to execute git init: {ex.Message}", ex);
        }
    }

    private static void AddGitSubmodule(string basePath)
    {
        Console.WriteLine($@"[AddGitSubmodule] Adding git submodule...");
        Console.WriteLine($@"[AddGitSubmodule] Directory: {basePath}");

        string modulesDir = Path.Combine(basePath, "modules");
        string submodulePath = Path.Combine(modulesDir, "Round.SDK");

        if (Directory.Exists(submodulePath))
        {
            Console.WriteLine($@"[AddGitSubmodule] Submodule already exists at: {submodulePath}, skipping.");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"submodule add https://github.com/Round-Studio/Round.SDK modules/Round.SDK",
            WorkingDirectory = basePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Console.WriteLine($@"[AddGitSubmodule] Executing: git submodule add https://github.com/Round-Studio/Round.SDK modules/Round.SDK");

        using Process process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Console.WriteLine($@"[AddGitSubmodule] Exit code: {process.ExitCode}");
            if (!string.IsNullOrEmpty(output))
                Console.WriteLine($@"[AddGitSubmodule] Output: {output}");
            if (!string.IsNullOrEmpty(error))
                Console.WriteLine($@"[AddGitSubmodule] Error: {error}");

            if (process.ExitCode != 0)
                throw new Exception($"Git submodule add failed: {error}");
            
            Console.WriteLine($@"[AddGitSubmodule] Submodule added successfully!");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to execute git submodule add: {ex.Message}", ex);
        }
    }

    private static void CreateSolutionFile(string basePath, string projectName)
    {
        Console.WriteLine($@"[CreateSolutionFile] Creating solution file...");
        Console.WriteLine($@"[CreateSolutionFile] Directory: {basePath}");

        string slnPath = Path.Combine(basePath, $"{projectName}.slnx");
        Console.WriteLine($@"[CreateSolutionFile] Solution path: {slnPath}");
        
        if (File.Exists(slnPath))
        {
            Console.WriteLine($@"[CreateSolutionFile] Solution file already exists, skipping.");
            return;
        }

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
        Console.WriteLine($@"[CreateSolutionFile] Solution file created successfully!");
    }

    private static void AddProjectReferences(string projectDirectory, string projectName)
    {
        Console.WriteLine($@"[AddProjectReferences] Adding project references...");
        Console.WriteLine($@"[AddProjectReferences] Project directory: {projectDirectory}");
        
        string csprojPath = Path.Combine(projectDirectory, $"{projectName}.csproj");
        Console.WriteLine($@"[AddProjectReferences] CSProj path: {csprojPath}");
        
        if (!File.Exists(csprojPath))
        {
            Console.WriteLine($@"[AddProjectReferences] CSProj file not found at: {csprojPath}");
            return;
        }

        Console.WriteLine($@"[AddProjectReferences] Loading CSProj file...");
        XDocument doc = XDocument.Load(csprojPath);
        XNamespace ns = doc.Root.GetDefaultNamespace();

        XElement itemGroup = doc.Root.Elements(ns + "ItemGroup")
            .FirstOrDefault(x => x.Elements(ns + "ProjectReference").Any());

        if (itemGroup == null)
        {
            Console.WriteLine($@"[AddProjectReferences] No existing ItemGroup with ProjectReference found, creating new one.");
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
            Console.WriteLine($@"[AddProjectReferences] Checking reference: {fullRefPath}");
            
            if (File.Exists(fullRefPath))
            {
                Console.WriteLine($@"[AddProjectReferences] Reference file exists: {refPath}");
                
                XElement projectRef = new XElement(ns + "ProjectReference",
                    new XAttribute("Include", refPath));

                if (!itemGroup.Elements(ns + "ProjectReference")
                    .Any(x => (string)x.Attribute("Include") == refPath))
                {
                    Console.WriteLine($@"[AddProjectReferences] Adding reference: {refPath}");
                    itemGroup.Add(projectRef);
                    addedCount++;
                }
                else
                {
                    Console.WriteLine($@"[AddProjectReferences] Reference already exists: {refPath}");
                }
            }
            else
            {
                Console.WriteLine($@"[AddProjectReferences] Reference file not found: {fullRefPath}");
            }
        }

        if (addedCount > 0)
        {
            Console.WriteLine($@"[AddProjectReferences] Saving CSProj file...");
            doc.Save(csprojPath);
            Console.WriteLine($@"[AddProjectReferences] Added {addedCount} project references successfully!");
        }
        else
        {
            Console.WriteLine($@"[AddProjectReferences] No new references added.");
        }
    }

    private static void GitCommit(string basePath, string projectName)
    {
        Console.WriteLine($@"[GitCommit] Committing initial changes...");
        Console.WriteLine($@"[GitCommit] Directory: {basePath}");

        ProcessStartInfo addStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "add .",
            WorkingDirectory = basePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Console.WriteLine($@"[GitCommit] Executing: git add .");

        using Process addProcess = new Process { StartInfo = addStartInfo };

        try
        {
            addProcess.Start();
            string addOutput = addProcess.StandardOutput.ReadToEnd();
            string addError = addProcess.StandardError.ReadToEnd();
            addProcess.WaitForExit();

            Console.WriteLine($@"[GitCommit] Git add exit code: {addProcess.ExitCode}");
            if (!string.IsNullOrEmpty(addOutput))
                Console.WriteLine($@"[GitCommit] Add output: {addOutput}");
            if (!string.IsNullOrEmpty(addError))
                Console.WriteLine($@"[GitCommit] Add error: {addError}");

            if (addProcess.ExitCode != 0)
                throw new Exception($"Git add failed: {addError}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to execute git add: {ex.Message}", ex);
        }

        ProcessStartInfo commitStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"commit -m \"Initial commit for {projectName}\"",
            WorkingDirectory = basePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Console.WriteLine($@"[GitCommit] Executing: git commit");

        using Process commitProcess = new Process { StartInfo = commitStartInfo };

        try
        {
            commitProcess.Start();
            string commitOutput = commitProcess.StandardOutput.ReadToEnd();
            string commitError = commitProcess.StandardError.ReadToEnd();
            commitProcess.WaitForExit();

            Console.WriteLine($@"[GitCommit] Git commit exit code: {commitProcess.ExitCode}");
            if (!string.IsNullOrEmpty(commitOutput))
                Console.WriteLine($@"[GitCommit] Commit output: {commitOutput}");
            if (!string.IsNullOrEmpty(commitError))
                Console.WriteLine($@"[GitCommit] Commit error: {commitError}");

            if (commitProcess.ExitCode != 0)
                throw new Exception($"Git commit failed: {commitError}");
            
            Console.WriteLine($@"[GitCommit] Initial commit completed successfully!");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to execute git commit: {ex.Message}", ex);
        }
    }

    private static void BuildProject(string projectDirectory, string outputDirectory)
    {
        Console.WriteLine($@"[BuildProject] Building project...");
        Console.WriteLine($@"[BuildProject] Project directory: {projectDirectory}");
        Console.WriteLine($@"[BuildProject] Output directory: {outputDirectory}");

        if (!Directory.Exists(outputDirectory))
        {
            Console.WriteLine($@"[BuildProject] Creating output directory: {outputDirectory}");
            Directory.CreateDirectory(outputDirectory);
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build -c Debug -o \"{outputDirectory}\"",
            WorkingDirectory = projectDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Console.WriteLine($@"[BuildProject] Executing: dotnet {startInfo.Arguments}");

        using Process process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Console.WriteLine($@"[BuildProject] Exit code: {process.ExitCode}");
            if (!string.IsNullOrEmpty(output))
                Console.WriteLine($@"[BuildProject] Output: {output}");
            if (!string.IsNullOrEmpty(error))
                Console.WriteLine($@"[BuildProject] Error: {error}");

            if (process.ExitCode != 0)
                throw new Exception($"dotnet build failed: {error}");
            
            Console.WriteLine($@"[BuildProject] Project built successfully!");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to execute dotnet build: {ex.Message}", ex);
        }
    }

    #endregion
}