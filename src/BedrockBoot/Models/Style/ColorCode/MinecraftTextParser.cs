// MinecraftTextParser.cs
using Avalonia.Controls.Documents;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BedrockBoot.Models.Style
{
    public class MinecraftTextParser
    {
        public class TextSegment : IDisposable
        {
            // 临时文本，用于构建过程中的存储
            private string _text = string.Empty;
            
            public string Text
            {
                get => _text;
                set => _text = value ?? string.Empty;
            }
            
            public string OriginalText { get; set; } = string.Empty;
            public Color? Color { get; set; }
            public bool IsBold { get; set; }
            public bool IsItalic { get; set; }
            public bool IsUnderline { get; set; }
            public bool IsStrikethrough { get; set; }
            public bool IsObfuscated { get; set; }
            
            // 混淆文本的Run引用，用于更新
            public Run? ObfuscatedRun { get; set; }
            
            // 定时器用于更新混淆文本
            private System.Threading.Timer? _obfuscationTimer;
            
            public void StartObfuscation()
            {
                if (!IsObfuscated || ObfuscatedRun == null) return;
                
                _obfuscationTimer?.Dispose();
                _obfuscationTimer = new System.Threading.Timer(_ =>
                {
                    if (ObfuscatedRun != null)
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            ObfuscatedRun.Text = GenerateObfuscatedText(OriginalText);
                        });
                    }
                }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(20)); // 每100ms更新一次
            }
            
            public void StopObfuscation()
            {
                _obfuscationTimer?.Dispose();
                _obfuscationTimer = null;
            }
            
            public void Dispose()
            {
                StopObfuscation();
                _obfuscationTimer?.Dispose();
            }
        }
        
        public static List<TextSegment> ParseMinecraftText(string input)
        {
            var segments = new List<TextSegment>();
            var currentSegment = new TextSegment();
            
            Color? currentColor = null;
            bool isBold = false;
            bool isItalic = false;
            bool isUnderline = false;
            bool isStrikethrough = false;
            bool isObfuscated = false;
            
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '§' && i + 1 < input.Length)
                {
                    // 保存当前段落的文本
                    if (!string.IsNullOrEmpty(currentSegment.Text))
                    {
                        currentSegment.OriginalText = currentSegment.Text;
                        segments.Add(currentSegment);
                        currentSegment = new TextSegment();
                    }
                    
                    string code = $"§{input[i + 1]}";
                    i++; // 跳过颜色代码字符
                    
                    if (MinecraftColorCode.ColorCodes.TryGetValue(code, out var colorCode))
                    {
                        currentColor = colorCode.Color;
                    }
                    else
                    {
                        switch (code)
                        {
                            case "§l": isBold = true; break;
                            case "§o": isItalic = true; break;
                            case "§n": isUnderline = true; break;
                            case "§m": isStrikethrough = true; break;
                            case "§k": isObfuscated = true; break;
                            case "§r":
                                // 重置所有格式
                                currentColor = null;
                                isBold = isItalic = isUnderline = isStrikethrough = isObfuscated = false;
                                break;
                        }
                    }
                    
                    currentSegment.Color = currentColor;
                    currentSegment.IsBold = isBold;
                    currentSegment.IsItalic = isItalic;
                    currentSegment.IsUnderline = isUnderline;
                    currentSegment.IsStrikethrough = isStrikethrough;
                    currentSegment.IsObfuscated = isObfuscated;
                }
                else
                {
                    currentSegment.Text += input[i];
                }
            }
            
            // 添加最后一个段落
            if (!string.IsNullOrEmpty(currentSegment.Text))
            {
                currentSegment.OriginalText = currentSegment.Text;
                segments.Add(currentSegment);
            }
            
            return segments;
        }
        
        public static (InlineCollection Inlines, List<TextSegment> Segments) ConvertToInlines(string text)
        {
            var inlines = new InlineCollection();
            var segments = ParseMinecraftText(text);
            
            foreach (var segment in segments)
            {
                var run = new Run(segment.IsObfuscated ? GenerateObfuscatedText(segment.OriginalText) : segment.OriginalText);
                
                // 设置颜色
                if (segment.Color.HasValue)
                {
                    run.Foreground = new SolidColorBrush(segment.Color.Value);
                }
                
                // 设置字体样式
                run.FontWeight = segment.IsBold ? FontWeight.Bold : FontWeight.Normal;
                run.FontStyle = segment.IsItalic ? FontStyle.Italic : FontStyle.Normal;
                
                // 设置文本装饰
                var decorations = new TextDecorationCollection();
                
                run.TextDecorations = decorations;
                
                // 保存Run引用用于混淆文本更新
                if (segment.IsObfuscated)
                {
                    segment.ObfuscatedRun = run;
                }
                
                inlines.Add(run);
            }
            
            return (inlines, segments);
        }
        
        private static string GenerateObfuscatedText(string input)
        {
            const string chars = "!@#$%^&*()_+-=[]{}|;':\",./<>?";
            var random = new Random(Guid.NewGuid().GetHashCode());
            var result = new char[input.Length];
            
            for (int i = 0; i < input.Length; i++)
            {
                result[i] = char.IsWhiteSpace(input[i]) 
                    ? input[i] 
                    : chars[random.Next(chars.Length)];
            }
            
            return new string(result);
        }
    }
}