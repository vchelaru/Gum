using System;
using System.Collections.Generic;
using System.Linq;
using RenderingLibrary.Math.Geometry;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using ToolsUtilitiesStandard.Helpers;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector3 = System.Numerics.Vector3;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;
using EditorTabPlugin_XNA.Utilities;

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

        LineRectangle[] mHandles = new LineRectangle[8];
        LineRectangle[] mInnerHandles = new LineRectangle[8];

        readonly List<OriginDisplay> _originDisplays = new List<OriginDisplay>();
        Layer _layer;
        Color _color;
        bool _originDisplaysVisible = true;

        const float WidthAtNoZoom = 12;

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
                    mInnerHandles[i].Visible = value;
                }

                _originDisplaysVisible = value;
                foreach (var display in _originDisplays)
                {
                    display.Visible = value && ShowOrigin;
                }
            }
        }

        public bool ShowOrigin
        {
            get;
            set;
        }

        #endregion

        #region Methods

        public ResizeHandles(Layer layer, Color color)
        {
            for (int i = 0; i < mHandles.Length; i++)
            {
                mHandles[i] = new LineRectangle();
                mHandles[i].IsDotted = false;
                mHandles[i].Width = WidthAtNoZoom;
                mHandles[i].Height = WidthAtNoZoom;
                ShapeManager.Self.Add(mHandles[i], layer);

                mInnerHandles[i] = new LineRectangle();
                mInnerHandles[i].IsDotted = false;
                mInnerHandles[i].Width = WidthAtNoZoom - 2;
                mInnerHandles[i].Height = WidthAtNoZoom - 2;
                mInnerHandles[i].Color = Color.Black;
                ShapeManager.Self.Add(mInnerHandles[i], layer);
            }

            _layer = layer;
            _color = color;
            Visible = true;
            UpdateToProperties();
        }

        public void Destroy()
        {
            for (int i = 0; i < mHandles.Length; i++)
            {
                ShapeManager.Self.Remove(mHandles[i]);
                ShapeManager.Self.Remove(mInnerHandles[i]);
            }

            foreach (var display in _originDisplays)
            {
                display.Destroy();
            }
            _originDisplays.Clear();
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
            //if(ipso is GraphicalUiElement gue && (gue.RenderableComponent as IRenderableIpso) != null)
            //{
            //    ipso = (gue.RenderableComponent as IRenderableIpso);
            //}

            var bounds = ipso.GetBounds();

            this.mX = bounds.left;
            this.mY = bounds.top;
            this.mWidth = bounds.right - bounds.left;
            this.mHeight = bounds.bottom - bounds.top;

            this.mRotation = ipso.GetAbsoluteRotation();

            if (ipso is GraphicalUiElement asGue)
            {
                AdjustOriginDisplayCount(1);
                _originDisplays[0].SetOriginXPosition(asGue);
                _originDisplays[0].UpdateTo(asGue);
            }
            else
            {
                AdjustOriginDisplayCount(0);
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

                var firstBounds = first.GetBounds();
                float minX = firstBounds.left;
                float minY = firstBounds.top;
                float maxX = firstBounds.right;
                float maxY = firstBounds.bottom;

                foreach(var item in ipsoList)
                {
                    var itemBounds = item.GetBounds();

                    minX = Math.Min(minX, itemBounds.left);
                    minY = Math.Min(minY, itemBounds.top);

                    maxX = Math.Max(maxX, itemBounds.right);
                    maxY = Math.Max(maxY, itemBounds.bottom);
                }

                mX = minX;
                mY = minY;
                mWidth = maxX - minX;
                mHeight = maxY - minY;

                var gues = ipsoList.OfType<GraphicalUiElement>().ToList();
                AdjustOriginDisplayCount(gues.Count);
                for (int i = 0; i < gues.Count; i++)
                {
                    _originDisplays[i].SetOriginXPosition(gues[i]);
                    _originDisplays[i].UpdateTo(gues[i]);
                }
            }

            UpdateToProperties();
        }


        public void UpdateHandleSizes()
        {
            foreach (var handle in this.mHandles)
            {
                handle.Width = WidthAtNoZoom / Renderer.Self.Camera.Zoom;
                handle.Height = WidthAtNoZoom / Renderer.Self.Camera.Zoom;
            }
            foreach(var innerHandle in mInnerHandles)
            {
                innerHandle.Width = (WidthAtNoZoom-2) / Renderer.Self.Camera.Zoom;
                innerHandle.Height = (WidthAtNoZoom-2) / Renderer.Self.Camera.Zoom;
            }
        }

        private void AdjustOriginDisplayCount(int count)
        {
            while (_originDisplays.Count > count)
            {
                _originDisplays[_originDisplays.Count - 1].Destroy();
                _originDisplays.RemoveAt(_originDisplays.Count - 1);
            }

            while (_originDisplays.Count < count)
            {
                var display = new OriginDisplay(_layer);
                display.SetColor(_color);
                display.Visible = _originDisplaysVisible && ShowOrigin;
                _originDisplays.Add(display);
            }
        }

        private void UpdateToProperties()
        {
            var dim = WidthAtNoZoom / Renderer.Self.Camera.Zoom;
            var halflDim = dim / 2.0f;

            mHandles[0].X = 0 - dim;
            mHandles[0].Y = 0 - dim;

            mHandles[1].X = Width / 2.0f - halflDim;
            mHandles[1].Y = 0 - dim;

            mHandles[2].X = Width;
            mHandles[2].Y = 0 - dim;

            mHandles[3].X = Width;
            mHandles[3].Y = Height / 2.0f - halflDim;

            mHandles[4].X = Width;
            mHandles[4].Y = Height;

            mHandles[5].X = Width / 2.0f - halflDim;
            mHandles[5].Y = Height;

            mHandles[6].X = 0 - dim;
            mHandles[6].Y = Height;

            mHandles[7].X = 0 - dim;
            mHandles[7].Y = Height / 2.0f - halflDim;

            Matrix rotationMatrix = Matrix.CreateRotationZ(-MathHelper.ToRadians( mRotation ));

            for(int i = 0; i < mHandles.Length; i++)
            {
                var handle = mHandles[i];
                var xComponent = handle.X * rotationMatrix.Right();
                var yComponent = handle.Y * rotationMatrix.Up();

                handle.X = X + xComponent.X + yComponent.X;
                handle.Y = Y + xComponent.Y + yComponent.Y;

                handle.Rotation = mRotation;

                var innerHandle = mInnerHandles[i];
                innerHandle.Rotation = mRotation;

                var innerHandlePosition = new Vector3( handle.Position, 0);
                // shift 1 pixel
                innerHandlePosition += rotationMatrix.Right() / Renderer.Self.Camera.Zoom;
                innerHandlePosition += rotationMatrix.Up() / Renderer.Self.Camera.Zoom;
                innerHandle.Position.X = innerHandlePosition.X;
                innerHandle.Position.Y = innerHandlePosition.Y;

            }
        }

        #endregion
    }
}
