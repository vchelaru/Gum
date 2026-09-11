using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EditorTabPlugin_XNA.Utilities;

public static class IpsoExtensions
{
    public static (float left, float top, float right, float bottom) GetBounds(this IRenderableIpso ipso)
    {
        var left = ipso.GetAbsoluteX();
        var top = ipso.GetAbsoluteY();
        var right = ipso.GetAbsoluteX() + ipso.Width;
        var bottom = ipso.GetAbsoluteY() + ipso.Height;


        return (left, top, right, bottom);
    }
}
