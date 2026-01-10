using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace BedrockBoot.Models.Style;

// MinecraftColorCode.cs
public class MinecraftColorCode
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Color Color { get; set; }
    public string HexColor { get; set; } = string.Empty;
    public string AnsiCode { get; set; } = string.Empty;
    public bool IsBedrockExclusive { get; set; }
    public bool IsJavaExclusive { get; set; }
    
    public static IBrush GetFontColorResourceFromApp()
    {
        // Application.Current 是一个全局的入口点
        var app = Application.Current;
    
        if (app != null)
        {
            // 从应用程序的资源中查找 :cite[1]
            // 注意：这里查找的是 Application.Resources 里定义的资源
            if (app.TryFindResource("PrimaryForegroundBrush", out var resourceValue))
            {
                return resourceValue as IBrush;
            }
        }
    
        return new SolidColorBrush(Colors.Gray);
    }

    // 基础16色 + 特殊格式
    public static readonly Dictionary<string, MinecraftColorCode> ColorCodes = new()
    {
        // 基础颜色
        ["§0"] = new() { Code = "§0", Name = "black", HexColor = "#000000", Color = Colors.Black },
        ["§1"] = new() { Code = "§1", Name = "dark_blue", HexColor = "#0000AA", Color = Color.Parse("#0000AA") },
        ["§2"] = new() { Code = "§2", Name = "dark_green", HexColor = "#00AA00", Color = Color.Parse("#00AA00") },
        ["§3"] = new() { Code = "§3", Name = "dark_aqua", HexColor = "#00AAAA", Color = Color.Parse("#00AAAA") },
        ["§4"] = new() { Code = "§4", Name = "dark_red", HexColor = "#AA0000", Color = Color.Parse("#AA0000") },
        ["§5"] = new() { Code = "§5", Name = "dark_purple", HexColor = "#AA00AA", Color = Color.Parse("#AA00AA") },
        ["§6"] = new() { Code = "§6", Name = "gold", HexColor = "#FFAA00", Color = Color.Parse("#FFAA00") },
        ["§7"] = new() { Code = "§7", Name = "gray", HexColor = "#AAAAAA", Color = Color.Parse("#AAAAAA") },
        ["§8"] = new() { Code = "§8", Name = "dark_gray", HexColor = "#555555", Color = Color.Parse("#555555") },
        ["§9"] = new() { Code = "§9", Name = "blue", HexColor = "#5555FF", Color = Color.Parse("#5555FF") },
        ["§a"] = new() { Code = "§a", Name = "green", HexColor = "#55FF55", Color = Color.Parse("#55FF55") },
        ["§b"] = new() { Code = "§b", Name = "aqua", HexColor = "#55FFFF", Color = Color.Parse("#55FFFF") },
        ["§c"] = new() { Code = "§c", Name = "red", HexColor = "#FF5555", Color = Color.Parse("#FF5555") },
        ["§d"] = new() { Code = "§d", Name = "light_purple", HexColor = "#FF55FF", Color = Color.Parse("#FF55FF") },
        ["§e"] = new() { Code = "§e", Name = "yellow", HexColor = "#FFFF55", Color = Color.Parse("#FFFF55") },
        ["§f"] = new() { Code = "§f", Name = "white", HexColor = "#FFFFFF", Color = Color.Parse(GetFontColorResourceFromApp().ToString()) },

        // BE独占颜色
        ["§g"] = new()
        {
            Code = "§g", Name = "minecoin_gold", HexColor = "#DDD605", Color = Color.Parse("#DDD605"),
            IsBedrockExclusive = true
        },
        ["§h"] = new()
        {
            Code = "§h", Name = "material_quartz", HexColor = "#E3D4D1", Color = Color.Parse("#E3D4D1"),
            IsBedrockExclusive = true
        },
        ["§i"] = new()
        {
            Code = "§i", Name = "material_iron", HexColor = "#CECACA", Color = Color.Parse("#CECACA"),
            IsBedrockExclusive = true
        },
        ["§j"] = new()
        {
            Code = "§j", Name = "material_netherite", HexColor = "#443A3B", Color = Color.Parse("#443A3B"),
            IsBedrockExclusive = true
        },
        ["§m"] = new()
        {
            Code = "§m", Name = "material_redstone", HexColor = "#971607", Color = Color.Parse("#971607"),
            IsBedrockExclusive = true
        },
        ["§n"] = new()
        {
            Code = "§n", Name = "material_copper", HexColor = "#B4684D", Color = Color.Parse("#B4684D"),
            IsBedrockExclusive = true
        },
        ["§p"] = new()
        {
            Code = "§p", Name = "material_gold", HexColor = "#DEB12D", Color = Color.Parse("#DEB12D"),
            IsBedrockExclusive = true
        },
        ["§q"] = new()
        {
            Code = "§q", Name = "material_emerald", HexColor = "#47A036", Color = Color.Parse("#47A036"),
            IsBedrockExclusive = true
        },
        ["§s"] = new()
        {
            Code = "§s", Name = "material_diamond", HexColor = "#2CBAA8", Color = Color.Parse("#2CBAA8"),
            IsBedrockExclusive = true
        },
        ["§t"] = new()
        {
            Code = "§t", Name = "material_lapis", HexColor = "#21497B", Color = Color.Parse("#21497B"),
            IsBedrockExclusive = true
        },
        ["§u"] = new()
        {
            Code = "§u", Name = "material_amethyst", HexColor = "#9A5CC6", Color = Color.Parse("#9A5CC6"),
            IsBedrockExclusive = true
        },
    };

    // 格式代码
    public static readonly Dictionary<string, string> FormatCodes = new()
    {
        ["§k"] = "obfuscated", // 混淆/随机字符
        ["§l"] = "bold", // 粗体
        ["§m"] = "strikethrough", // 删除线
        ["§n"] = "underline", // 下划线
        ["§o"] = "italic", // 斜体
        ["§r"] = "reset", // 重置所有格式
    };
}