using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Helper
{

    public sealed class HotKey
    {
        public int? VirtualKey { get; set; } // 允许为空

        public HotKeyModifier Modifiers { get; set; }

        public override string ToString()
        {
            var parts = new List<string>();

            if (Modifiers.HasFlag(HotKeyModifier.Ctrl))
                parts.Add("Ctrl");

            if (Modifiers.HasFlag(HotKeyModifier.Alt))
                parts.Add("Alt");

            if (Modifiers.HasFlag(HotKeyModifier.Shift))
                parts.Add("Shift");

            if (Modifiers.HasFlag(HotKeyModifier.Win))
                parts.Add("Win");

            // 没有 Key 时只输出修饰键
            if (VirtualKey.HasValue)
                parts.Add(GetKeyName(VirtualKey.Value));

            return string.Join("+", parts);
        }
        public static HotKey Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Hotkey string is empty.");

            var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            HotKeyModifier modifiers = HotKeyModifier.None;
            int? key = null;

            foreach (var raw in parts)
            {
                var part = raw.Trim();

                // modifiers
                switch (part.ToLowerInvariant())
                {
                    case "ctrl":
                        modifiers |= HotKeyModifier.Ctrl;
                        continue;
                    case "alt":
                        modifiers |= HotKeyModifier.Alt;
                        continue;
                    case "shift":
                        modifiers |= HotKeyModifier.Shift;
                        continue;
                    case "win":
                    case "meta":
                        modifiers |= HotKeyModifier.Win;
                        continue;
                }

                // key（最后一个非 modifier）
                key = ParseKey(part);
            }

            return new HotKey
            {
                Modifiers = modifiers,
                VirtualKey = key
            };
        }
        private static int ParseKey(string text)
        {
            if (SpecialKeys.TryGetValue(text, out var vk))
                return vk;

            if (OemKeys.TryGetValue(text, out vk))
                return vk;

            if (text.StartsWith("F", StringComparison.OrdinalIgnoreCase) && int.TryParse(text[1..], out int f) && f >= 1 && f <= 24)
            {
                return 0x70 + (f - 1);
            }
            if (text.StartsWith("NumPad", StringComparison.OrdinalIgnoreCase) && int.TryParse(text[6..], out int n) && n >= 0 && n <= 9)
            {
                return 0x60 + n;
            }
            if (text.Length == 1)
            {
                char c = char.ToUpperInvariant(text[0]);

                if (c >= 'A' && c <= 'Z')
                    return c;

                if (c >= '0' && c <= '9')
                    return c;
            }
            if (text.StartsWith("VK_", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToInt32(text[3..], 16);
            }

            throw new FormatException($"Unknown key: {text}");
        }

        private static readonly Dictionary<string, int> OemKeys = new()
        {
            [";"] = 0xBA,
            ["="] = 0xBB,
            [","] = 0xBC,
            ["-"] = 0xBD,
            ["."] = 0xBE,
            ["/"] = 0xBF,
            ["`"] = 0xC0,
            ["["] = 0xDB,
            ["\\"] = 0xDC,
            ["]"] = 0xDD,
            ["'"] = 0xDE,
        };
        private static readonly Dictionary<string, int> SpecialKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Left"] = 0x25,
            ["Up"] = 0x26,
            ["Right"] = 0x27,
            ["Down"] = 0x28,

            ["PageUp"] = 0x21,
            ["PageDown"] = 0x22,

            ["Home"] = 0x24,
            ["End"] = 0x23,

            ["Insert"] = 0x2D,
            ["Delete"] = 0x2E,

            ["Backspace"] = 0x08,
            ["Tab"] = 0x09,
            ["Enter"] = 0x0D,
            ["Escape"] = 0x1B,
            ["Space"] = 0x20,

            ["CapsLock"] = 0x14,
            ["NumLock"] = 0x90,
            ["ScrollLock"] = 0x91,

            ["Pause"] = 0x13,
            ["PrintScreen"] = 0x2C,

            ["Apps"] = 0x5D,

            ["Multiply"] = 0x6A,
            ["Add"] = 0x6B,
            ["Separator"] = 0x6C,
            ["Subtract"] = 0x6D,
            ["Decimal"] = 0x6E,
            ["Divide"] = 0x6F,
        };
        public static bool TryParse(string text, out HotKey hotkey)
        {
            try
            {
                hotkey = Parse(text);
                return true;
            }
            catch
            {
                hotkey = null!;
                return false;
            }
        }
        public bool IsPressed()
        {
            bool Check(int vk)
                => (GetAsyncKeyState(vk) & 0x8000) != 0;

            if (Modifiers.HasFlag(HotKeyModifier.Ctrl) && !Check(0x11))
                return false;

            if (Modifiers.HasFlag(HotKeyModifier.Alt) && !Check(0x12))
                return false;

            if (Modifiers.HasFlag(HotKeyModifier.Shift) && !Check(0x10))
                return false;

            if (VirtualKey.HasValue)
                return Check(VirtualKey.Value);

            return true;
        }

        private static string GetKeyName(int vk)
        {
            return vk switch
            {
                >= 0x70 and <= 0x87 => $"F{vk - 0x6F}",

                0x25 => "Left",
                0x26 => "Up",
                0x27 => "Right",
                0x28 => "Down",

                0x21 => "PageUp",
                0x22 => "PageDown",
                0x23 => "End",
                0x24 => "Home",

                0x2D => "Insert",
                0x2E => "Delete",

                0x08 => "Backspace",
                0x09 => "Tab",
                0x0D => "Enter",
                0x1B => "Escape",
                0x20 => "Space",

                >= 0x30 and <= 0x39 => ((char)vk).ToString(), // 0-9
                >= 0x41 and <= 0x5A => ((char)vk).ToString(), // A-Z

                0x60 => "NumPad0",
                0x61 => "NumPad1",
                0x62 => "NumPad2",
                0x63 => "NumPad3",
                0x64 => "NumPad4",
                0x65 => "NumPad5",
                0x66 => "NumPad6",
                0x67 => "NumPad7",
                0x68 => "NumPad8",
                0x69 => "NumPad9",

                0x6A => "Multiply",
                0x6B => "Add",
                0x6C => "Separator",
                0x6D => "Subtract",
                0x6E => "Decimal",
                0x6F => "Divide",

                0x90 => "NumLock",
                0x91 => "ScrollLock",
                0x14 => "CapsLock",

                0x13 => "Pause",
                0x2C => "PrintScreen",

                0x5D => "Apps",

                // OEM keys（键盘符号区）
                0xBA => ";",
                0xBB => "=",
                0xBC => ",",
                0xBD => "-",
                0xBE => ".",
                0xBF => "/",
                0xC0 => "`",
                0xDB => "[",
                0xDC => "\\",
                0xDD => "]",
                0xDE => "'",

                _ => $"VK_{vk:X2}"
            };
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12;
        private const int VK_SHIFT = 0x10;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
    }

    [Flags]
    public enum HotKeyModifier
    {
        None = 0,
        Ctrl = 1,
        Alt = 2,
        Shift = 4,
        Win = 8
    }

    public static class HotKeyHelper
    {
        public sealed class HotKeyCaptureSession : IDisposable
        {
            private readonly TopLevel _topLevel;
            private readonly TaskCompletionSource<HotKey> _tcs;

            private bool _finished;

            private HotKeyModifier _modifiers;
            private int? _key;

            public Task<HotKey> Task => _tcs.Task;

            public HotKeyCaptureSession(TopLevel topLevel)
            {
                _topLevel = topLevel;
                _tcs = new TaskCompletionSource<HotKey>();

                _topLevel.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
                _topLevel.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
                _topLevel.LostFocus += OnLostFocus;
            }

            private void OnLostFocus(object? sender, EventArgs e)
                => Cancel();

            private void OnKeyDown(object? sender, KeyEventArgs e)
            {
                if (e.Key == Key.Escape)
                {
                    Cancel();
                    return;
                }

                _modifiers = HotKeyHelper.ConvertModifiers(e.KeyModifiers);

                // 记录最后一个“非修饰键”
                if (!IsModifierKey(e.Key))
                {
                    _key = HotKeyHelper.ToVirtualKey(e.Key);
                }

                e.Handled = true;
            }

            private void OnKeyUp(object? sender, KeyEventArgs e)
            {
                if (_finished)
                    return;

                
                // 只要有修饰键变化发生释放 → 可以提交

                bool hasModifiers = _modifiers != HotKeyModifier.None;

                if (!hasModifiers && !_key.HasValue)
                    return;

                Finish(new HotKey
                {
                    VirtualKey = _key,   // 可以为 null（纯修饰键组合）
                    Modifiers = _modifiers
                });
            }

            private void Finish(HotKey hotkey)
            {
                if (_finished) return;

                _finished = true;

                _topLevel.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
                _topLevel.RemoveHandler(InputElement.KeyUpEvent, OnKeyUp);
                _topLevel.LostFocus -= OnLostFocus;

                _tcs.TrySetResult(hotkey);
            }

            private void Cancel()
            {
                if (_finished) return;

                _finished = true;

                _topLevel.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
                _topLevel.RemoveHandler(InputElement.KeyUpEvent, OnKeyUp);
                _topLevel.LostFocus -= OnLostFocus;

                _tcs.TrySetResult(null!);
            }
            private static bool IsModifierKey(Key key)
            {
                return key is
                    Key.LeftCtrl or Key.RightCtrl or
                    Key.LeftAlt or Key.RightAlt or
                    Key.LeftShift or Key.RightShift or
                    Key.LWin or Key.RWin;
            }
            public void Dispose() => Cancel();
        }

        public static HotKeyCaptureSession Begin(InputElement element)
        {
            var topLevel = TopLevel.GetTopLevel(element)
                  ?? throw new InvalidOperationException("No TopLevel found");

            topLevel.Focus();

            return new HotKeyCaptureSession(topLevel);
        }

        internal static HotKeyModifier ConvertModifiers(KeyModifiers modifiers)
        {
            HotKeyModifier result = HotKeyModifier.None;

            if (modifiers.HasFlag(KeyModifiers.Control))
                result |= HotKeyModifier.Ctrl;

            if (modifiers.HasFlag(KeyModifiers.Alt))
                result |= HotKeyModifier.Alt;

            if (modifiers.HasFlag(KeyModifiers.Shift))
                result |= HotKeyModifier.Shift;

            if (modifiers.HasFlag(KeyModifiers.Meta))
                result |= HotKeyModifier.Win;

            return result;
        }

        internal static int ToVirtualKey(Key key)
        {
            return key switch
            {
                Key.F1 => 0x70,
                Key.F2 => 0x71,
                Key.F3 => 0x72,
                Key.F4 => 0x73,
                Key.F5 => 0x74,
                Key.F6 => 0x75,
                Key.F7 => 0x76,
                Key.F8 => 0x77,
                Key.F9 => 0x78,
                Key.F10 => 0x79,
                Key.F11 => 0x7A,
                Key.F12 => 0x7B,
                Key.F13 => 0x7C,
                Key.F14 => 0x7D,
                Key.F15 => 0x7E,
                Key.F16 => 0x7F,
                Key.F17 => 0x80,
                Key.F18 => 0x81,
                Key.F19 => 0x82,
                Key.F20 => 0x83,
                Key.F21 => 0x84,
                Key.F22 => 0x85,
                Key.F23 => 0x86,
                Key.F24 => 0x87,

                Key.Left => 0x25,
                Key.Up => 0x26,
                Key.Right => 0x27,
                Key.Down => 0x28,

                Key.PageUp => 0x21,
                Key.PageDown => 0x22,
                Key.Home => 0x24,
                Key.End => 0x23,

                Key.Insert => 0x2D,
                Key.Delete => 0x2E,

                Key.Back => 0x08,
                Key.Tab => 0x09,
                Key.Enter => 0x0D,
                Key.Escape => 0x1B,
                Key.Space => 0x20,

                Key.CapsLock => 0x14,

                >= Key.A and <= Key.Z => 0x41 + (key - Key.A),

                >= Key.D0 and <= Key.D9 => 0x30 + (key - Key.D0),

                Key.NumPad0 => 0x60,
                Key.NumPad1 => 0x61,
                Key.NumPad2 => 0x62,
                Key.NumPad3 => 0x63,
                Key.NumPad4 => 0x64,
                Key.NumPad5 => 0x65,
                Key.NumPad6 => 0x66,
                Key.NumPad7 => 0x67,
                Key.NumPad8 => 0x68,
                Key.NumPad9 => 0x69,

                Key.Multiply => 0x6A,
                Key.Add => 0x6B,
                Key.Separator => 0x6C,
                Key.Subtract => 0x6D,
                Key.Decimal => 0x6E,
                Key.Divide => 0x6F,

                Key.OemSemicolon => 0xBA,
                Key.OemPlus => 0xBB,
                Key.OemComma => 0xBC,
                Key.OemMinus => 0xBD,
                Key.OemPeriod => 0xBE,
                Key.OemQuestion => 0xBF,
                Key.OemTilde => 0xC0,
                Key.OemOpenBrackets => 0xDB,
                Key.OemPipe => 0xDC,
                Key.OemCloseBrackets => 0xDD,
                Key.OemQuotes => 0xDE,

                Key.PrintScreen => 0x2C,
                Key.Pause => 0x13,
                Key.Apps => 0x5D,

                _ => (int)key
            };
        }
    }
}
