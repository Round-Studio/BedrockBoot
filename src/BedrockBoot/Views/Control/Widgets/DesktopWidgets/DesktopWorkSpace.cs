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
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Interface;
using BedrockBoot.Style.Widgets;
using Octokit;

namespace BedrockBoot.Views.Control.Widgets.DesktopWidgets
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

        private Dictionary<Point, DesktopWidgetTemplated> _occupiedGridCells =
            new Dictionary<Point, DesktopWidgetTemplated>();

        private List<DesktopWidgetTemplated> _allWidgets = new List<DesktopWidgetTemplated>();

        private ContextMenu? _widgetContextMenu;
        private ContextMenu? _emptyContextMenu;
        private DesktopWidgetTemplated? _contextMenuWidget;

        public event EventHandler<WidgetLayoutChangedEventArgs>? LayoutChanged;
        public event EventHandler<WidgetDeletedEventArgs>? WidgetDeleted;
        public event EventHandler<WidgetAddedEventArgs>? WidgetAdded;
        public event EventHandler? AddWidgetCallOn;
        public static DesktopWorkspace Instance { get; private set; }

        public static List<WidgetRegisterInfo> RegistedWidgets { get; private set; } = new();

        public static void WidgetRegister(WidgetRegisterInfo info)
        {
            RegistedWidgets.Add(info);
        }

        public DesktopWorkspace()
        {
            Instance = this;
            this.Content = CreateLayout();
            this.AttachedToVisualTree += OnAttachedToVisualTree;
            this.DetachedFromVisualTree += OnDetachedFromVisualTree;
            InitializeContextMenus();
            UpdateCanvasSize();
        }

        private void InitializeContextMenus()
        {
            _widgetContextMenu = new ContextMenu();
            var deleteItem = new MenuItem
            {
                Header = "删除组件",
                Background = new SolidColorBrush(Colors.Transparent)
            };
            deleteItem.Click += OnDeleteWidgetClick;
            _widgetContextMenu.Items.Add(deleteItem);

            _emptyContextMenu = new ContextMenu();
            var addItem = new MenuItem
            {
                Header = "添加组件",
                Background = new SolidColorBrush(Colors.Transparent)
            };
            addItem.Click += ((sender, args) =>
            {
                AddWidgetCallOn?.Invoke(sender, args);
            });
            _emptyContextMenu.Items.Add(addItem);
        }

        private void OnDeleteWidgetClick(object? sender, EventArgs e)
        {
            if (_contextMenuWidget == null || _canvas == null) return;

            _contextMenuWidget.PointerPressed -= OnWidgetPointerPressed;
            _contextMenuWidget.PointerReleased -= OnWidgetPointerReleased;
            _contextMenuWidget.Resized -= OnWidgetResized;

            ClearWidgetOccupancy(_contextMenuWidget);
            _canvas.Children.Remove(_contextMenuWidget);
            _allWidgets.Remove(_contextMenuWidget);

            WidgetDeleted?.Invoke(this, new WidgetDeletedEventArgs(_contextMenuWidget));
            OnLayoutChanged(_contextMenuWidget);
            _contextMenuWidget = null;

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
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = new SolidColorBrush(Colors.Transparent)
                }
            };

            _canvas.PointerMoved += OnCanvasPointerMoved;
            _canvas.PointerPressed += OnCanvasPointerPressed;
            return _scrollViewer;
        }

        private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                _contextMenuWidget = null;
                _emptyContextMenu?.Open(_canvas);
                e.Handled = true;
            }
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

        public void AddWidget(WidgetType type)
        {
            if (_canvas == null) return;

            var widget = CreateWidgetFromType(type);
            if (widget == null) return;

            var startPos = FindNearestFreeGridPosition(new Point(0, 0), widget);
            PlaceWidgetAtGrid(widget, startPos);

            _canvas.Children.Add(widget);
            _allWidgets.Add(widget);

            widget.PointerPressed += OnWidgetPointerPressed;
            widget.PointerReleased += OnWidgetPointerReleased;
            widget.Resized += OnWidgetResized;
            widget.RightButtonPressed += OnWidgetRightButtonDown;

            UpdateCanvasSize();
            OnLayoutChanged(widget);
            WidgetAdded?.Invoke(this, new WidgetAddedEventArgs(widget, widget.WidgetConfig));
        }

        private void OnWidgetRightButtonDown(object? sender, PointerPressedEventArgs e)
        {
            if (sender is DesktopWidgetTemplated widget)
            {
                _contextMenuWidget = widget;
                _widgetContextMenu?.Open(widget);
                e.Handled = true;
            }
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
                    WidgetType = widget.WidgetConfig.WidgetType
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
                var widget = CreateWidgetFromType(data.WidgetType);
                if (widget == null) continue;

                SetWidgetSize(widget, data.Size);

                var gridPos = new Point(data.GridX, data.GridY);
                PlaceWidgetAtGrid(widget, gridPos);

                _canvas.Children.Add(widget);
                _allWidgets.Add(widget);

                widget.PointerPressed += OnWidgetPointerPressed;
                widget.PointerReleased += OnWidgetPointerReleased;
                widget.Resized += OnWidgetResized;
                widget.RightButtonPressed += OnWidgetRightButtonDown;
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
                widget.RightButtonPressed -= OnWidgetRightButtonDown;
                _canvas.Children.Remove(widget);
            }

            _allWidgets.Clear();
            _occupiedGridCells.Clear();
        }

        public static DesktopWidgetTemplated? CreateWidgetFromType(WidgetType type)
        {
            var config = new WidgetLayoutData()
            {
                WidgetType = type,
                Size = RegistedWidgets.Find(x => x.Type == type).DefaultSize
            };
            if (config == null) return null;

            var widget = new DesktopWidgetTemplated();

            var content = Activator.CreateInstance(RegistedWidgets.Find(x=>x.Type == config.WidgetType).WidgetTypeof);

            if (content == null) return null;

            widget.WidgetContent = (IWidgetTemplated)content;
            widget.WidgetConfig = config;
            SetWidgetSize(widget, config.Size);

            return widget;
        }

        public static void SetWidgetSize(DesktopWidgetTemplated widget, WidgetSize size)
        {
            var (width, height) = GetSizeDimensions(size);
            widget.Width = width;
            widget.Height = height;
        }

        public static (double width, double height) GetSizeDimensions(WidgetSize size)
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
            double width = widget.Bounds.Width > 0
                ? widget.Bounds.Width
                : (double.IsNaN(widget.Width) ? 180 : widget.Width);
            double height = widget.Bounds.Height > 0
                ? widget.Bounds.Height
                : (double.IsNaN(widget.Height) ? 180 : widget.Height);

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

        private Point? FindNearestFreeGridPositionByCenter(double centerX, double centerY, int widgetCols,
            int widgetRows)
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

                double width = widget.Bounds.Width > 0
                    ? widget.Bounds.Width
                    : (double.IsNaN(widget.Width) ? 180 : widget.Width);
                double height = widget.Bounds.Height > 0
                    ? widget.Bounds.Height
                    : (double.IsNaN(widget.Height) ? 180 : widget.Height);

                double rightEdge = left + width;
                double bottomEdge = top + height;

                if (rightEdge > maxWidth) maxWidth = rightEdge;
                if (bottomEdge > maxHeight) maxHeight = bottomEdge;
            }

            _canvas.Width = Math.Max(viewportWidth, maxWidth) - 2 * Padding;
            _canvas.Height = Math.Max(viewportHeight, maxHeight) - 2 * Padding;
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

    public class WidgetDeletedEventArgs : EventArgs
    {
        public DesktopWidgetTemplated Widget { get; }

        public WidgetDeletedEventArgs(DesktopWidgetTemplated widget)
        {
            Widget = widget;
        }
    }

    public class WidgetAddedEventArgs : EventArgs
    {
        public DesktopWidgetTemplated Widget { get; }
        public WidgetLayoutData Config { get; }

        public WidgetAddedEventArgs(DesktopWidgetTemplated widget, WidgetLayoutData config)
        {
            Widget = widget;
            Config = config;
        }
    }
}