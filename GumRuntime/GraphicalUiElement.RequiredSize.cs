using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.RenderingLibrary;
using GumDataTypes.Variables;

using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.ComponentModel;
using ToolsUtilitiesStandard.Helpers;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using GumRuntime;
using Gum.Collections;

#if FRB

using InteractiveGue = Gum.Wireframe.GraphicalUiElement;
#endif


#if !FRB
using Gum.StateAnimation.Runtime;
#endif

namespace Gum.Wireframe;

public partial class GraphicalUiElement
{

    float GetRequiredParentWidth()
    {
        var effectiveParent = this.EffectiveParentGue;
        if (effectiveParent != null && effectiveParent.ChildrenLayout == ChildrenLayout.TopToBottomStack && effectiveParent.WrapsChildren)
        {
            // The column needs what this child contributes to it, measured from the column's start.
            return _wrappedLineStart + GetStackedLineDimension(effectiveParent);
        }
        else if (effectiveParent != null && effectiveParent.ChildrenLayout == ChildrenLayout.LeftToRightStack
            && mXUnits != GeneralUnitType.PixelsFromSmall)
        {
            // The parent owns this child's X position via stacking, so a non-PixelsFromSmall
            // XUnits/XOrigin must not feed back into the parent's width measure. Without this
            // branch, a stacked child with XUnits = PixelsFromMiddle (or PixelsFromLarge) would
            // inflate the parent's RelativeToChildren width through the GetDimensionFromEdges
            // path below, which doubles centered widths and clamps right-anchored widths against
            // the parent's right edge (#2896). A PixelsFromSmall X offset, by contrast, is a real
            // additive nudge the stack applies on top of the stacked position, so it is allowed to
            // fall through to the edge path below and grow the parent's required width.
            var asIpso = this as IPositionedSizedObject;
            return asIpso.Width;
        }
        else
        {
            return GetRequiredParentWidthFromEdges();
        }
    }

    // The width a parent sized to its children needs for this child, from its X units, X and origin.
    // Portions positioned outside the parent are not counted.
    float GetRequiredParentWidthFromEdges()
    {
        float positionValue = mX;

        // This GUE hasn't been set yet so it can't give
        // valid widths/heights
        if (this.mContainedObjectAsIpso == null)
        {
            return 0;
        }
        float smallEdge = positionValue;
        if (mXOrigin == HorizontalAlignment.Center)
        {
            smallEdge = positionValue - ((IPositionedSizedObject)this).Width / 2.0f;
        }
        else if (mXOrigin == HorizontalAlignment.Right)
        {
            smallEdge = positionValue - ((IPositionedSizedObject)this).Width;
        }

        float bigEdge = positionValue;
        if (mXOrigin == HorizontalAlignment.Center)
        {
            bigEdge = positionValue + ((IPositionedSizedObject)this).Width / 2.0f;
        }
        if (mXOrigin == HorizontalAlignment.Left)
        {
            bigEdge = positionValue + ((IPositionedSizedObject)this).Width;
        }

        var units = mXUnits;

        float dimensionToReturn = GetDimensionFromEdges(smallEdge, bigEdge, units);

        return dimensionToReturn;
    }

    float GetRequiredParentHeight()
    {
        var effectiveParent = this.EffectiveParentGue;
        if (effectiveParent != null && effectiveParent.ChildrenLayout == ChildrenLayout.LeftToRightStack && effectiveParent.WrapsChildren)
        {
            // The row needs what this child contributes to it, measured from the row's start.
            return _wrappedLineStart + GetStackedLineDimension(effectiveParent);
        }
        else if (effectiveParent != null && effectiveParent.ChildrenLayout == ChildrenLayout.TopToBottomStack
            && mYUnits != GeneralUnitType.PixelsFromSmall)
        {
            // Symmetric to GetRequiredParentWidth's LeftToRightStack branch: the parent owns this
            // child's Y position via stacking, so a non-PixelsFromSmall YUnits/YOrigin must not
            // feed back into the parent's height measure (#2896 - PixelsFromMiddle doubles and
            // PixelsFromLarge clamps through GetDimensionFromEdges). A PixelsFromSmall Y offset is
            // a real downward nudge the stack applies on top of the stacked position, so it falls
            // through to the edge path below and grows the parent's required height. This is what
            // makes a RelativeToChildren stack contain a first child placed at e.g. Y = 12.
            var asIpso = this as IPositionedSizedObject;
            return asIpso.Height;
        }
        else
        {
            return GetRequiredParentHeightFromEdges();
        }
    }

