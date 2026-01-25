using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Windows.Markup;

namespace BedrockBoot.Models.Loader;

public class DynamicLayoutLoader
{
    public static Control LoadXamlFromFile(string filePath)
    {
        return (Control)AvaloniaRuntimeXamlLoader.Load(File.ReadAllText(filePath));
    }
}