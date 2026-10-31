open System
open System.IO

// 读取环境变量，找不到就返回 None（不抛异常）
let tryGetEnv (envVarName: string) =
    let envValue = Environment.GetEnvironmentVariable(envVarName)
    if String.IsNullOrEmpty(envValue) then None
    else Some envValue

// 转义，防止值里的引号/反斜杠破坏 C# 字符串字面量
let escape (s: string) = s.Replace("\\", "\\\\").Replace("\"", "\\\"")

// 生成 C# 源码；占位符作为默认值传入
let generateCSharpCode (curseForgeApiKey: string) (githubClientId: string) (githubSecret: string) =
    sprintf """namespace BedrockBoot.Core
{
    public class Keys
    {
	    public static readonly string CurseForgeApiKey = "%s";
	    public static readonly string GithubClientId = "%s";
	    public static readonly string GithubSecret = "%s";
    }
}""" (escape curseForgeApiKey) (escape githubClientId) (escape githubSecret)

try
    printfn "Starting to read environment variables and generate C# code..."

    // 读不到就保留原来的占位符
    let curseForgeApiKey =
        match tryGetEnv "CURSEFORGE_API_KEY" with
        | Some v -> printfn "CURSEFORGE_API_KEY found, using it."; v
        | None   -> printfn "CURSEFORGE_API_KEY not found, keeping placeholder."; "你真的很想得到吗"

    let githubClientId =
        match tryGetEnv "API_GITHUB_CLIENT_ID" with
        | Some v -> printfn "API_GITHUB_CLIENT_ID found, using it."; v
        | None   -> printfn "API_GITHUB_CLIENT_ID not found, keeping placeholder."; "O.o"

    let githubSecret =
        match tryGetEnv "API_GITHUB_SECRET" with
        | Some v -> printfn "API_GITHUB_SECRET found, using it."; v
        | None   -> printfn "API_GITHUB_SECRET not found, keeping placeholder."; "O.o"

    let content = generateCSharpCode curseForgeApiKey githubClientId githubSecret

    let filename = Path.Combine(Directory.GetCurrentDirectory(), "Keys.cs")
    File.WriteAllText(filename, content)

    printfn "Generate: %s" filename
    printfn "Generation successful!"

with
| ex ->
    printfn "Failed to Generate: %s" ex.Message
    Environment.Exit(1)