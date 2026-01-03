using Gum;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Renderables;


public class LinePolygon : IRenderable
{
    public BlendState BlendState => throw new NotImplementedException();

    public bool Wrap => throw new NotImplementedException();

    public LinePolygon()
    : this(null)
    {
    }

    public LinePolygon(SystemManagers managers)
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
