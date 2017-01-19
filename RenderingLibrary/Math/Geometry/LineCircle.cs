using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary.Math.Geometry;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace RenderingLibrary.Math.Geometry
{
    public enum CircleOrigin
    {
        Center,
        TopLeft
    }

    public class LineCircle : IVisible, IRenderableIpso
    {
        #region Fields
        float mRadius;
        LinePrimitive mLinePrimitive;

        IRenderableIpso mParent;

        bool mVisible;

        List<IRenderableIpso> mChildren;

        CircleOrigin mCircleOrigin;

        #endregion

        #region Properties

        public string Name
        {
            get;
            set;
        }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z
        {
            get;
            set;
        }

        public bool Visible
        {
            get { return mVisible; }
            set
            {
                mVisible = value;
            }
        }

        public float Radius
        {
            get
            {
                return mRadius;
            }
            set
            {
                mRadius = value;
                UpdatePoints();
            }
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

        public bool Wrap
        {
            get { return true; }
        }

        public CircleOrigin CircleOrigin
        {
            get
            {
                return mCircleOrigin;
            }
            set
            {
                mCircleOrigin = value;
                UpdatePoints();
            }
        }


        bool IRenderableIpso.ClipsChildren
        {
            get
            {
                return false;
            }
        }

        public float Rotation
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float Width
        {
            get
            {
                return Radius * 2;
            }
            set
            {
                Radius = value / 2;
            }
        }

        public float Height
        {
            get
            {
                return Radius * 2;
            }
            set
            {
                Radius = value / 2;
            }
        }

        #endregion

        #region Methods


        public LineCircle() : this(null)
        {

        }

        public LineCircle(SystemManagers managers)
        {

            mChildren = new List<IRenderableIpso>();

            mRadius = 32;
            Visible = true;

            if (managers != null)
            {
                mLinePrimitive = new LinePrimitive(managers.Renderer.SinglePixelTexture);
            }
            else
            {
                mLinePrimitive = new LinePrimitive(Renderer.Self.SinglePixelTexture);
            }

            UpdatePoints();


        }

        private void UpdatePoints()
        {

            mLinePrimitive.CreateCircle(Radius, 15);

            if(mCircleOrigin == Geometry.CircleOrigin.TopLeft)
            {
                mLinePrimitive.Shift(Radius, Radius);
            }
        }

        public bool HasCursorOver(float x, float y)
        {
            float radiusSquared = mRadius * mRadius;

            float distanceSquared = (x - mLinePrimitive.Position.X) * (distanceSquared = x - mLinePrimitive.Position.X) + 
                (y - mLinePrimitive.Position.Y) * (y - mLinePrimitive.Position.Y);
            return distanceSquared <= radiusSquared;
        }

        void IRenderable.Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
            if (AbsoluteVisible)
            {
                mLinePrimitive.Position.X = this.GetAbsoluteLeft();
                mLinePrimitive.Position.Y = this.GetAbsoluteTop();
                mLinePrimitive.Render(spriteRenderer, managers);
            }
        }
        #endregion


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

        public List<IRenderableIpso> Children
        {
            get { return mChildren; }
        }

        void IRenderableIpso.SetParentDirect(IRenderableIpso parent)
        {
            mParent = parent;
        }

        public object Tag { get; set; }

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
                return ((IRenderableIpso)this).Parent as IVisible;
            }
        }

        void IRenderable.PreRender() { }

    }
}
