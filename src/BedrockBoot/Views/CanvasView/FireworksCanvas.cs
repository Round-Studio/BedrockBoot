using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace BedrockBoot.Views.CanvasView;

public class Particle
{
    public Point Position { get; set; }
    public Vector Velocity { get; set; }
    public Color Color { get; set; }
    public double Alpha { get; set; } = 1.0;
    public double Radius { get; set; } = 3.0;
    public double Decay { get; set; }
}

public class FireworksControl : Avalonia.Controls.Control
{
    private readonly List<Particle> _particles = new();
    private readonly Random _random = new();
    private readonly DispatcherTimer _timer;

    public FireworksControl()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _timer.Tick += (s, e) => UpdateAndRedraw();
        _timer.Start();

        PointerPressed += (s, e) =>
        {
            var point = e.GetPosition(this);
            SpawnFirework(point);
        };
    }

    public void SpawnFirework(Point origin)
    {
        int particleCount = _random.Next(60, 100);
        Color baseColor = Color.FromRgb(
            (byte)_random.Next(100, 256),
            (byte)_random.Next(100, 256),
            (byte)_random.Next(100, 256));

        for (int i = 0; i < particleCount; i++)
        {
            double angle = _random.NextDouble() * Math.PI * 2;
            double speed = _random.NextDouble() * 8 + 2;

            _particles.Add(new Particle
            {
                Position = origin,
                Velocity = new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed),
                Color = baseColor,
                Decay = _random.NextDouble() * 0.02 + 0.015,
                Radius = _random.NextDouble() * 2 + 2
            });
        }
    }

    private void UpdateAndRedraw()
    {
        double gravity = 0.15;
        double drag = 0.98;

        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Velocity = new Vector(p.Velocity.X * drag, p.Velocity.Y * drag + gravity);
            p.Position = new Point(p.Position.X + p.Velocity.X, p.Position.Y + p.Velocity.Y);
            p.Alpha -= p.Decay;

            if (p.Alpha <= 0)
            {
                _particles.RemoveAt(i);
            }
        }

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        foreach (var p in _particles)
        {
            byte a = (byte)(Math.Max(0, p.Alpha) * 255);
            var brush = new SolidColorBrush(Color.FromArgb(a, p.Color.R, p.Color.G, p.Color.B));
            context.DrawEllipse(brush, null, p.Position, p.Radius, p.Radius);
        }
    }
}