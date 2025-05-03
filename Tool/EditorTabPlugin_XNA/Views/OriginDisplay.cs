﻿using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using ToolsUtilitiesStandard.Helpers;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;

namespace Gum.Wireframe
{
    public class OriginDisplay
    {
        const float RadiusAtNoZoom = 5;

        Line mXLine1;
        Line mXLine2;

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

        public void Destroy()
        {
            ShapeManager.Self.Remove(mXLine1);
            ShapeManager.Self.Remove(mXLine2);
            ShapeManager.Self.Remove(mOriginLine);
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

            var parentFlips = false;

            if (parent != null)
            {
                parentAbsoluteX = parent.GetAbsoluteX();
                parentAbsoluteY = parent.GetAbsoluteY();

                parentFlips = parent.GetAbsoluteFlipHorizontal();
                // Vic says - I believe this is all handled in asGue.GetParentOffsets above
                //if (parentFlips)
                //{
                //    var rotationMatrix = parent.GetAbsoluteRotationMatrix();
                //    Vector3 offset = new Vector3(parent.GetAbsoluteWidth(), 0, 0);
                //    offset = Vector3.Transform(offset, rotationMatrix);
                //    parentOriginOffsetX += offset.X;
                //    parentOriginOffsetY += offset.Y;
                //}


                if (parent.ChildrenLayout != Managers.ChildrenLayout.Regular &&
                    parent.WrapsChildren)
                {
                    // The origin may actually be a new row/column, so let's get that:
                    if (parent.ChildrenLayout == Managers.ChildrenLayout.LeftToRightStack)
                    {
                        for (int i = 0; i < asGue.StackedRowOrColumnIndex; i++)
                        {
                            parentAbsoluteY += parent.StackedRowOrColumnDimensions[i] + parent.StackSpacing;
                        }
                    }
                    if (parent.ChildrenLayout == Managers.ChildrenLayout.TopToBottomStack)
                    {
                        for (int i = 0; i < asGue.StackedRowOrColumnIndex; i++)
                        {
                            parentAbsoluteX += parent.StackedRowOrColumnDimensions[i] + parent.StackSpacing;
                        }
                    }
                }
            }

            var parentAbsoluteRotation = 0.0f;

            if (parent != null)
            {
                parentAbsoluteRotation = parent.GetAbsoluteRotation();
            }

            if (parentAbsoluteRotation == 0)
            {
                mOriginLine.X = parentOriginOffsetX + parentAbsoluteX;
                mOriginLine.Y = parentOriginOffsetY + parentAbsoluteY;

            }
            else
            {
                var matrix = Matrix.CreateRotationZ(-MathHelper.ToRadians(parentAbsoluteRotation));

                var rotatedVector = parentOriginOffsetX * matrix.Right() + parentOriginOffsetY * matrix.Up();


                mOriginLine.X = rotatedVector.X + parentAbsoluteX;
                mOriginLine.Y = rotatedVector.Y + parentAbsoluteY;
            }

            GetSelectedAbsoluteXAndY(asGue, parentFlips, out float selectedObjectX, out float selectedObjectY);

            mOriginLine.RelativePoint.X = selectedObjectX - mOriginLine.X;
            mOriginLine.RelativePoint.Y = selectedObjectY - mOriginLine.Y;
        }

        public void SetColor(Color color)
        {
            mXLine1.Color = color;
            mXLine2.Color = color;

            mOriginLine.Color = color;
        }

        private static void GetSelectedAbsoluteXAndY(GraphicalUiElement asGue, bool parentFlips, out float selectedObjectX, out float selectedObjectY)
        {
            selectedObjectX = asGue.AbsoluteX;
            selectedObjectY = asGue.AbsoluteY;

            if (parentFlips)
            {
                var rotationMatrix = asGue.GetAbsoluteRotationMatrix();
                Vector3 offset = new Vector3(asGue.GetAbsoluteWidth(), 0, 0);
                offset = Vector3.Transform(offset, rotationMatrix);
                selectedObjectX += offset.X;
                selectedObjectY += offset.Y;
            }
        }

        public void SetOriginXPosition(GraphicalUiElement asGue)
        {
            var parent = asGue.EffectiveParentGue;

            var parentFlips = false;

            if (parent != null)
            {
                parentFlips = parent.GetAbsoluteFlipHorizontal();
            }
             
            GetSelectedAbsoluteXAndY(asGue, parentFlips, out float selectedObjectX, out float selectedObjectY);


            IPositionedSizedObject asIpso = asGue;
            float zoom = Renderer.Self.Camera.Zoom;

            float offset = RadiusAtNoZoom * 1.5f / zoom;



            mXLine1.X = selectedObjectX - offset;
            mXLine1.Y = selectedObjectY - offset;

            mXLine2.X = selectedObjectX - offset;
            mXLine2.Y = selectedObjectY + offset;

            mXLine1.RelativePoint = new Vector2(offset * 2, offset * 2);
            mXLine2.RelativePoint = new Vector2(offset * 2, -offset * 2);
        }

    }
}
