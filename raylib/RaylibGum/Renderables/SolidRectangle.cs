using Raylib_cs;
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
        int x = (int)this.GetAbsoluteLeft();
        int y = (int)this.GetAbsoluteTop();
        int width = (int)this.Width;
        int height = (int)this.Height;

        Raylib.DrawRectangle(x,y, width, height, Color);
    }
}
