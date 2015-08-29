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

    public class LineCircle : IVisible, IRenderable, IPositionedSizedObject
    {
        #region Fields
        float mRadius;
        LinePrimitive mLinePrimitive;

        IPositionedSizedObject mParent;

        bool mVisible;

        List<IPositionedSizedObject> mChildren;

        CircleOrigin mCircleOrigin;

        #endregion

        #region Properties

        public string Name
        {
            get;
            set;
        }

        public float X
        {
            get
            {
                return mLinePrimitive.Position.X;
            }
            set
            {
                mLinePrimitive.Position.X = value;
            }
        }

        public float Y
        {
            get
            {
                return mLinePrimitive.Position.Y;
            }
            set
            {
                mLinePrimitive.Position.Y = value;
            }
        }

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

            mChildren = new List<IPositionedSizedObject>();

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

        void IRenderable.Render(SpriteBatch spriteBatch, SystemManagers managers)
        {
            if (AbsoluteVisible)
            {
                mLinePrimitive.Render(spriteBatch, managers);
            }
        }
        #endregion


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

        public List<IPositionedSizedObject> Children
        {
            get { return mChildren; }
        }

        void IPositionedSizedObject.SetParentDirect(IPositionedSizedObject parent)
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
                return ((IPositionedSizedObject)this).Parent as IVisible;
            }
        }
    }
}
