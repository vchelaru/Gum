using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RenderingLibrary.Graphics
{
    public class SolidRectangle : IPositionedSizedObject, IRenderable, IVisible
    {
        #region Fields
        
        Vector2 Position;
        IPositionedSizedObject mParent;

        List<IPositionedSizedObject> mChildren;
        public Color Color;

        #endregion

        #region Properties

        public bool Wrap
        {
            get { return false; }
        }

        public string Name
        {
            get;
            set;
        }
        public float X
        {
            get { return Position.X; }
            set { Position.X = value; }
        }

        public float Y
        {
            get { return Position.Y; }
            set { Position.Y = value; }
        }

        public float Z
        {
            get;
            set;
        }

        public float Width
        {
            get;
            set;
        }

        public float Height
        {
            get;
            set;
        }

        public IPositionedSizedObject Parent
        {
            get { return mParent; }
            set
            {
                if (mParent != value)
                {
                    if (mParent != null && mParent.Children != null)
                    {
                        mParent.Children.Remove(this);
                    }
                    mParent = value;
                    if (mParent != null && mParent.Children != null)
                    {
                        mParent.Children.Add(this);
                    }
                }
            }
        }

        public float Rotation { get; set; }

        public ICollection<IPositionedSizedObject> Children
        {
            get { return mChildren; }
        }

        public object Tag { get; set; }

        public BlendState BlendState
        {
            get { return BlendState.NonPremultiplied; }
        }


        public int Alpha
        {
            get
            {
                return Color.A;
            }
            set
            {
                Color.A = (byte)value;
            }
        }

        public int Red
        {
            get
            {
                return Color.R;
            }
            set
            {
                Color.R = (byte)value;
            }
        }

        public int Green
        {
            get
            {
                return Color.G;
            }
            set
            {
                Color.G = (byte)value;
            }
        }

        public int Blue
        {
            get
            {
                return Color.B;
            }
            set
            {
                Color.B = (byte)value;
            }
        }


        #endregion

        public SolidRectangle()
        {
            mChildren = new List<IPositionedSizedObject>();
            Color = Color.White;
            Visible = true;
        }


        void IRenderable.Render(SpriteBatch spriteBatch, SystemManagers managers)
        {
            if (this.AbsoluteVisible)
            {
                Renderer renderer = null;
                if (managers == null)
                {
                    renderer = Renderer.Self;
                }
                else
                {
                    renderer = managers.Renderer;
                }

                Sprite.Render(managers, spriteBatch, this,
                    renderer.SinglePixelTexture,
                    this.Color, null, false, false, Rotation);

            }
        }


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

        IVisible IVisible.Parent
        {
            get
            {
                return ((IPositionedSizedObject)this).Parent as IVisible;
            }
        }

        void IPositionedSizedObject.SetParentDirect(IPositionedSizedObject parent)
        {
            mParent = parent;
        }

        public bool Visible
        {
            get;
            set;
        }
    }
}
