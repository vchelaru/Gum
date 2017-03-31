using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace RenderingLibrary.Graphics
{
    public class InvisibleRenderable : IVisible, IRenderableIpso
    {
        public bool AbsoluteVisible
        {
            get
            {
                if (((IVisible)this).Parent == null)
                {
                    return Visible;
                }
                else
                {
                    return Visible && ((IVisible)this).Parent.AbsoluteVisible;
                }
            }
        }

        public BlendState BlendState => BlendState.AlphaBlend;

        List<IRenderableIpso> children = new List<IRenderableIpso>();
        public List<IRenderableIpso> Children => children;

        public bool ClipsChildren => false;

        public float Height { get; set; }

        public string Name { get; set; }

        public IRenderableIpso Parent { get; set; }

        public float Rotation { get; set; }

        public object Tag { get; set; }

        public bool Visible { get; set; }

        public float Width { get; set; }

        public bool Wrap => false;

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        IVisible IVisible.Parent { get { return Parent as IVisible; } }

        public void PreRender()
        {
        }

        public void Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
        }

        public void SetParentDirect(IRenderableIpso newParent)
        {
        }
    }
}
