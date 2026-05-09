using RenderingLibrary;
using RenderingLibrary.Graphics;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;

/// <summary>Where the (X, Y) coordinate refers to on a <see cref="LineCircle"/>.</summary>
public enum CircleOrigin
{
    /// <summary>(X, Y) is the centre point.</summary>
    Center,
    /// <summary>(X, Y) is the top-left of the circle's bounding box — centre is (X + Radius, Y + Radius).</summary>
    TopLeft,
}

/// <summary>Stroked circle outline rendered using Raylib's DrawCircleLines.</summary>
public class LineCircle : InvisibleRenderable
{
    public CircleOrigin CircleOrigin { get; set; }
    public float Radius { get; set; }

    public Color Color { get; set; } = Color.White;

    public int Alpha
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

    public LineCircle() : this(null) { }

    public LineCircle(SystemManagers? _) { }

    public override void Render(ISystemManagers managers)
    {
        if (!Visible)
        {
            return;
        }

        float cx = this.GetAbsoluteLeft();
        float cy = this.GetAbsoluteTop();
        if (CircleOrigin == CircleOrigin.TopLeft)
        {
            cx += Radius;
            cy += Radius;
        }

        DrawCircleLines((int)cx, (int)cy, Radius, Color);
    }
}
