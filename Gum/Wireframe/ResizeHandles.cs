using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary.Math.Geometry;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace Gum.Wireframe
{

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


    public class ResizeHandles
    {
        #region Fields

        float mX;
        float mY;
        float mWidth;
        float mHeight;

        LineCircle[] mHandles = new LineCircle[8];

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

            }
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

        public void SetValuesFrom(IPositionedSizedObject ipso)
        {
            this.mX = ipso.GetAbsoluteX();
            this.mY = ipso.GetAbsoluteY();
            this.mWidth = ipso.Width;
            this.mHeight = ipso.Height;
            UpdateToProperties();
        }

        public void SetValuesFrom(List<IPositionedSizedObject> ipsoList)
        {
            if (ipsoList.Count != 0)
            {
                float minX = ipsoList[0].GetAbsoluteX();
                float minY = ipsoList[0].GetAbsoluteY();
                float maxX = ipsoList[0].X + ipsoList[0].Width;
                float maxY = ipsoList[0].Y + ipsoList[0].Height;

                if (InputLibrary.Cursor.Self.PrimaryClick)
                {
                    int m = 3;
                }

                for (int i = 1; i < ipsoList.Count; i++)
                {
                    var item = ipsoList[i];

                    minX = Math.Min(minX, item.X);
                    minY = Math.Min(minY, item.Y);

                    maxX = Math.Max(maxX, item.X + item.Width);
                    maxY = Math.Max(maxY, item.Y + item.Height);
                }

                mX = minX;
                mY = minY;
                mWidth = maxX - minX;
                mHeight = maxY - minY;
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
            mHandles[0].X = X;
            mHandles[0].Y = Y;

            mHandles[1].X = X + Width/2.0f;
            mHandles[1].Y = Y;

            mHandles[2].X = X + Width;
            mHandles[2].Y = Y;

            mHandles[3].X = X + Width;
            mHandles[3].Y = Y + Height / 2.0f;

            mHandles[4].X = X + Width;
            mHandles[4].Y = Y + Height;

            mHandles[5].X = X + Width/2.0f;
            mHandles[5].Y = Y + Height;

            mHandles[6].X = X;
            mHandles[6].Y = Y + Height;

            mHandles[7].X = X;
            mHandles[7].Y = Y + Height/2.0f;

        }

        #endregion
    }
}
