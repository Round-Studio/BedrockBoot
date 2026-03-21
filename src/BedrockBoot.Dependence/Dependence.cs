using System.Reflection;

namespace BedrockBoot.Dependence;

public class Dependence
{
    public static byte[] GetResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
    
        var resources = assembly.GetManifestResourceNames();
        Console.WriteLine(@"Available embedded resources:");
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
                    $"Resource '{fileName}' not found. Available resources: {string.Join(", ", resources)}");
            }
        
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}