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
    public class LineCircle : IRenderable
    {
        #region Fields
        float mRadius;
        LinePrimitive mLinePrimitive;

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
            get;
            set;
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
        #endregion

        #region Methods


        public LineCircle() : this(null)
        {

        }

        public LineCircle(SystemManagers managers)
        {
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
            if (Visible)
            {
                mLinePrimitive.Render(spriteBatch, managers);
            }
        }
        #endregion
    }
}
