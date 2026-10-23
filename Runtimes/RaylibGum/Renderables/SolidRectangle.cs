using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Raylib_cs.Raylib;


namespace Gum.Renderables;
public class SolidRectangle : InvisibleRenderable
{
    public int Red
    {
        get => Color.R;
        set => Color.R = (byte)value;
    }

    public int Green
    {
        get => Color.G;
        set => Color.G = (byte)value;
    }

    public int Blue
    {
        get => Color.B;
        set => Color.B = (byte)value;
    }

    public Color Color = Color.White;

    public override void Render(ISystemManagers managers)
    {
        if (!Visible) return;


        // Turned about its world position, counterclockwise for a positive rotation, at float
        // precision, like MonoGame's SolidRectangle and raylib's Sprite.
        var destination = new Raylib_cs.Rectangle(
            this.GetAbsoluteLeft(), this.GetAbsoluteTop(), this.Width, this.Height);

        DrawRectanglePro(destination, System.Numerics.Vector2.Zero, -this.GetAbsoluteRotation(), Color);
    }
}
