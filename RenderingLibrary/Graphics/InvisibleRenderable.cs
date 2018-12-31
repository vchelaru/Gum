using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public BlendState BlendState => Renderer.NormalBlendState;

        ObservableCollection<IRenderableIpso> children = new ObservableCollection<IRenderableIpso>();
        public ObservableCollection<IRenderableIpso> Children => children;

        // If a GUE uses this, it needs to support storing the values.
        public bool ClipsChildren { get; set; }

        float height;
        public float Height
        {
            get { return height; }
            set
            {
#if DEBUG
                if(float.IsPositiveInfinity(value))
                {
                    throw new ArgumentException();
                }
#endif
                height = value;
            }
        }

        public string Name { get; set; }

        IRenderableIpso mParent;
        public IRenderableIpso Parent
        {
            get { return mParent; }
            set
            {
                if (mParent != value)
                {
                    if (mParent != null)
                    {
                        mParent.Children.Remove(this);
                    }
                    mParent = value;
                    if (mParent != null)
                    {
                        mParent.Children.Add(this);
                    }
                }
            }
        }

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


        void IRenderableIpso.SetParentDirect(IRenderableIpso parent)
        {
            mParent = parent;
        }
    }
}
