using RenderingLibrary;
using RenderingLibrary.Graphics;
using SokolGum;

namespace Gum.Renderables;

/// <summary>
/// Filled rectangle renderable. Emits a single sgp_draw_filled_rect call
/// per Render, using the renderable's absolute screen position and size.
/// </summary>
public sealed class SolidRectangle : InvisibleRenderable
{
    public Color Color = Color.White;

    public int Red   { get => Color.R; set => Color.R = (byte)value; }
    public int Green { get => Color.G; set => Color.G = (byte)value; }
    public int Blue  { get => Color.B; set => Color.B = (byte)value; }

    public override void Render(ISystemManagers? managers)
    {
        if (!Visible) return;

        var x = this.GetAbsoluteLeft();
        var y = this.GetAbsoluteTop();
        var w = this.Width;
        var h = this.Height;

        // sgp_set_color takes floats 0..1; Alpha is on InvisibleRenderable (0..255).
        var a = (Color.A / 255f) * (Alpha / 255f);
        Sokol.SGP.sgp_set_color(Color.R / 255f, Color.G / 255f, Color.B / 255f, a);
        Sokol.SGP.sgp_draw_filled_rect(x, y, w, h);
    }
}
