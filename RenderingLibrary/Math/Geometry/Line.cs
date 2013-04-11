using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RenderingLibrary.Math.Geometry
{
    public class Line : IRenderable, IPositionedSizedObject
    {
        LinePrimitive mLinePrimitive;

        public Vector2 RelativePoint;


        IPositionedSizedObject mParent;

        List<IPositionedSizedObject> mChildren;

        public string Name
        {
            get;
            set;
        }

        public float X
        {
            get;
            set;
        }

        public float Y
        {
            get;
            set;
        }

        public float Z
        {
            get;
            set;
        }

        public bool Visible
        {
            get;
            set;
        }

        public Color Color
        {
            get
            {
                return mLinePrimitive.Color;
            }
            set
            {
                mLinePrimitive.Color = value;
            }
        }

        public BlendState BlendState
        {
            get { return BlendState.NonPremultiplied; }
        }


        public Line(SystemManagers managers)
        {
            Visible = true;
            if (managers != null)
            {
                mLinePrimitive = new LinePrimitive(managers.Renderer.SinglePixelTexture);
            }
            else
            {
                mLinePrimitive = new LinePrimitive(Renderer.Self.SinglePixelTexture);
            }

            mChildren = new List<IPositionedSizedObject>();
            UpdatePoints();
        }

        private void UpdatePoints()
        {
            while (mLinePrimitive.VectorCount < 2)
            {
                mLinePrimitive.Add(0, 0);
            }

            mLinePrimitive.Replace(1, this.RelativePoint);

            mLinePrimitive.Position.X = this.GetAbsoluteX();
            mLinePrimitive.Position.Y = this.GetAbsoluteY() ;
        }

        void IRenderable.Render(SpriteBatch spriteBatch, SystemManagers managers)
        {
            UpdatePoints();
            if (Visible)
            {
                mLinePrimitive.Render(spriteBatch, managers);
            }
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

        public ICollection<IPositionedSizedObject> Children
        {
            get { return mChildren; }
        }

        public object Tag
        {
            get;
            set;
        }
    }
}
