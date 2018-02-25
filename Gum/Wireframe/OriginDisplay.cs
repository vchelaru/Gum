using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    class OriginDisplay
    {
        const float RadiusAtNoZoom = 5;


        Line mXLine1;
        Line mXLine2;

        Line dottedRowOrColumnLine;

        Line mOriginLine;

        public bool Visible
        {
            get
            {
                return mOriginLine.Visible;
            }
            set
            {
                mXLine1.Visible = value;
                mXLine2.Visible = value;

                mOriginLine.Visible = value;
            }
        }

        public OriginDisplay(Layer layer)
        {

            mXLine1 = new Line(null);
            mXLine2 = new Line(null);
            mXLine1.Name = "Resize Handle X Line 1";
            mXLine2.Name = "Resize Handle X Line 2";

            ShapeManager.Self.Add(mXLine1, layer);
            ShapeManager.Self.Add(mXLine2, layer);

            mOriginLine = new Line(null);
            mOriginLine.Name = "Resize Handle Offset Line";
            ShapeManager.Self.Add(mOriginLine, layer);

        }

        public void UpdateTo(GraphicalUiElement asGue)
        {
            var parent = asGue.EffectiveParentGue;


            mOriginLine.Visible = true;

            // The child's position is relative
            // to the parent, but not always the
            // top left - depending on the XUnits
            // and YUnits. ParentOriginOffset contains
            // the point that the child is relative to, 
            // relative to the top-left of the parent.
            // In other words, if the child's XUnits is
            // PixelsFromRight, then the parentOriginOffsetX
            // will be the width of the parent.
            float parentOriginOffsetX;
            float parentOriginOffsetY;



            asGue.GetParentOffsets(out parentOriginOffsetX, out parentOriginOffsetY);
            
            // This currently has some confusing behavior:
            // If an object is part of a stacked parent, then
            // the offset line is correct if the parent is either
            // not wrapping, or if the object is not the first item
            // in a row/column. 
            // If the object is the first item in a row or column that
            // isn't the first, then the origin returned is the previous 
            // item's point, but the row/column index is added.

            float parentAbsoluteX = 0;
            float parentAbsoluteY = 0;

            if (parent != null)
            {
                parentAbsoluteX = parent.GetAbsoluteX();
                parentAbsoluteY = parent.GetAbsoluteY();

                if(parent.ChildrenLayout != Managers.ChildrenLayout.Regular &&
                    parent.WrapsChildren)
                {
                    // The origin may actually be a new row/column, so let's get that:
                    if(parent.ChildrenLayout == Managers.ChildrenLayout.LeftToRightStack)
                    {
                        for (int i = 0; i < asGue.StackedRowOrColumnIndex; i++)
                        {
                            parentAbsoluteY += parent.StackedRowOrColumnDimensions[i];
                        }
                    }
                    if (parent.ChildrenLayout == Managers.ChildrenLayout.TopToBottomStack)
                    {
                        for (int i = 0; i < asGue.StackedRowOrColumnIndex; i++)
                        {
                            parentAbsoluteX += parent.StackedRowOrColumnDimensions[i];
                        }
                    }
                }
            }

            mOriginLine.X = parentOriginOffsetX + parentAbsoluteX;
            mOriginLine.Y = parentOriginOffsetY + parentAbsoluteY;

            mOriginLine.RelativePoint.X = asGue.AbsoluteX - mOriginLine.X;
            mOriginLine.RelativePoint.Y = asGue.AbsoluteY - mOriginLine.Y;
        }


        public void SetOriginXPosition(GraphicalUiElement asGue)
        {
            float absoluteX = asGue.AbsoluteX;
            float absoluteY = asGue.AbsoluteY;

            IPositionedSizedObject asIpso = asGue;
            float zoom = Renderer.Self.Camera.Zoom;

            float offset = RadiusAtNoZoom * 1.5f / zoom;



            mXLine1.X = absoluteX - offset;
            mXLine1.Y = absoluteY - offset;

            mXLine2.X = absoluteX - offset;
            mXLine2.Y = absoluteY + offset;

            mXLine1.RelativePoint = new Microsoft.Xna.Framework.Vector2(offset * 2, offset * 2);
            mXLine2.RelativePoint = new Microsoft.Xna.Framework.Vector2(offset * 2, -offset * 2);
        }

    }
}
