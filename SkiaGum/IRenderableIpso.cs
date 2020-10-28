using SkiaGum.GueDeriving;
using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace RenderingLibrary.Graphics
{
    public interface IRenderableIpso : IRenderable, IPositionedSizedObject
    {
        //bool ClipsChildren { get; }
        IRenderableIpso Parent { get; set; }
        ObservableCollection<IRenderableIpso> Children { get; }
        void SetParentDirect(IRenderableIpso newParent);

    }

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
            else if(renderableIpso is BindableGraphicalUiElement gue && gue.ClipsChildren)
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
