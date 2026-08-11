using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Models.Helper;
using Path = System.IO.Path;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DataStats;

public partial class PlayData : UserControl
{
    private readonly VersionConfig _versionInfo;
    private Dictionary<DateTime, long> _dailyTotals;

    private static readonly SolidColorBrush ChartLineColor = new(Color.Parse("#4CAF50"));
    private static readonly SolidColorBrush ChartFillColor = new(Color.FromArgb(40, 76, 175, 80));
    private static readonly SolidColorBrush GridLineColor = new(Color.FromArgb(30, 128, 128, 128));
    private static readonly SolidColorBrush LabelColor = new(Color.FromArgb(153, 128, 128, 128));

    public PlayData()
    {
        InitializeComponent();
    }

    public PlayData(VersionConfig versionInfo) : this()
    {
        _versionInfo = versionInfo;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            var playerData = _versionInfo.PlayerData;
            var currentDir = _versionInfo.VersionPath;
            if (string.IsNullOrEmpty(currentDir)) return;

            var bedrockVersionsDir = System.IO.Path.GetDirectoryName(currentDir);
            var gameFolder = Path.GetDirectoryName(bedrockVersionsDir);
            if (string.IsNullOrEmpty(gameFolder)) return;

            var allConfigs = GameInfoHelper.GetVersionConfigs(gameFolder);
            if (allConfigs.Count == 0) return;

            // ========== 修正：按 TotalSessions 排序 ==========
            var sessionRank = allConfigs
                .Where(c => c?.PlayerData != null)
                .OrderByDescending(c => c.PlayerData.TotalPlayTime)
                .ToList();

            var currentSessionIdx = sessionRank.FindIndex(c => c.VersionPath == currentDir);
            if (currentSessionIdx >= 0)
                TotalPlayRankingLabel.Text = $"#{currentSessionIdx + 1}";
            // ================================================

            // 每周统计数据（用于图表展示，保留）
            var allWeeklyStats = new List<(string versionPath, int sessions, int activeDays, double totalHours)>();

            foreach (var config in allConfigs)
            {
                try
                {
                    var stats = SessionStoreHelper.GetWeeklyStats(config.VersionPath);
                    allWeeklyStats.Add((config.VersionPath, stats.Sessions, stats.ActiveDays, stats.TotalHours));
                }
                catch
                {
                }
            }

            // 周数据用于展示当前版本的周统计
            var (sessions, activeDays, totalHours) = SessionStoreHelper.GetWeeklyStats(_versionInfo.VersionPath);
            SessionsText.Text = sessions.ToString();
            ActiveDaysText.Text = activeDays.ToString();
            TotalHoursText.Text = $"{(playerData.TotalPlayTime / 3600.00).ToString("F2")}";

            _dailyTotals = SessionStoreHelper.GetDailyTotals(_versionInfo.VersionPath, 7);

            WeeklyChart.SizeChanged += OnChartSizeChanged;

            if (WeeklyChart.Bounds.Width > 0)
                DrawChart();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"加载游玩数据失败: {ex.Message}");
        }
    }

    private void OnChartSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        DrawChart();
    }

    private void DrawChart()
    {
        var canvas = WeeklyChart;
        if (canvas == null) return;

        canvas.Children.Clear();

        double width = canvas.Bounds.Width;
        double height = canvas.Bounds.Height;

        if (width <= 0 || height <= 0 || _dailyTotals == null || _dailyTotals.Count == 0)
            return;

        const double leftPadding = 50;
        const double rightPadding = 20;
        const double topPadding = 15;
        const double bottomPadding = 30;

        double chartWidth = width - leftPadding - rightPadding;
        double chartHeight = height - topPadding - bottomPadding;

        if (chartWidth <= 0 || chartHeight <= 0) return;

        var days = _dailyTotals.Keys.OrderBy(d => d).ToList();
        long maxValue = Math.Max(_dailyTotals.Values.Max(), 1);

        int gridCount = 4;
        for (int i = 0; i <= gridCount; i++)
        {
            double y = topPadding + chartHeight - (chartHeight * i / gridCount);

            var gridLine = new Line
            {
                StartPoint = new Point(leftPadding, y),
                EndPoint = new Point(leftPadding + chartWidth, y),
                Stroke = GridLineColor,
                StrokeThickness = 1
            };
            canvas.Children.Add(gridLine);

            long labelValue = maxValue * i / gridCount;
            string yLabel = FormatDuration(labelValue);

            var yText = new TextBlock
            {
                Text = yLabel,
                FontSize = 10,
                Foreground = LabelColor,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Canvas.SetLeft(yText, 2);
            Canvas.SetTop(yText, y - 7);
            canvas.Children.Add(yText);
        }

        int pointCount = days.Count;
        var dataPoints = new List<Point>();
        for (int i = 0; i < pointCount; i++)
        {
            double x = leftPadding + (chartWidth * i / Math.Max(pointCount - 1, 1));
            double value = _dailyTotals[days[i]];
            double y = topPadding + chartHeight - (chartHeight * value / maxValue);
            dataPoints.Add(new Point(x, y));
        }

        if (dataPoints.Count >= 2 && _dailyTotals.Values.Any(v => v > 0))
        {
            var linePoints = new List<Point> { new(dataPoints[0].X, topPadding + chartHeight) };
            linePoints.AddRange(dataPoints);
            linePoints.Add(new Point(dataPoints[^1].X, topPadding + chartHeight));

            var fillPolygon = new Polygon
            {
                Fill = ChartFillColor,
                Stroke = Brushes.Transparent
            };
            foreach (var pt in linePoints)
                fillPolygon.Points.Add(pt);
            canvas.Children.Add(fillPolygon);

            var polyline = new Polyline
            {
                Stroke = ChartLineColor,
                StrokeThickness = 2.5,
                StrokeLineCap = PenLineCap.Round,
                StrokeJoin = PenLineJoin.Round
            };
            foreach (var pt in dataPoints)
                polyline.Points.Add(pt);
            canvas.Children.Add(polyline);
        }

        foreach (var pt in dataPoints)
        {
            var dot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = ChartLineColor
            };
            Canvas.SetLeft(dot, pt.X - 4);
            Canvas.SetTop(dot, pt.Y - 4);
            canvas.Children.Add(dot);
        }

        for (int i = 0; i < dataPoints.Count; i++)
        {
            long value = _dailyTotals[days[i]];
            if (value <= 0) continue;

            var valText = new TextBlock
            {
                Text = FormatDuration(value),
                FontSize = 10,
                Foreground = ChartLineColor,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Canvas.SetLeft(valText, dataPoints[i].X - 12);
            Canvas.SetTop(valText, dataPoints[i].Y - 18);
            canvas.Children.Add(valText);
        }

        for (int i = 0; i < pointCount; i++)
        {
            double x = leftPadding + (chartWidth * i / Math.Max(pointCount - 1, 1));

            var dayText = new TextBlock
            {
                Text = days[i].ToString("MM/dd"),
                FontSize = 10,
                Foreground = LabelColor,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Canvas.SetLeft(dayText, x - 16);
            Canvas.SetTop(dayText, topPadding + chartHeight + 5);
            canvas.Children.Add(dayText);
        }
    }

    private static string FormatDuration(long seconds)
    {
        if (seconds >= 3600)
            return $"{seconds / 3600}h";
        if (seconds >= 60)
            return $"{seconds / 60}m";
        return $"{seconds}s";
    }
}