using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using BedrockBoot.Base.Entry.Config;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Interface;

namespace BedrockBoot.Style.Widgets
{
    public class DesktopWidgetTemplated : ContentControl
    {
        public event Action<DesktopWidgetTemplated>? Resized;
        public event EventHandler<PointerPressedEventArgs>? RightButtonPressed;
        public static readonly StyledProperty<IWidgetTemplated> WidgetContentProperty = AvaloniaProperty.Register<DesktopWidgetTemplated, IWidgetTemplated>(nameof (WidgetContent));

        public IWidgetTemplated WidgetContent
        {
            get
            {
                return this.GetValue<IWidgetTemplated>(DesktopWidgetTemplated.WidgetContentProperty);
            }
            set
            {
                this.SetValue<IWidgetTemplated>(DesktopWidgetTemplated.WidgetContentProperty, value);
                this.SetValue<object?>(DesktopWidgetTemplated.ContentProperty, value);
                UpdateMaxSize();
            }
        }

        private static readonly Size[] SnapSizes = new Size[]
        {
            new Size(180, 180),
            new Size(180, 360),
            new Size(360, 180),
            new Size(360, 360)
        };

        private void UpdateMaxSize()
        {
            WidgetContent.SupportWidgetSize.ForEach(x =>
            {
                switch (x)
                {
                    case WidgetSize.ExtraLarge:
                        this.MaxWidth = this.MaxHeight = 360;
                        break;
                    case WidgetSize.Large:
                        this.MaxWidth = 360;
                        this.MaxHeight = 180;
                        break;
                    case WidgetSize.Medium:
                        this.MaxWidth = 180;
                        this.MaxHeight = 360;
                        break;
                    case WidgetSize.Small:
                        this.MaxWidth = 180;
                        this.MaxHeight = 180;
                        break;
                }
            });
        }

        private Border? _resizeHandle;
        private Point _startMousePoint;
        private Size _startSize;
        private bool _isResizing = false;
        private Pointer? _activePointer;
        private WidgetLayoutData _widgetConfig = new WidgetLayoutData();

        public WidgetLayoutData WidgetConfig
        {
            get => _widgetConfig;
            set => _widgetConfig = value;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _resizeHandle = e.NameScope.Find<Border>("HoverBorder");

            if (_resizeHandle != null)
            {
                _resizeHandle.PointerPressed += OnHandlePointerPressed;
                _resizeHandle.PointerReleased += OnHandlePointerReleased;

                this.PointerMoved += OnGlobalPointerMoved;
                this.PointerReleased += OnGlobalPointerReleased;
            }

            this.SizeChanged += OnSizeChanged;
            this.PointerPressed += OnPointerPressed;
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
                    double.IsNaN(this.Width) ? this.Bounds.Width : this.Width,
                    double.IsNaN(this.Height) ? this.Bounds.Height : this.Height
                );

                this.Transitions = null;
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

            double newWidth = _startSize.Width + deltaX;
            double newHeight = _startSize.Height + deltaY;

            newWidth = Math.Clamp(newWidth, this.MinWidth, this.MaxWidth);
            newHeight = Math.Clamp(newHeight, this.MinHeight, this.MaxHeight);

            this.Width = newWidth;
            this.Height = newHeight;
        }

        private void StopResize(IPointer pointer)
        {
            if (!_isResizing) return;

            _isResizing = false;
            _activePointer = null;

            pointer.Capture(null);

            this.Transitions = new Transitions
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
        }

        private void SnapToNearestSize()
        {
            double currentW = double.IsNaN(this.Width) ? this.Bounds.Width : this.Width;
            double currentH = double.IsNaN(this.Height) ? this.Bounds.Height : this.Height;

            Size bestFit = SnapSizes[0];
            double minDistance = double.MaxValue;

            foreach (var size in SnapSizes)
            {
                double distance = Math.Sqrt(Math.Pow(size.Width - currentW, 2) + Math.Pow(size.Height - currentH, 2));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestFit = size;
                }
            }

            this.Width = bestFit.Width;
            this.Height = bestFit.Height;
        }
    }
}