    // The height a parent sized to its children needs for this child, from its Y units, Y and origin.
    // Portions positioned outside the parent are not counted.
    float GetRequiredParentHeightFromEdges()
    {
        var units = mYUnits;
        float positionValue = mY;
#pragma warning disable CS0618 // PixelsFromMiddleInverted is obsolete but still loads from older projects
        if (units == GeneralUnitType.PixelsFromMiddleInverted)
#pragma warning restore CS0618
        {
            // Inverted Y positions upward, so both edges are measured from the negated value.
            positionValue = -mY;
        }

        // This GUE hasn't been set yet so it can't give
        // valid widths/heights
        if (this.mContainedObjectAsIpso == null)
        {
            return 0;
        }
        float smallEdge = positionValue;

        if (mYOrigin == VerticalAlignment.Center)
        {
            smallEdge = positionValue - ((IPositionedSizedObject)this).Height / 2.0f;
        }
        else if (mYOrigin == VerticalAlignment.TextBaseline)
        {
            if (mContainedObjectAsIpso is IText text)
            {
                smallEdge = positionValue - ((IPositionedSizedObject)this).Height + text.DescenderHeight * text.FontScale;
            }
            else
            {
                smallEdge = positionValue - ((IPositionedSizedObject)this).Height;
            }
        }
        else if (mYOrigin == VerticalAlignment.Bottom)
        {
            smallEdge = positionValue - ((IPositionedSizedObject)this).Height;
        }

        float bigEdge = positionValue;
        if (mYOrigin == VerticalAlignment.Center)
        {
            bigEdge = positionValue + ((IPositionedSizedObject)this).Height / 2.0f;
        }
        if (mYOrigin == VerticalAlignment.Top)
        {
            bigEdge = positionValue + ((IPositionedSizedObject)this).Height;
        }

        float dimensionToReturn = GetDimensionFromEdges(smallEdge, bigEdge, units);

        return dimensionToReturn;
    }

    private void GetParentLayoutInputs(out float parentWidth, out float parentHeight, out float parentAbsoluteRotation,
        out bool isParentFlippedHorizontally)
    {
        GetParentDimensions(out parentWidth, out parentHeight);

        parentAbsoluteRotation = 0;
        isParentFlippedHorizontally = false;
        if (this.Parent != null)
        {
            parentAbsoluteRotation = this.Parent.GetAbsoluteRotation();
            isParentFlippedHorizontally = Parent.GetAbsoluteFlipHorizontal();
        }
        else if (this.ElementGueContainingThis != null && this.ElementGueContainingThis.mContainedObjectAsIpso != null)
        {
            parentWidth = this.ElementGueContainingThis.mContainedObjectAsIpso.Width;
            parentHeight = this.ElementGueContainingThis.mContainedObjectAsIpso.Height;

            parentAbsoluteRotation = this.ElementGueContainingThis.GetAbsoluteRotation();
        }
    }

    private void GetParentDimensions(out float parentWidth, out float parentHeight)
    {
        parentWidth = CanvasWidth;
        parentHeight = CanvasHeight;

        // I think we want to obey the non GUE parent first if it exists, then the GUE
        //if (this.ParentGue != null && this.ParentGue.mContainedObjectAsRenderable != null)
        //{
        //    parentWidth = this.ParentGue.mContainedObjectAsIpso.Width;
        //    parentHeight = this.ParentGue.mContainedObjectAsIpso.Height;
        //}
        //else if (this.Parent != null)
        //{
        //    parentWidth = Parent.Width;
        //    parentHeight = Parent.Height;
        //}

        if (this.Parent != null)
        {
            if (Parent.ChildrenLayout == ChildrenLayout.AutoGridVertical || Parent.ChildrenLayout == ChildrenLayout.AutoGridHorizontal)
            {
                var effectiveHorizontalCells = Parent.AutoGridHorizontalCells;
                if (effectiveHorizontalCells < 1) effectiveHorizontalCells = 1;
                var effectiveVerticalCells = Parent.AutoGridVerticalCells;
                if (effectiveVerticalCells < 1) effectiveVerticalCells = 1;

                var setCellCount = effectiveHorizontalCells * effectiveVerticalCells;

                if (Parent.GetVisibleChildCount() > setCellCount)
                {
                    // Matches GetCellDimensions: a horizontal grid fixes its columns and grows rows,
                    // a vertical grid fixes its rows and grows columns.
                    if (Parent.ChildrenLayout == ChildrenLayout.AutoGridHorizontal)
                    {
                        if (Parent.HeightUnits == DimensionUnitType.RelativeToChildren)
                        {
                            effectiveVerticalCells = (int)System.Math.Ceiling((float)Parent.GetVisibleChildCount() / effectiveHorizontalCells);
                        }
                    }
                    else
                    {
                        if (Parent.WidthUnits == DimensionUnitType.RelativeToChildren)
                        {
                            effectiveHorizontalCells = (int)System.Math.Ceiling((float)Parent.GetVisibleChildCount() / effectiveVerticalCells);
                        }
                    }
                }

                parentWidth = (Parent.AbsoluteWidth - (effectiveHorizontalCells - 1) * Parent.StackSpacing) / effectiveHorizontalCells;

                parentHeight = ( Parent.AbsoluteHeight - (effectiveVerticalCells - 1) * Parent.StackSpacing ) / effectiveVerticalCells;
            }
            else
            {
                parentWidth = Parent.AbsoluteWidth;
                parentHeight = Parent.AbsoluteHeight;
            }
        }
        else if (this.ElementGueContainingThis != null && this.ElementGueContainingThis.mContainedObjectAsIpso != null)
        {
            parentWidth = this.ElementGueContainingThis.mContainedObjectAsIpso.Width;
            parentHeight = this.ElementGueContainingThis.mContainedObjectAsIpso.Height;
        }

#if FULL_DIAGNOSTICS
        if (float.IsPositiveInfinity(parentHeight))
        {
            throw new Exception();
        }
#endif
    }

