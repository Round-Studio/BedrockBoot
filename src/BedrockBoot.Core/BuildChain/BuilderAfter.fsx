open System.IO

let content = """namespace BedrockBoot.Core
{
	public class Keys
	{
		public static readonly string CurseForgeApiKey = "你真的很想得到吗";
		public static readonly string GithubClientId = "O.o";
		public static readonly string GithubSecret = "O.o";
	}
}"""

let filename = "Keys.cs"

try
    File.WriteAllText(filename, content)
    printfn "Generate: %s" filename
with
| ex -> printfn "Failed to Generate: %s" ex.Message