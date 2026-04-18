using System.Numerics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using static Sokol.SGP;
using SokolGum;

namespace Gum.Renderables;

/// <summary>
/// Single straight line segment from <c>(X, Y)</c> to
/// <c>(X, Y) + RelativePoint</c>. Exists for parity with the other Gum
/// backends — rarely instantiated from <c>.gumx</c>, more often used
/// programmatically for debug overlays.
/// </summary>
public sealed class Line : RenderableBase, IRenderableIpso
{
    public Vector2 RelativePoint { get; set; }

    public Color Color = Color.White;

    public int Red   { get => Color.R; set => Color.R = (byte)value; }
    public int Green { get => Color.G; set => Color.G = (byte)value; }
    public int Blue  { get => Color.B; set => Color.B = (byte)value; }
    public new int Alpha { get => Color.A; set => Color.A = (byte)value; }

    // RenderableBase explicitly implements IRenderableIpso.Alpha as 255;
    // our `new int Alpha` only shadows the regular member, so an
    // interface-cast caller (e.g. color-operation extensions that iterate
    // IRenderableIpso) would see 255 instead of the real color alpha.
    // Overriding the interface member keeps both paths in agreement.
    int IRenderableIpso.Alpha => Color.A;

    public Line() { }
    public Line(SystemManagers? _) { }

    public override void Render(ISystemManagers? managers)
    {
        if (!Visible) return;

        float ax = this.GetAbsoluteLeft();
        float ay = this.GetAbsoluteTop();
        float bx = ax + RelativePoint.X;
        float by = ay + RelativePoint.Y;

        sgp_set_color(Color.R / 255f, Color.G / 255f, Color.B / 255f, Color.A / 255f);
        sgp_draw_line(ax, ay, bx, by);
        sgp_reset_color();
    }
}
