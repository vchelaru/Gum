#if SKIA
using System;
#endif
using BlendState = Gum.BlendState;

namespace RenderingLibrary.Graphics
{
    public class SortableLayer : Layer, IRenderable
    {
        public bool Wrap
        {
            get { return false; }
        }

#if MONOGAME || XNA4
        public BlendState BlendState
        {
            get { return BlendState.NonPremultiplied; }
        }

        public void Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
            if (managers == null)
            {
                managers = SystemManagers.Default;
            }
            managers.Renderer.RenderLayer(managers, this);
        }
#endif
        void IRenderable.PreRender() { }

        public float Z
        {
            get;
            set;
        }


#if SKIA
        public void Render(SkiaSharp.SKCanvas canvas)
        {
            throw new NotImplementedException();
        }
#endif
    }
}
