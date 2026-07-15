using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using BedrockBoot.Base.Entry.Config;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Style.Widgets;

namespace BedrockBoot.Views.Control.Widgets
{
    public class DesktopWorkspace : UserControl
    {
        private const double CellSize = 180.0;
        private const double Padding = 15;
        
        private ScrollViewer? _scrollViewer;
        private Canvas? _canvas;
        
        private DesktopWidgetTemplated? _draggingWidget;
        private Point _dragStartPoint;
        private Point _widgetInitialPosition;

        private Border? _ghostPlaceholder;
        private Dictionary<Point, DesktopWidgetTemplated> _occupiedGridCells = new Dictionary<Point, DesktopWidgetTemplated>();
        private List<DesktopWidgetTemplated> _allWidgets = new List<DesktopWidgetTemplated>();

        public event EventHandler<WidgetLayoutChangedEventArgs>? LayoutChanged;

        public DesktopWorkspace()
        {
            this.Content = CreateLayout();
            this.AttachedToVisualTree += OnAttachedToVisualTree;
            this.DetachedFromVisualTree += OnDetachedFromVisualTree;
            UpdateCanvasSize();
        }

        private Avalonia.Controls.Control CreateLayout()
        {
            _scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = _canvas = new Canvas()
                {
                    Margin = new Thickness(Padding),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                }
            };
            
