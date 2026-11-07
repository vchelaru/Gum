using Gum.Services;
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
using Gum.Plugins.InternalPlugins.EditorTab.Services;

namespace Gum.Wireframe
{
    // ResizeSide moved to Gum.Presentation (Tools/Gum.Presentation/Wireframe/ResizeSide.cs) —
    // it's pure data with no XNALIKE dependency, unlike ResizeHandles itself (draws LineRectangles),
    // and ResizeInputHandler (also moved) needs it. Same namespace, so no using changes needed here.

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
        readonly ICanvasDisplayScale _displayScale;
        bool _originDisplaysVisible = true;
        bool _showOrigin;
        bool _showXOffsetLine = true;
        bool _showYOffsetLine = true;

        readonly ResizeHandleLayout _layout;

        const float WidthAtNoZoom = ResizeHandleLayout.HandleSize;

        bool _visible = true;
        bool _widthEnabled = true;
        bool _heightEnabled = true;

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
                return _visible;
            }
            set
            {
                _visible = value;
                ApplyHandleVisibility();

                _originDisplaysVisible = value;
                ApplyOriginVisibility();
            }
        }

        /// <summary>
        /// Whether the origin marker draws while the handles are visible. Independent of which
        /// resize handles are available.
        /// </summary>
        public bool ShowOrigin
        {
            get => _showOrigin;
            set
            {
                _showOrigin = value;
                ApplyOriginVisibility();
            }
        }

        /// <summary>
        /// Whether the red X offset line and its dotted connector draw while the handles are visible.
        /// </summary>
        public bool ShowXOffsetLine
        {
            get => _showXOffsetLine;
            set
            {
                _showXOffsetLine = value;
                ApplyOriginVisibility();
            }
        }

        /// <summary>
        /// Whether the green Y offset line and its dotted connector draw while the handles are visible.
        /// </summary>
        public bool ShowYOffsetLine
        {
            get => _showYOffsetLine;
            set
            {
                _showYOffsetLine = value;
                ApplyOriginVisibility();
            }
        }

        private void ApplyOriginVisibility()
        {
            foreach (OriginDisplay display in _originDisplays)
            {
                ApplyOriginVisibility(display);
            }
        }

        private void ApplyOriginVisibility(OriginDisplay display)
        {
            display.ShowOriginMarker = _showOrigin;
            display.ShowXOffsetLine = _showXOffsetLine;
            display.ShowYOffsetLine = _showYOffsetLine;
            display.Visible = _originDisplaysVisible;
        }

        #endregion

        #region Methods

        public ResizeHandles(Layer layer, Color color, ICanvasDisplayScale displayScale)
        {
            _displayScale = displayScale;
            _layout = new ResizeHandleLayout(displayScale);
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

        /// <summary>
        /// Limits the drawn and hit-tested handles to those that can resize along an enabled axis
        /// (see <see cref="ResizeHandleLayout.IsSideAvailable"/>).
        /// </summary>
        public void SetAvailableAxes(bool widthEnabled, bool heightEnabled)
        {
            _widthEnabled = widthEnabled;
            _heightEnabled = heightEnabled;
            ApplyHandleVisibility();
        }

        private void ApplyHandleVisibility()
        {
            for (int i = 0; i < mHandles.Length; i++)
            {
                bool isShown = _visible && IsAvailable(i);
                mHandles[i].Visible = isShown;
                mInnerHandles[i].Visible = isShown;
            }
        }

        private bool IsAvailable(int handleIndex)
        {
            return ResizeHandleLayout.IsSideAvailable((ResizeSide)handleIndex, _widthEnabled, _heightEnabled);
        }

        public ResizeSide GetSideOver(float x, float y)
        {
            for (int i = 0; i < this.mHandles.Length; i++)
            {
                if (IsAvailable(i) && mHandles[i].HasCursorOver(x, y))
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

            ApplyOriginVisibility();
            UpdateToProperties();
        }

        /// <summary>
        /// Places the handles around a rectangle given directly instead of read from an object.
        /// </summary>
        /// <param name="x">World X of the rectangle's top-left corner.</param>
        /// <param name="y">World Y of the rectangle's top-left corner.</param>
        /// <param name="rotation">Absolute rotation in degrees, about the top-left corner.</param>
        public void SetBounds(float x, float y, float width, float height, float rotation)
        {
            mX = x;
            mY = y;
            mWidth = width;
            mHeight = height;
            mRotation = rotation;

            AdjustOriginDisplayCount(0);
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
                var first = ipsoList.First();

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

            // UpdateTo turns the origin lines on; reapply the handles' own origin visibility.
            ApplyOriginVisibility();
            UpdateToProperties();
        }


        public void UpdateHandleSizes()
        {
            var lineWidth = _displayScale.DisplayScale;
            foreach (var handle in this.mHandles)
            {
                handle.Width = _layout.GetHandleWorldSize(Renderer.Self.Camera.Zoom);
                handle.Height = handle.Width;
                handle.LinePixelWidth = lineWidth;
            }
            foreach(var innerHandle in mInnerHandles)
            {
                innerHandle.Width = _layout.GetInnerHandleWorldSize(Renderer.Self.Camera.Zoom);
                innerHandle.Height = innerHandle.Width;
                innerHandle.LinePixelWidth = lineWidth;
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
                var display = new OriginDisplay(_layer, _displayScale);
                display.SetColor(_color);
                ApplyOriginVisibility(display);
                _originDisplays.Add(display);
            }
        }

        private void UpdateToProperties()
        {
            var dim = _layout.GetHandleWorldSize(Renderer.Self.Camera.Zoom);

            // Laid out unrotated around the origin, then rotated and offset to X, Y below.
            for (int i = 0; i < mHandles.Length; i++)
            {
                var position = _layout.GetHandlePosition((ResizeSide)i, 0, 0, Width, Height, dim);
                mHandles[i].X = position.X;
                mHandles[i].Y = position.Y;
            }

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
                var inset = _layout.GetInnerHandleWorldInset(Renderer.Self.Camera.Zoom);
                innerHandlePosition += rotationMatrix.Right() * inset;
                innerHandlePosition += rotationMatrix.Up() * inset;
                innerHandle.Position.X = innerHandlePosition.X;
                innerHandle.Position.Y = innerHandlePosition.Y;

            }
        }

        #endregion
    }
}
