using EditorTabPlugin_XNA.Utilities;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Numerics;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;

namespace Gum.Wireframe
{
    public class GraphicalOutline
    {
        #region Fields

        List<LineRectangle> mHighlightRectangles = new List<LineRectangle>();
        GraphicalUiElement mHighlightedIpso;
        Layer mUiLayer;
        #endregion

        #region Properties

        public int SelectionBorder
        {
            get;
            set;
        }

        public GraphicalUiElement HighlightedIpso
        {
            set
            {
                if (value != mHighlightedIpso)
                {
                    mHighlightedIpso = value;

                    for (int i = 0; i < mHighlightRectangles.Count; i++)
                    {
                        mHighlightRectangles[i].Visible = false;
                    }

                    // This is now called by the caller (SelectionManager) every frame so that it refreshes immediately on a nudge
                     // Update Dec 2, 2024 - Update this immediately in case this is called by some external object like the treeview
                     // on highlight
                    if (mHighlightedIpso != null && mHighlightedIpso.Component != null &&
                        // We don't want to show the rectangle around line polygons, they don't fit inside a rectangle
                        (mHighlightedIpso.RenderableComponent) is LinePolygon == false)
                    {
                        UpdateHighlightElements();
                    }
                }
            }
        }


        #endregion

        #region Methods

        public GraphicalOutline(Layer uiLayer)
        {
            SelectionBorder = 2;
            mUiLayer = uiLayer;
        }

        public void UpdateHighlightElements()
        {
            SetLineRectangleAroundIpso(GetOrMakeRectangleAtIndex(0), mHighlightedIpso);

            if (mHighlightedIpso.Component is NineSlice)
            {
                CreateNineSliceSplitLines();
            }
        }

        private void CreateNineSliceSplitLines()
        {
            NineSlice nineSlice = mHighlightedIpso.Component as NineSlice;

            float topHeight = 0;
            float centerHeight = 0;
            float bottomHeight = 0;

            float leftWidth = 0;
            float rightWidth = 0;
            float centerWidth = 0;

            if (nineSlice.TopTexture != null && nineSlice.BottomTexture != null &&
                nineSlice.LeftTexture != null && nineSlice.RightTexture != null)
            {
                topHeight = nineSlice.OutsideSpriteHeight;

                bottomHeight = nineSlice.OutsideSpriteHeight;

                leftWidth = nineSlice.OutsideSpriteWidth;

                rightWidth = nineSlice.OutsideSpriteWidth;

                centerHeight = nineSlice.Height - (topHeight + bottomHeight);
                centerWidth = nineSlice.Width - (leftWidth + rightWidth);

                float offsetX = leftWidth;
                float offsetY = 0;

                //Vector3 right;
                //Vector3 up;

                //if (nineSlice.Rotation == 0)
                //{
                //    right = Vector3.Right;
                //    up = Vector3.Up;
                //}
                //else
                //{
                //    var matrix = Matrix.CreateRotationZ(-MathHelper.ToRadians(nineSlice.Rotation));

                //    right = matrix.Right;
                //    up = matrix.Up;
                //}

                LineRectangle tallRectangle = GetOrMakeRectangleAtIndex(1);
                tallRectangle.Color = Color.Red;
                tallRectangle.X = nineSlice.GetAbsoluteX() + offsetX ;
                tallRectangle.Y = nineSlice.GetAbsoluteY() ;
                tallRectangle.Width = centerWidth;
                tallRectangle.Height = nineSlice.Height;
                tallRectangle.Rotation = nineSlice.GetAbsoluteRotation();


                offsetX = 0;
                offsetY = topHeight;

                LineRectangle wideRectangle = GetOrMakeRectangleAtIndex(2);
                wideRectangle.Color = Color.Red;
                wideRectangle.X = nineSlice.GetAbsoluteX() ;
                wideRectangle.Y = nineSlice.GetAbsoluteY() + offsetY ;
                wideRectangle.Width = nineSlice.Width;
                wideRectangle.Height = centerHeight;
                wideRectangle.Rotation = nineSlice.GetAbsoluteRotation();
            }
        }

        private void SetLineRectangleAroundIpso(LineRectangle rectangle, IRenderableIpso pso)
        {
            float adjustedSelectionBorder = SelectionBorder / Renderer.Self.Camera.Zoom;

            rectangle.Visible = true;

            float left, top, width, height;
            GetDimensions(pso, out left, out top, out width, out height);

            rectangle.X = left - adjustedSelectionBorder;
            rectangle.Y = top - adjustedSelectionBorder;

            rectangle.Width = width + adjustedSelectionBorder * 2;
            rectangle.Height = height + adjustedSelectionBorder * 2;

            rectangle.Rotation = pso.GetAbsoluteRotation();
        }

        private static void GetDimensions(IRenderableIpso pso, out float left, out float top, out float width, out float height)
        {
            var bounds = pso.GetBounds();

            left = bounds.left;
            top = bounds.top;


            float right = left;
            float bottom = top;

            if(pso is GraphicalUiElement gue && gue.RenderableComponent is LinePolygon linePolygon)
            {
                var absolutePosition = new Vector2(left, top);

                for(int i = 0; i < linePolygon.PointCount; i++)
                {
                    var absolutePoint = linePolygon.PointAt(i) + absolutePosition;

                    left = Math.Min(left, absolutePoint.X);
                    top = Math.Min(top, absolutePoint.Y);

                    right = Math.Max(right, absolutePoint.X);
                    bottom = Math.Max(bottom, absolutePoint.Y);
                }
            }
            else
            {
                right = bounds.right;
                bottom = bounds.bottom;
            }

            width = right - left;
            height = bottom - top;
        }

        LineRectangle GetOrMakeRectangleAtIndex(int i)
        {
            if (i < mHighlightRectangles.Count)
            {
                mHighlightRectangles[i].Visible = true;
                return mHighlightRectangles[i];
            }
            else
            {

                LineRectangle newRect = new LineRectangle();
                newRect.IsDotted = true;
                newRect.Name = "Highlight Rectangle";
                newRect.Color = Color.Yellow;
                newRect.Visible = true;
                mHighlightRectangles.Add(newRect);
                ShapeManager.Self.Add(newRect, mUiLayer);

                return newRect;
            }
        }

        #endregion

    }
}