            _canvas.PointerMoved += OnCanvasPointerMoved;
            return _scrollViewer;
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (_scrollViewer != null)
            {
                _scrollViewer.PropertyChanged += OnScrollViewerPropertyChanged;
            }
        }

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (_scrollViewer != null)
            {
                _scrollViewer.PropertyChanged -= OnScrollViewerPropertyChanged;
            }
        }

        private void OnScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == BoundsProperty)
            {
                UpdateCanvasSize();
            }
        }

        public void AddWidget(DesktopWidgetTemplated widget, WidgetSize size = WidgetSize.Small)
        {
            if (_canvas == null) return;

            SetWidgetSize(widget, size);
            
            var startPos = FindNearestFreeGridPosition(new Point(0, 0), widget);
            PlaceWidgetAtGrid(widget, startPos);
            
            _canvas.Children.Add(widget);
            _allWidgets.Add(widget);
            
            widget.PointerPressed += OnWidgetPointerPressed;
            widget.PointerReleased += OnWidgetPointerReleased;
            widget.Resized += OnWidgetResized;
            
            UpdateCanvasSize();
            OnLayoutChanged(widget);
        }

        public string ExportLayout()
        {
            var layoutData = new List<WidgetLayoutData>();
            
            foreach (var widget in _allWidgets)
            {
                var gridPos = GetWidgetGridPosition(widget);
                var size = GetWidgetSize(widget);
                
                var data = new WidgetLayoutData
                {
                    GridX = (int)gridPos.X,
                    GridY = (int)gridPos.Y,
                    Size = size,
                    WidgetConfig = widget.WidgetConfig
                };
                layoutData.Add(data);
            }
            
            return JsonSerializer.Serialize(layoutData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }

        public void ImportLayout(string json)
        {
            if (_canvas == null) return;
            
            var layoutData = JsonSerializer.Deserialize<List<WidgetLayoutData>>(json);
            if (layoutData == null) return;
            
            ClearAllWidgets();
            
            foreach (var data in layoutData)
            {
                var widget = CreateWidgetFromConfig(data.WidgetConfig);
                if (widget == null) continue;
                
                SetWidgetSize(widget, data.Size);
                
                var gridPos = new Point(data.GridX, data.GridY);
                PlaceWidgetAtGrid(widget, gridPos);
                
                _canvas.Children.Add(widget);
                _allWidgets.Add(widget);
                
                widget.PointerPressed += OnWidgetPointerPressed;
                widget.PointerReleased += OnWidgetPointerReleased;
                widget.Resized += OnWidgetResized;
            }
            
            UpdateCanvasSize();
        }

        private void ClearAllWidgets()
        {
            if (_canvas == null) return;
            
            foreach (var widget in _allWidgets)
            {
                widget.PointerPressed -= OnWidgetPointerPressed;
                widget.PointerReleased -= OnWidgetPointerReleased;
                widget.Resized -= OnWidgetResized;
                _canvas.Children.Remove(widget);
            }
            
            _allWidgets.Clear();
            _occupiedGridCells.Clear();
        }

        private DesktopWidgetTemplated? CreateWidgetFromConfig(WidgetConfig config)
        {
            if (config == null) return null;
            
            return config.WidgetType switch
            {
                _ => new DesktopWidgetTemplated()
            };
        }

        private void SetWidgetSize(DesktopWidgetTemplated widget, WidgetSize size)
        {
            var (width, height) = GetSizeDimensions(size);
            widget.Width = width;
            widget.Height = height;
        }

        private (double width, double height) GetSizeDimensions(WidgetSize size)
        {
            return size switch
            {
                WidgetSize.Small => (180, 180),
                WidgetSize.Medium => (180, 360),
                WidgetSize.Large => (360, 180),
                WidgetSize.ExtraLarge => (360, 360),
                _ => (180, 180)
            };
        }

        private WidgetSize GetWidgetSize(DesktopWidgetTemplated widget)
        {
            double width = widget.Bounds.Width > 0 ? widget.Bounds.Width : (double.IsNaN(widget.Width) ? 180 : widget.Width);
            double height = widget.Bounds.Height > 0 ? widget.Bounds.Height : (double.IsNaN(widget.Height) ? 180 : widget.Height);
            
            if (Math.Abs(width - 180) < 1 && Math.Abs(height - 180) < 1)
                return WidgetSize.Small;
            if (Math.Abs(width - 180) < 1 && Math.Abs(height - 360) < 1)
                return WidgetSize.Medium;
            if (Math.Abs(width - 360) < 1 && Math.Abs(height - 180) < 1)
                return WidgetSize.Large;
            if (Math.Abs(width - 360) < 1 && Math.Abs(height - 360) < 1)
                return WidgetSize.ExtraLarge;
                
            return WidgetSize.Small;
        }

        private Point GetWidgetGridPosition(DesktopWidgetTemplated widget)
        {
            double left = Canvas.GetLeft(widget);
            double top = Canvas.GetTop(widget);
            return new Point(left / CellSize, top / CellSize);
        }

        private void OnWidgetResized(DesktopWidgetTemplated widget)
        {
            if (widget == null) return;

            ClearWidgetOccupancy(widget);

            var currentPos = new Point(Canvas.GetLeft(widget), Canvas.GetTop(widget));
            var freeGridPos = FindNearestFreeGridPosition(currentPos, widget);
            PlaceWidgetAtGrid(widget, freeGridPos);
            
            UpdateCanvasSize();
            OnLayoutChanged(widget);
        }

        private void OnWidgetPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not DesktopWidgetTemplated widget) return;
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
            if (e.Handled) return;

            _draggingWidget = widget;
            _dragStartPoint = e.GetPosition(_canvas);
            _widgetInitialPosition = new Point(Canvas.GetLeft(widget), Canvas.GetTop(widget));

            ClearWidgetOccupancy(widget);
            CreateGhostPlaceholder(widget);

            _canvas?.Children.Remove(widget);
            _canvas?.Children.Add(widget);
            
            e.Pointer.Capture(widget);
        }

        private void OnWidgetPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_draggingWidget == null) return;

            var widget = _draggingWidget;
            _draggingWidget = null;

            e.Pointer.Capture(null);
            RemoveGhostPlaceholder();

            var currentPos = new Point(Canvas.GetLeft(widget), Canvas.GetTop(widget));
            var freeGridPos = FindNearestFreeGridPosition(currentPos, widget);
            
            PlaceWidgetAtGrid(widget, freeGridPos);
            UpdateCanvasSize();
            OnLayoutChanged(widget);
        }

        private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_draggingWidget == null || _canvas == null) return;

            var currentMousePos = e.GetPosition(_canvas);
            
            double deltaX = currentMousePos.X - _dragStartPoint.X;
            double deltaY = currentMousePos.Y - _dragStartPoint.Y;

            double newX = _widgetInitialPosition.X + deltaX;
            double newY = _widgetInitialPosition.Y + deltaY;

            Canvas.SetLeft(_draggingWidget, newX);
            Canvas.SetTop(_draggingWidget, newY);

            UpdateGhostPositionByCenter(newX, newY);
        }

        private void CreateGhostPlaceholder(DesktopWidgetTemplated widget)
        {
            if (_canvas == null) return;

            int cols = GetWidgetCols(widget);
            int rows = GetWidgetRows(widget);

            _ghostPlaceholder = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#313131"), 0.2),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(11),
                Width = cols * CellSize,
                Height = rows * CellSize,
                ZIndex = 0,
                IsVisible = false
            };

            _canvas.Children.Add(_ghostPlaceholder);
        }

        private void UpdateGhostPositionByCenter(double pixelLeft, double pixelTop)
        {
            if (_ghostPlaceholder == null || _draggingWidget == null) return;

            int widgetCols = GetWidgetCols(_draggingWidget);
            int widgetRows = GetWidgetRows(_draggingWidget);

            double widgetWidth = widgetCols * CellSize;
            double widgetHeight = widgetRows * CellSize;

            double widgetCenterX = pixelLeft + widgetWidth / 2;
            double widgetCenterY = pixelTop + widgetHeight / 2;

            var bestPos = FindNearestFreeGridPositionByCenter(widgetCenterX, widgetCenterY, widgetCols, widgetRows);

            if (bestPos != null)
            {
                _ghostPlaceholder.IsVisible = true;
                Canvas.SetLeft(_ghostPlaceholder, bestPos.Value.X * CellSize);
                Canvas.SetTop(_ghostPlaceholder, bestPos.Value.Y * CellSize);
            }
            else
            {
                _ghostPlaceholder.IsVisible = false;
            }
        }

        private void RemoveGhostPlaceholder()
        {
            if (_ghostPlaceholder != null && _canvas != null)
            {
                _canvas.Children.Remove(_ghostPlaceholder);
                _ghostPlaceholder = null;
            }
        }

        private Point? FindNearestFreeGridPositionByCenter(double centerX, double centerY, int widgetCols, int widgetRows)
        {
            int startCol = (int)Math.Floor(centerX / CellSize);
            int startRow = (int)Math.Floor(centerY / CellSize);

            int searchRadius = Math.Max(widgetCols, widgetRows) + 5;

            List<(Point pos, double distance)> candidates = new List<(Point, double)>();

            for (int r = Math.Max(0, startRow - searchRadius); r <= startRow + searchRadius; r++)
            {
                for (int c = Math.Max(0, startCol - searchRadius); c <= startCol + searchRadius; c++)
                {
                    if (!IsAreaFree(c, r, widgetCols, widgetRows))
                        continue;

                    double gridCenterX = c * CellSize + (widgetCols * CellSize) / 2;
                    double gridCenterY = r * CellSize + (widgetRows * CellSize) / 2;

                    double dist = Math.Pow(centerX - gridCenterX, 2) + Math.Pow(centerY - gridCenterY, 2);
                    candidates.Add((new Point(c, r), dist));
                }
            }

            if (candidates.Count == 0) return null;

            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
            return candidates[0].pos;
        }

        private Point FindNearestFreeGridPosition(Point targetPixelPos, DesktopWidgetTemplated widget)
        {
            int widgetCols = GetWidgetCols(widget);
            int widgetRows = GetWidgetRows(widget);

            double widgetWidth = widgetCols * CellSize;
            double widgetHeight = widgetRows * CellSize;

            double centerX = targetPixelPos.X + widgetWidth / 2;
            double centerY = targetPixelPos.Y + widgetHeight / 2;

            var bestPos = FindNearestFreeGridPositionByCenter(centerX, centerY, widgetCols, widgetRows);

            return bestPos ?? new Point(0, 0);
        }

        private int GetWidgetCols(DesktopWidgetTemplated widget)
        {
            double w = double.IsNaN(widget.Width) ? widget.Bounds.Width : widget.Width;
            return Math.Max(1, (int)Math.Ceiling(w / CellSize));
        }

        private int GetWidgetRows(DesktopWidgetTemplated widget)
        {
            double h = double.IsNaN(widget.Height) ? widget.Bounds.Height : widget.Height;
            return Math.Max(1, (int)Math.Ceiling(h / CellSize));
        }

        private bool IsAreaFree(int col, int row, int cols, int rows)
        {
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    if (_occupiedGridCells.ContainsKey(new Point(col + c, row + r)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void PlaceWidgetAtGrid(DesktopWidgetTemplated widget, Point gridPos)
        {
            double pixelX = gridPos.X * CellSize;
            double pixelY = gridPos.Y * CellSize;

            Canvas.SetLeft(widget, pixelX);
            Canvas.SetTop(widget, pixelY);

            RegisterWidgetOccupancy(widget, gridPos);
        }

        private void RegisterWidgetOccupancy(DesktopWidgetTemplated widget, Point gridPos)
        {
            int cols = GetWidgetCols(widget);
            int rows = GetWidgetRows(widget);
            
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    _occupiedGridCells[new Point(gridPos.X + c, gridPos.Y + r)] = widget;
                }
            }
        }

        private void ClearWidgetOccupancy(DesktopWidgetTemplated widget)
        {
            var keysToRemove = _occupiedGridCells.Where(kvp => kvp.Value == widget).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
            {
                _occupiedGridCells.Remove(key);
            }
        }

        private void UpdateCanvasSize()
        {
            if (_canvas == null || _scrollViewer == null) return;

            double viewportWidth = _scrollViewer.Bounds.Width > 0 ? _scrollViewer.Bounds.Width : 800;
            double viewportHeight = _scrollViewer.Bounds.Height > 0 ? _scrollViewer.Bounds.Height : 600;

            double maxWidth = viewportWidth;
            double maxHeight = viewportHeight;

            foreach (var widget in _allWidgets)
            {
                double left = Canvas.GetLeft(widget);
                double top = Canvas.GetTop(widget);
                
                double width = widget.Bounds.Width > 0 ? widget.Bounds.Width : (double.IsNaN(widget.Width) ? 180 : widget.Width);
                double height = widget.Bounds.Height > 0 ? widget.Bounds.Height : (double.IsNaN(widget.Height) ? 180 : widget.Height);

                double rightEdge = left + width;
                double bottomEdge = top + height;

                if (rightEdge > maxWidth) maxWidth = rightEdge;
                if (bottomEdge > maxHeight) maxHeight = bottomEdge;
            }

            _canvas.Width = Math.Max(viewportWidth, maxWidth);
            _canvas.Height = Math.Max(viewportHeight, maxHeight);
        }

        private void OnLayoutChanged(DesktopWidgetTemplated widget)
        {
            LayoutChanged?.Invoke(this, new WidgetLayoutChangedEventArgs(widget));
        }
    }

    public class WidgetLayoutChangedEventArgs : EventArgs
    {
        public DesktopWidgetTemplated Widget { get; }

        public WidgetLayoutChangedEventArgs(DesktopWidgetTemplated widget)
        {
            Widget = widget;
        }
    }
}