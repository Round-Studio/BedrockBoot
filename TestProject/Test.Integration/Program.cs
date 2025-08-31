using BedrockBoot.Integration.Classes.Save;
using BedrockBoot.Integration.Entry;

namespace Test.Integration
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var save = new SaveIntegration(new IntegrationInfo()
            {
                VersionOntologyInfo = new VersionOntologyInfo()
                {
                    FolderPath = @"K:\Bedrock_Data",
                    Name = "1.21.11024"
                }
            });
            Progress<(double,string)> progress = new Progress<(double, string)>();
            progress.ProgressChanged += (sender, tuple) =>
            {
                Console.WriteLine($"{tuple.Item1:0.00} | {tuple.Item2}");
            };
            save.StartMake(progress);
        }
    }
}
