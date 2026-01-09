// MinecraftText.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using System.Collections.Generic;

namespace BedrockBoot.Models.Style
{
    public class MinecraftTextBlock : TextBlock
    {
        public static readonly StyledProperty<string> MinecraftTextProperty =
            AvaloniaProperty.Register<MinecraftTextBlock, string>(
                nameof(MinecraftText),
                defaultValue: string.Empty);

        public static readonly StyledProperty<bool> ShowRawTextProperty =
            AvaloniaProperty.Register<MinecraftTextBlock, bool>(
                nameof(ShowRawText),
                defaultValue: false);

        private List<MinecraftTextParser.TextSegment>? _currentSegments;

        public string MinecraftText
        {
            get => GetValue(MinecraftTextProperty);
            set => SetValue(MinecraftTextProperty, value);
        }

        public bool ShowRawText
        {
            get => GetValue(ShowRawTextProperty);
            set => SetValue(ShowRawTextProperty, value);
        }

        static MinecraftTextBlock()
        {
            MinecraftTextProperty.Changed.AddClassHandler<MinecraftTextBlock>((x, e) => x.OnTextChanged());
            ShowRawTextProperty.Changed.AddClassHandler<MinecraftTextBlock>((x, e) => x.OnTextChanged());
        }

        public MinecraftTextBlock()
        {
            this.Foreground = Brushes.White;
            this.TextWrapping = TextWrapping.Wrap;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            CleanupSegments();
        }

        private void OnTextChanged()
        {
            // 清理旧的混淆文本定时器
            CleanupSegments();

            if (ShowRawText)
            {
                // 显示原始文本
                this.Inlines?.Clear();
                this.Text = MinecraftText;
            }
            else
            {
                // 显示解析后的富文本
                this.Text = null;

                // 获取元组，只取第一个元素（InlineCollection）
                var (inlines, segments) = MinecraftTextParser.ConvertToInlines(MinecraftText);
                this.Inlines = inlines;

                // 保存segment引用并启动混淆文本定时器
                _currentSegments = segments;
                StartObfuscation();
            }
        }

        private void StartObfuscation()
        {
            if (_currentSegments == null) return;

            foreach (var segment in _currentSegments)
            {
                if (segment.IsObfuscated)
                {
                    segment.StartObfuscation();
                }
            }
        }

        private void CleanupSegments()
        {
            if (_currentSegments != null)
            {
                foreach (var segment in _currentSegments)
                {
                    segment.Dispose();
                }

                _currentSegments.Clear();
                _currentSegments = null;
            }
        }
    }
}