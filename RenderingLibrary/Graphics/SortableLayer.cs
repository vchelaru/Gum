using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RenderingLibrary.Graphics
{
    public class SortableLayer : Layer, IRenderable
    {
        public Microsoft.Xna.Framework.Graphics.BlendState BlendState
        {
            get { return Microsoft.Xna.Framework.Graphics.BlendState.NonPremultiplied; }
        }

        public bool Wrap
        {
            get { return false; }
        }

        public void Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
            if (managers == null)
            {
                managers = SystemManagers.Default;
            }
            managers.Renderer.RenderLayer(managers, this);
        }

        public float Z
        {
            get;
            set;
        }

        void IRenderable.PreRender() { }


    }
}
