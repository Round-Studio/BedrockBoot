using System.IO;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BedrockBoot.Models.Loader;

public class DynamicLayoutLoader
{
    public static Control LoadXamlFromFile(string filePath)
    {
        return (Control)AvaloniaRuntimeXamlLoader.Load(File.ReadAllText(filePath));
    }
}