using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RenderingLibrary.Graphics
{
    public class SortableLayer : Layer, IRenderable
    {
        public bool Wrap
        {
            get { return false; }
        }

#if MONOGAME
        public Microsoft.Xna.Framework.Graphics.BlendState BlendState
        {
            get { return Microsoft.Xna.Framework.Graphics.BlendState.NonPremultiplied; }
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
