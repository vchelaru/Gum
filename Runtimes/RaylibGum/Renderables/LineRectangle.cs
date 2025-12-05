using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Renderables;


public class LineRectangle : IRenderable
{
    public BlendState BlendState => throw new NotImplementedException();

    public bool IsDotted { get; set; }

    public Color Color
    {
        get; set;
    }

    public bool Wrap => throw new NotImplementedException();

    public LineRectangle()
    : this(null)
    {
    }

    public LineRectangle(SystemManagers managers)
    {
        throw new NotImplementedException("This exists to satisfy the syntax matching MonoGame Gum. If you run into this, please report on discord " +
            "or consider submtiting a PR");
    }

    public void PreRender()
    {
        throw new NotImplementedException();
    }

    public void Render(ISystemManagers managers)
    {
        throw new NotImplementedException();
    }
}