    private void UpdateTextureCoordinatesDimensionBased()
    {
        int left = mTextureLeft;
        int top = mTextureTop;

        int width = 0;

        if (mTextureWidthScale != 0)
        {
            width = (int)(RequiredContainedObject.Width / mTextureWidthScale);
        }

        int height = 0;

        if (mTextureHeightScale != 0)
        {
            height = (int)(RequiredContainedObject.Height / mTextureHeightScale);
        }


        if (mContainedObjectAsIpso is ITextureCoordinate containedTextureCoordinateObject)
        {
            switch (mTextureAddress)
            {
                case TextureAddress.DimensionsBased:
                    containedTextureCoordinateObject.SourceRectangle = new Rectangle(
                        left,
                        top,
                        width,
                        height);
                    containedTextureCoordinateObject.Wrap = mWrap;
                    break;
            }
        }
    }

    private void UpdateTextureCoordinatesNotDimensionBased()
    {
        if (mContainedObjectAsIpso is ITextureCoordinate textureCoordinateObject)
        {
            var textureAddress = mTextureAddress;
            switch (textureAddress)
            {
                case TextureAddress.EntireTexture:
                    textureCoordinateObject.SourceRectangle = null;
                    textureCoordinateObject.Wrap = false;
                    break;
                case TextureAddress.Custom:
                    textureCoordinateObject.SourceRectangle = new Rectangle(
                        mTextureLeft,
                        mTextureTop,
                        mTextureWidth,
                        mTextureHeight);
                    textureCoordinateObject.Wrap = mWrap;
                    break;
                case TextureAddress.DimensionsBased:
                    // This is done *after* setting dimensions

                    break;
            }
        }
    }

    private static float GetDimensionFromEdges(float smallEdge, float bigEdge, GeneralUnitType units)
    {
        float dimensionToReturn = 0;
        if (units == GeneralUnitType.PixelsFromSmall)
        // The value already comes in properly inverted
        {
            smallEdge = 0;

            bigEdge = System.Math.Max(0, bigEdge);
            dimensionToReturn = bigEdge - smallEdge;
        }
        else if (units == GeneralUnitType.PixelsFromMiddle ||
#pragma warning disable CS0618 // PixelsFromMiddleInverted is obsolete but still loads from older projects
            units == GeneralUnitType.PixelsFromMiddleInverted)
#pragma warning restore CS0618
        {
            // use the full width
            float abs1 = System.Math.Abs(smallEdge);
            float abs2 = System.Math.Abs(bigEdge);

            dimensionToReturn = 2 * System.Math.Max(abs1, abs2);
        }
        // A non-Text parent's baseline is its bottom edge, so Baseline measures like PixelsFromLarge.
        else if (units == GeneralUnitType.PixelsFromLarge || units == GeneralUnitType.PixelsFromBaseline)
        {
            smallEdge = System.Math.Min(0, smallEdge);
            bigEdge = 0;
            dimensionToReturn = bigEdge - smallEdge;

        }
        return dimensionToReturn;
    }

    public bool GetIfDimensionsDependOnChildren()
    {
        // If this is a Screen, then it doesn't have a size. Screens cannot depend on children:
        bool isScreen = ElementSave != null && ElementSave is ScreenSave;
        return !isScreen &&
            (this.WidthUnits.GetDependencyType() == HierarchyDependencyType.DependsOnChildren ||
            this.WidthUnits == DimensionUnitType.RelativeToMaxParentOrChildren ||
            this.HeightUnits.GetDependencyType() == HierarchyDependencyType.DependsOnChildren ||
            this.HeightUnits == DimensionUnitType.RelativeToMaxParentOrChildren);
    }
}
