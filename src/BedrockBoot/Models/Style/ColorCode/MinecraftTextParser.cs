// MinecraftTextParser.cs

using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;

namespace BedrockBoot.Models.Style;

public class MinecraftTextParser
{
    public static List<TextSegment> ParseMinecraftText(string input)
    {
        var segments = new List<TextSegment>();
        var currentSegment = new TextSegment();

        Color? currentColor = null;
        var isBold = false;
        var isItalic = false;
        var isUnderline = false;
        var isStrikethrough = false;
        var isObfuscated = false;

        for (var i = 0; i < input.Length; i++)
        {
            if (input[i] == '§' && i + 1 < input.Length)
            {
                if (!string.IsNullOrEmpty(currentSegment.Text))
                {
                    currentSegment.OriginalText = currentSegment.Text;
                    segments.Add(currentSegment);
                    currentSegment = new TextSegment();
                }

                var code = $"§{input[i + 1]}";
                i++;

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
            var displayText = segment.IsObfuscated ? GenerateObfuscatedText(segment.OriginalText) : segment.OriginalText;
            var run = new Run(displayText);

            if (segment.Color.HasValue)
            {
                run.Foreground = new SolidColorBrush(segment.Color.Value);
            }

            run.FontWeight = segment.IsBold ? FontWeight.Bold : FontWeight.Normal;
            run.FontStyle = segment.IsItalic ? FontStyle.Italic : FontStyle.Normal;

            var decorations = new TextDecorationCollection();
            run.TextDecorations = decorations;

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
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        const string chars = "!@#$%^&*()_+-=[]{}|;':\",./<>?";
        var random = new Random(Guid.NewGuid().GetHashCode());
        var result = new char[input.Length];

        for (var i = 0; i < input.Length; i++)
        {
            result[i] = char.IsWhiteSpace(input[i]) ? input[i] : chars[random.Next(chars.Length)];
        }

        return new string(result);
    }

    public class TextSegment : IDisposable
    {
        private Timer _obfuscationTimer;
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
        public Run ObfuscatedRun { get; set; }

        public void Dispose()
        {
            StopObfuscation();
            _obfuscationTimer?.Dispose();
        }

        public void StartObfuscation()
        {
            if (!IsObfuscated || ObfuscatedRun == null) return;

            _obfuscationTimer?.Dispose();
            _obfuscationTimer = new Timer(_ =>
            {
                if (ObfuscatedRun != null)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        ObfuscatedRun.Text = GenerateObfuscatedText(OriginalText);
                    });
                }
            }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(50));
        }

        public void StopObfuscation()
        {
            _obfuscationTimer?.Dispose();
            _obfuscationTimer = null;
            
            if (ObfuscatedRun != null && !string.IsNullOrEmpty(OriginalText))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ObfuscatedRun.Text = OriginalText;
                });
            }
        }
    }
}