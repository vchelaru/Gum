using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.Wireframe
{
    class GraphicalOutline
    {
        #region Fields

        List<LineRectangle> mHighlightRectangles = new List<LineRectangle>();
        IPositionedSizedObject mHighlightedIpso;
        Layer mUiLayer;
        #endregion

        #region Properties

        public int SelectionBorder
        {
            get;
            set;
        }

        public IPositionedSizedObject HighlightedIpso
        {
            set
            {
                mHighlightedIpso = value;

                for (int i = 0; i < mHighlightRectangles.Count; i++)
                {
                    mHighlightRectangles[i].Visible = false;
                }

                if (mHighlightedIpso != null)
                {
                    UpdateLineRectanglesToHighlight();
                }
            }
        }

        private void UpdateLineRectanglesToHighlight()
        {
            SetLineRectangleAroundIpso(GetOrMakeAtIndex(0), mHighlightedIpso);


            if (mHighlightedIpso is NineSlice)
            {
                NineSlice nineSlice = mHighlightedIpso as NineSlice;

                float topHeight = 0;
                float centerHeight = 0;
                float bottomHeight = 0;

                float leftWidth = 0;
                float rightWidth = 0;
                float centerWidth = 0;

                if (nineSlice.TopTexture != null && nineSlice.BottomTexture != null && 
                    nineSlice.LeftTexture != null && nineSlice.RightTexture != null)
                {
                    topHeight = nineSlice.TopTexture.Height;

                    bottomHeight = nineSlice.BottomTexture.Height;

                    leftWidth = nineSlice.LeftTexture.Width;

                    rightWidth = nineSlice.RightTexture.Width;

                    centerHeight = nineSlice.Height - (topHeight + bottomHeight);
                    centerWidth = nineSlice.Width - (leftWidth + rightWidth);

                    LineRectangle tallRectangle = GetOrMakeAtIndex(1);
                    tallRectangle.Color = Color.Red;
                    tallRectangle.X = nineSlice.GetAbsoluteX() + leftWidth;
                    tallRectangle.Y = nineSlice.GetAbsoluteY();
                    tallRectangle.Width = centerWidth;
                    tallRectangle.Height = nineSlice.Height;

                    LineRectangle wideRectangle = GetOrMakeAtIndex(2);
                    wideRectangle.Color = Color.Red;
                    wideRectangle.X = nineSlice.GetAbsoluteX();
                    wideRectangle.Y = nineSlice.GetAbsoluteY() + topHeight;
                    wideRectangle.Width = nineSlice.Width;
                    wideRectangle.Height = centerHeight;


                }
            }
        }


        #endregion

        #region Methods

        public GraphicalOutline(Layer uiLayer)
        {
            SelectionBorder = 0;
            mUiLayer = uiLayer;
        }

        private void SetLineRectangleAroundIpso(LineRectangle rectangle, IPositionedSizedObject pso)
        {
            float adjustedSelectionBorder = SelectionBorder / Renderer.Self.Camera.Zoom;

            rectangle.Visible = true;
            rectangle.X = pso.GetAbsoluteX() - adjustedSelectionBorder;
            rectangle.Y = pso.GetAbsoluteY() - adjustedSelectionBorder;


            if ((pso.Width == 0 || pso.Height == 0) && pso is Sprite)
            {
                Sprite asSprite = pso as Sprite;

                rectangle.Width = asSprite.EffectiveWidth + adjustedSelectionBorder * 2;
                rectangle.Height = asSprite.EffectiveHeight + adjustedSelectionBorder * 2;
            }
            else
            {
                rectangle.Width = pso.Width + adjustedSelectionBorder * 2;
                rectangle.Height = pso.Height + adjustedSelectionBorder * 2;
            }
        }

        LineRectangle GetOrMakeAtIndex(int i)
        {
            if (i < mHighlightRectangles.Count)
            {
                mHighlightRectangles[i].Visible = true;
                return mHighlightRectangles[i];
            }
            else
            {

                LineRectangle newRect = new LineRectangle();
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
