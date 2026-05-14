using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Numerics;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;


public class LineRectangle : InvisibleRenderable
{
    public bool IsDotted { get; set; }

    /// <summary>Length of each dash (and gap) in pixels when <see cref="IsDotted"/>.</summary>
    public float DashLength { get; set; } = 2f;

    public float LinePixelWidth { get; set; } = 1f;

    public Color Color { get; set; } = Color.White;

    public override int Alpha
    {
        get => Color.A;
        set
        {
            if (value != Color.A)
            {
                Color = new Color(Color.R, Color.G, Color.B, (byte)value);
            }
        }
    }

    public int Red
    {
        get => Color.R;
        set
        {
            if (value != Color.R)
            {
                Color = new Color((byte)value, Color.G, Color.B, Color.A);
            }
        }
    }

    public int Green
    {
        get => Color.G;
        set
        {
            if (value != Color.G)
            {
                Color = new Color(Color.R, (byte)value, Color.B, Color.A);
            }
        }
    }

    public int Blue
    {
        get => Color.B;
        set
        {
            if (value != Color.B)
            {
                Color = new Color(Color.R, Color.G, (byte)value, Color.A);
            }
        }
    }

    public LineRectangle() : this(null) { }

    public LineRectangle(SystemManagers? _) { }

    public override void Render(ISystemManagers managers)
    {
        if (!Visible)
        {
            return;
        }


        int x = (int)this.GetAbsoluteLeft();
        int y = (int)this.GetAbsoluteTop();
        int width = (int)this.Width;
        int height = (int)this.Height;

        if (!IsDotted)
        {
            DrawRectangleLinesEx(new Rectangle(x, y, width, height), LinePixelWidth, Color);
        }
        else
        {
            DrawDashedSegment(x, y, x + width, y);
            DrawDashedSegment(x + width, y, x + width, y + height);
            DrawDashedSegment(x + width, y + height, x, y + height);
            DrawDashedSegment(x, y + height, x, y);
        }

    }

    private void DrawDashedSegment(float ax, float ay, float bx, float by)
    {
        var dx = bx - ax;
        var dy = by - ay;
        var len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 0.5f) return;

        var nx = dx / len;
        var ny = dy / len;
        var step = MathF.Max(DashLength, 1f);



        // Pattern: dash, gap, dash, gap — each segment is `step` long.
        for (float t = 0; t < len; t += step * 2)
        {
            var t2 = MathF.Min(t + step, len);
            DrawLineEx(
                new Vector2(ax + nx * t, ay + ny * t),
                new Vector2(ax + nx * t2, ay + ny * t2),
                LinePixelWidth,
                Color);
        }
    }
}
