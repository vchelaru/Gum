using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary.Math.Geometry;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Gum.Converters;
using Microsoft.Xna.Framework;

namespace Gum.Wireframe
{
    #region Enums

    public enum ResizeSide
    {
        None = -1,
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left
    }

    #endregion


    public class ResizeHandles
    {
        #region Fields

        float mX;
        float mY;
        float mWidth;
        float mHeight;

        float mRotation;

        LineCircle[] mHandles = new LineCircle[8];


        OriginDisplay originDisplay;


        const float RadiusAtNoZoom = 5;

        #endregion

        #region Properties

        public float X
        {
            get { return mX; }
            set
            {
                mX = value;
                UpdateToProperties();
            }
        }

        public float Y
        {
            get { return mY; }
            set
            {
                mY = value;
                UpdateToProperties();
            }
        }

        public float Width
        {
            get { return mWidth; }
            set
            {
                mWidth = value;
                UpdateToProperties();
            }
        }

        public float Height
        {
            get { return mHeight; }
            set
            {
                mHeight = value;
                UpdateToProperties();
            }
        }

        public bool Visible
        {
            get
            {
                return mHandles[0].Visible;
            }
            set
            {
                for (int i = 0; i < mHandles.Length; i++)
                {
                    mHandles[i].Visible = value;
                }

                originDisplay.Visible = value && ShowOrigin;

            }
        }

        public bool ShowOrigin
        {
            get;
            set;
        }

        #endregion

        #region Methods

        public ResizeHandles(Layer layer)
        {
            for (int i = 0; i < mHandles.Length; i++)
            {
                mHandles[i] = new LineCircle();
                mHandles[i].Radius = RadiusAtNoZoom;
                ShapeManager.Self.Add(mHandles[i], layer);

            }

            originDisplay = new OriginDisplay(layer);
            
            Visible = true;
            UpdateToProperties();
        }

        
        public ResizeSide GetSideOver(float x, float y)
        {
            for (int i = 0; i < this.mHandles.Length; i++)
            {
                if (mHandles[i].HasCursorOver(x, y))
                {
                    return (ResizeSide)i;
                }
            }

            return ResizeSide.None;
        }

        public void SetValuesFrom(IRenderableIpso ipso)
        {
            this.mX = ipso.GetAbsoluteX();
            this.mY = ipso.GetAbsoluteY();
            this.mWidth = ipso.Width;
            this.mHeight = ipso.Height;

            this.mRotation = ipso.Rotation;

            if (ipso is GraphicalUiElement)
            {
                var asGue = ipso as GraphicalUiElement;

                originDisplay.SetOriginXPosition(asGue);

                originDisplay.UpdateTo(asGue);
            }


            UpdateToProperties();
        }


        public void SetValuesFrom(IEnumerable<IRenderableIpso> ipsoList)
        {
            var count = ipsoList.Count();
            if(count == 1)
            {
                SetValuesFrom(ipsoList.First());
            }
            else if (ipsoList.Count() != 0)
            {
                var first = ipsoList.FirstOrDefault();

                float minX = first.GetAbsoluteX();
                float minY = first.GetAbsoluteY();
                float maxX = first.GetAbsoluteX() + first.Width;
                float maxY = first.GetAbsoluteY() + first.Height;

                foreach(var item in ipsoList)
                {
                    minX = Math.Min(minX, item.GetAbsoluteX());
                    minY = Math.Min(minY, item.GetAbsoluteY());

                    maxX = Math.Max(maxX, item.GetAbsoluteX() + item.Width);
                    maxY = Math.Max(maxY, item.GetAbsoluteY() + item.Height);
                }

                mX = minX;
                mY = minY;
                mWidth = maxX - minX;
                mHeight = maxY - minY;

                if (first is GraphicalUiElement)
                {
                    var asGue = first as GraphicalUiElement;

                    originDisplay.SetOriginXPosition(asGue);

                }



            }

            UpdateToProperties();


        }

        public void UpdateHandleRadius()
        {
            foreach (var circle in this.mHandles)
            {
                circle.Radius = RadiusAtNoZoom / Renderer.Self.Camera.Zoom;
            }
        }

        private void UpdateToProperties()
        {
            mHandles[0].X = 0;
            mHandles[0].Y = 0;

            float sin = (float)System.Math.Sin(mRotation);
            float cos = (float)System.Math.Cos(mRotation);

            mHandles[1].X = Width/2.0f;
            mHandles[1].Y = 0;

            mHandles[2].X = Width;
            mHandles[2].Y = 0;

            mHandles[3].X = Width;
            mHandles[3].Y = Height / 2.0f;

            mHandles[4].X = Width;
            mHandles[4].Y = Height;

            mHandles[5].X = Width/2.0f;
            mHandles[5].Y = Height;

            mHandles[6].X = 0;
            mHandles[6].Y = Height;

            mHandles[7].X = 0;
            mHandles[7].Y = Height/2.0f;

            Matrix rotationMatrix = Matrix.CreateRotationZ(-MathHelper.ToRadians( mRotation ));


            foreach(var handle in mHandles)
            {
                var xComponent = handle.X * rotationMatrix.Right;
                var yComponent = handle.Y * rotationMatrix.Up;

                handle.X = X + xComponent.X + yComponent.X;
                handle.Y = Y + xComponent.Y + yComponent.Y;
            }

        }

        #endregion
    }
}
