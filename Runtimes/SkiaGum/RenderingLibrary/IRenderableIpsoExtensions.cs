using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary.Graphics
{
    public static class IRenderableIpsoExtensions
    {
        public static SKRect? GetEffectiveClipRect(this IRenderableIpso renderableIpso)
        {
            if (renderableIpso is InvisibleRenderable invisibleRenderable && invisibleRenderable.ClipsChildren)
            {
                var left = renderableIpso.GetAbsoluteX();
                var top = renderableIpso.GetAbsoluteY();
                var right = left + renderableIpso.Width;
                var bottom = top + renderableIpso.Height;
                return new SKRect(left, top, right, bottom);
            }
            else if (renderableIpso is BindableGue gue && gue.ClipsChildren)
            {
                var left = renderableIpso.GetAbsoluteX();
                var top = renderableIpso.GetAbsoluteY();
                var right = left + gue.GetAbsoluteWidth();
                var bottom = top + gue.GetAbsoluteHeight(); ;

                return new SKRect(left, top, right, bottom);
            }
            else
            {
                return renderableIpso.Parent?.GetEffectiveClipRect();
            }
        }
    }
}
