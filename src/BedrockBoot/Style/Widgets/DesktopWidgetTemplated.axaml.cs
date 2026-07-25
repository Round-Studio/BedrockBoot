using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Primitives;
using BedrockBoot.Base.Entry.Config;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Interface;

namespace BedrockBoot.Style.Widgets
{
    public class DesktopWidgetTemplated : ContentControl
    {
        public event Action<DesktopWidgetTemplated>? Resized;
        public event EventHandler<PointerPressedEventArgs>? RightButtonPressed;

        public static readonly StyledProperty<IWidgetTemplated> WidgetContentProperty =
            AvaloniaProperty.Register<DesktopWidgetTemplated, IWidgetTemplated>(nameof(WidgetContent));

        public IWidgetTemplated WidgetContent
        {
            get => GetValue<IWidgetTemplated>(WidgetContentProperty);
            set
            {
                SetValue(WidgetContentProperty, value);
                SetValue(ContentProperty, value);
                UpdateMaxSize();
            }
        }

        private Border? _resizeHandle;
        private Point _startMousePoint;
        private Size _startSize;
        private bool _isResizing;
        private Pointer? _activePointer;
        private WidgetLayoutData _widgetConfig = new();

        public WidgetLayoutData WidgetConfig
        {
            get => _widgetConfig;
            set => _widgetConfig = value;
        }

        /// <summary>
        /// 集中管理 WidgetSize 枚举与具体像素尺寸的映射
        /// </summary>
        private static Size GetWidgetDimensions(WidgetSize size) => size switch
        {
            WidgetSize.Small => new Size(180, 180),
            WidgetSize.Medium => new Size(180, 360),
            WidgetSize.Large => new Size(360, 180),
            WidgetSize.ExtraLarge => new Size(360, 360),
            _ => new Size(0, 0)
        };

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _resizeHandle = e.NameScope.Find<Border>("HoverBorder");

            if (_resizeHandle != null)
            {
                _resizeHandle.PointerPressed += OnHandlePointerPressed;
                _resizeHandle.PointerReleased += OnHandlePointerReleased;

                PointerMoved += OnGlobalPointerMoved;
                PointerReleased += OnGlobalPointerReleased;
            }

            SizeChanged += OnSizeChanged;
            PointerPressed += OnPointerPressed;

            UpdateMaxSize();
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                RightButtonPressed?.Invoke(this, e);
                e.Handled = true;
            }
        }

        private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (!_isResizing)
            {
                Resized?.Invoke(this);
            }
        }

        private void OnHandlePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                e.Handled = true;
                _isResizing = true;
                _activePointer = (Pointer?)e.Pointer;

                _startMousePoint = e.GetPosition(this);
                _startSize = new Size(
                    double.IsNaN(Width) ? Bounds.Width : Width,
                    double.IsNaN(Height) ? Bounds.Height : Height
                );

                // 拖拽时关闭过渡动画，避免延迟
                Transitions = null;
                e.Pointer.Capture(_resizeHandle);
            }
        }

        private void OnHandlePointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isResizing && e.Pointer == _activePointer)
            {
                StopResize(e.Pointer);
            }
        }

        private void OnGlobalPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isResizing && e.Pointer == _activePointer)
            {
                StopResize(e.Pointer);
            }
        }

        private void OnGlobalPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isResizing || e.Pointer != _activePointer) return;

            var currentMousePoint = e.GetPosition(this);
            double deltaX = currentMousePoint.X - _startMousePoint.X;
            double deltaY = currentMousePoint.Y - _startMousePoint.Y;

            double newWidth = Math.Clamp(_startSize.Width + deltaX, MinWidth, MaxWidth);
            double newHeight = Math.Clamp(_startSize.Height + deltaY, MinHeight, MaxHeight);

            Width = newWidth;
            Height = newHeight;
            
            UpdateMaxSize();
        }

        private void StopResize(IPointer pointer)
        {
            if (!_isResizing) return;

            _isResizing = false;
            _activePointer = null;
            pointer.Capture(null);

            // 恢复过渡动画，让 Snap 吸附效果更平滑
            Transitions = new Transitions
            {
                new DoubleTransition 
                { 
                    Property = WidthProperty, 
                    Duration = TimeSpan.FromMilliseconds(200), 
                    Easing = new ExponentialEaseOut() 
                },
                new DoubleTransition 
                { 
                    Property = HeightProperty, 
                    Duration = TimeSpan.FromMilliseconds(200), 
                    Easing = new ExponentialEaseOut() 
                }
            };

            SnapToNearestSize();
            Resized?.Invoke(this);
            
            UpdateMaxSize();
        }

        private void SnapToNearestSize()
        {
            if (WidgetContent?.SupportWidgetSize == null || !WidgetContent.SupportWidgetSize.Any())
                return;

            double currentW = double.IsNaN(Width) ? Bounds.Width : Width;
            double currentH = double.IsNaN(Height) ? Bounds.Height : Height;

            // 使用统一的映射方法获取所有支持的尺寸
            var supportedSizes = WidgetContent.SupportWidgetSize
                .Select(GetWidgetDimensions)
                .ToList();

            Size bestFit = supportedSizes[0];
            double minDistance = double.MaxValue;

            foreach (var size in supportedSizes)
            {
                double distance = Math.Sqrt(Math.Pow(size.Width - currentW, 2) + Math.Pow(size.Height - currentH, 2));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestFit = size;
                }
            }

            Width = bestFit.Width;
            Height = bestFit.Height;
        }

        /// <summary>
        /// 根据 SupportWidgetSize 枚举集合，动态计算并设定控件的最大/最小尺寸
        /// </summary>
        public void UpdateMaxSize()
        {
            if (WidgetContent?.SupportWidgetSize == null || !WidgetContent.SupportWidgetSize.Any())
                return;

            double maxArea = 0;
            double minArea = double.MaxValue;
            Size maxSize = default;
            Size minSize = default;

            foreach (var widgetSize in WidgetContent.SupportWidgetSize)
            {
                var dimensions = GetWidgetDimensions(widgetSize);
                double area = dimensions.Width * dimensions.Height;

                if (area > maxArea)
                {
                    maxArea = area;
                    maxSize = dimensions;
                }

                if (area < minArea)
                {
                    minArea = area;
                    minSize = dimensions;
                }
            }

            MaxWidth = maxSize.Width;
            MaxHeight = maxSize.Height;
            MinWidth = minSize.Width;
            MinHeight = minSize.Height;
        }
    }
}