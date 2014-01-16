using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary.Math.Geometry;
using RenderingLibrary;
using RenderingLibrary.Graphics;

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

        LineCircle[] mHandles = new LineCircle[8];

        Line mXLine1;
        Line mXLine2;

        Line mOriginLine;



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

                mXLine1.Visible = value && ShowOrigin;
                mXLine2.Visible = value && ShowOrigin;

                mOriginLine.Visible = value && ShowOrigin;
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

            mXLine1 = new Line(null);
            mXLine2 = new Line(null);
            mXLine1.Name = "Resize Handle X Line 1";
            mXLine2.Name = "Resize Handle X Line 2";

            ShapeManager.Self.Add(mXLine1, layer);
            ShapeManager.Self.Add(mXLine2, layer);

            mOriginLine = new Line(null);
            mOriginLine.Name = "Resize Handle Offset Line";
            ShapeManager.Self.Add(mOriginLine, layer);


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

            if (ipso is GraphicalUiElement)
            {
                var asGue = ipso as GraphicalUiElement;

                SetOriginXPosition(asGue);
            }


            UpdateToProperties();
        }

        private void SetOriginXPosition(GraphicalUiElement asGue)
        {
            IPositionedSizedObject asIpso = asGue;
            float zoom = Renderer.Self.Camera.Zoom;

            float offset = RadiusAtNoZoom * 1.5f / zoom;



            float absoluteX = asGue.AbsoluteX;
            float absoluteY = asGue.AbsoluteY;

            mXLine1.X = absoluteX - offset;
            mXLine1.Y = absoluteY - offset;

            mXLine2.X = absoluteX - offset;
            mXLine2.Y = absoluteY + offset;

            mXLine1.RelativePoint = new Microsoft.Xna.Framework.Vector2(offset * 2, offset * 2);
            mXLine2.RelativePoint = new Microsoft.Xna.Framework.Vector2(offset * 2, -offset * 2);



            bool shouldShowOffsetLine = asGue.Parent != null && asGue.Parent is GraphicalUiElement;

            mOriginLine.Visible = shouldShowOffsetLine;

            if (shouldShowOffsetLine)
            {
                var gueParent = asGue.Parent as GraphicalUiElement;
                mOriginLine.X = asGue.Parent.GetAbsoluteX();
                mOriginLine.Y = asGue.Parent.GetAbsoluteY();

                switch (asGue.XUnits)
                {
                    case GeneralUnitType.PixelsFromMiddle:
                        mOriginLine.X += asIpso.Width/2;
                        break;
                    case GeneralUnitType.PixelsFromLarge:
                        mOriginLine.X += asIpso.Width;
                        break;
                }

                switch (asGue.YUnits)
                {
                    case GeneralUnitType.PixelsFromMiddle:
                        mOriginLine.Y += asIpso.Height / 2;
                        break;
                    case GeneralUnitType.PixelsFromLarge:
                        mOriginLine.Y += asIpso.Height;
                        break;
                }

                mOriginLine.RelativePoint.X = asGue.AbsoluteX - mOriginLine.X;
                mOriginLine.RelativePoint.Y = asGue.AbsoluteY - mOriginLine.Y;
            }



        }

        public void SetValuesFrom(List<IPositionedSizedObject> ipsoList)
        {
            if (ipsoList.Count != 0)
            {
                float minX = ipsoList[0].GetAbsoluteX();
                float minY = ipsoList[0].GetAbsoluteY();
                float maxX = ipsoList[0].GetAbsoluteX() + ipsoList[0].Width;
                float maxY = ipsoList[0].GetAbsoluteY() + ipsoList[0].Height;

                if (InputLibrary.Cursor.Self.PrimaryClick)
                {
                    int m = 3;
                }

                for (int i = 1; i < ipsoList.Count; i++)
                {
                    var item = ipsoList[i];

                    minX = Math.Min(minX, item.GetAbsoluteX());
                    minY = Math.Min(minY, item.GetAbsoluteY());

                    maxX = Math.Max(maxX, item.GetAbsoluteX() + item.Width);
                    maxY = Math.Max(maxY, item.GetAbsoluteY() + item.Height);
                }

                mX = minX;
                mY = minY;
                mWidth = maxX - minX;
                mHeight = maxY - minY;

                if (ipsoList[0] is GraphicalUiElement)
                {
                    var asGue = ipsoList[0] as GraphicalUiElement;

                    SetOriginXPosition(asGue);

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
