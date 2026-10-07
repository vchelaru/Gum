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

    #region Position/Offsets

    // wrappedLineAxis: the axis on which a wrapping stack's line is the parent. On that axis the
    // dimension passed in is the line's, and the value adds to the line's start.
    private void AdjustParentOriginOffsetsByUnits(float parentWidth, float parentHeight, bool isParentFlippedHorizontally,
        XOrY? wrappedLineAxis, ref float unitOffsetX, ref float unitOffsetY, ref bool wasHandledX, ref bool wasHandledY)
    {

        var shouldAdd = Parent is GraphicalUiElement parentGue &&
            (parentGue.ChildrenLayout == Gum.Managers.ChildrenLayout.AutoGridVertical || parentGue.ChildrenLayout == Gum.Managers.ChildrenLayout.AutoGridHorizontal);

        if (!wasHandledX)
        {
            var units = isParentFlippedHorizontally ? mXUnits.Flip() : mXUnits;

            // For information on why this force exists, see https://github.com/vchelaru/Gum/issues/695
            bool forcePixelsFromSmall = false;

#pragma warning disable CS0618 // PixelsFromMiddleInverted is obsolete but still loads from older projects
            if (mXUnits == GeneralUnitType.PixelsFromMiddle || mXUnits == GeneralUnitType.PixelsFromMiddleInverted ||
#pragma warning restore CS0618
                mXUnits == GeneralUnitType.PixelsFromLarge || mXUnits == GeneralUnitType.Percentage)
            {
                if (IsLaterChildInStack(ChildrenLayout.LeftToRightStack))
                {
                    forcePixelsFromSmall = true;

                    if (mXUnits == GeneralUnitType.Percentage)
                    {
                        shouldAdd = true;
                    }
                }
            }


            var value = 0f;
            if (forcePixelsFromSmall)
            {
                wasHandledX = true;
            }
            if (units == GeneralUnitType.PixelsFromLarge)
            {
                value = parentWidth;
                wasHandledX = true;
            }
            else if (units == GeneralUnitType.PixelsFromMiddle)
            {
                value = parentWidth / 2.0f;
                wasHandledX = true;
            }
            else if (units == GeneralUnitType.Percentage && isParentFlippedHorizontally)
            {
                // A flipped percentage is measured from the right edge (AdjustOffsetsByUnits negates it).
                value = parentWidth;
                wasHandledX = true;
            }
            else if (units == GeneralUnitType.PixelsFromSmall)
            {
                // no need to do anything
            }

            if (isParentFlippedHorizontally && GetIfParentStacks())
            {
                // A flipped stack measures from its right edge, and the stack offset (already in
                // unitOffsetX, negative) is added to that edge rather than replaced by it.
                if (forcePixelsFromSmall)
                {
                    value = parentWidth;
                }
                shouldAdd = true;
            }

            if (shouldAdd || wrappedLineAxis == XOrY.X)
            {
                unitOffsetX += value;
            }
            // units, not mXUnits: a flipped parent turns PixelsFromSmall into PixelsFromLarge.
            else if (units != GeneralUnitType.PixelsFromSmall && !forcePixelsFromSmall)
            {
                unitOffsetX = value;
            }
        }

        if (!wasHandledY)
        {
            var value = 0f;

            // For information on why this force exists, see https://github.com/vchelaru/Gum/issues/695
            bool forcePixelsFromSmall = false;

#pragma warning disable CS0618 // PixelsFromMiddleInverted is obsolete but still loads from older projects
            if (mYUnits == GeneralUnitType.PixelsFromMiddle || mYUnits == GeneralUnitType.PixelsFromMiddleInverted ||
#pragma warning restore CS0618
                mYUnits == GeneralUnitType.PixelsFromLarge || mYUnits == GeneralUnitType.PixelsFromBaseline ||
                mYUnits == GeneralUnitType.Percentage)
            {
                if (IsLaterChildInStack(ChildrenLayout.TopToBottomStack))
                {
                    forcePixelsFromSmall = true;

                    if (mYUnits == GeneralUnitType.Percentage)
                    {
                        shouldAdd = true;
                    }
                }
            }

            if (forcePixelsFromSmall)
            {
                wasHandledY = true;
            }
            else if (mYUnits == GeneralUnitType.PixelsFromLarge)
            {
                value = parentHeight;
                wasHandledY = true;
            }
#pragma warning disable CS0618 // PixelsFromMiddleInverted is obsolete but still loads from older projects
            else if (mYUnits == GeneralUnitType.PixelsFromMiddle || mYUnits == GeneralUnitType.PixelsFromMiddleInverted)
#pragma warning restore CS0618
            {
                value = parentHeight / 2.0f;
                wasHandledY = true;
            }
            else if (mYUnits == GeneralUnitType.PixelsFromBaseline)
            {
                if (wrappedLineAxis != XOrY.Y && Parent is GraphicalUiElement gue && gue.RenderableComponent is IText text)
                {
                    // January 9, 2025 - breaking layout logic to address this:
                    // https://github.com/vchelaru/Gum/issues/473
                    //value = parentHeight - text.DescenderHeight;
                    value = text.WrappedTextHeight - text.DescenderHeight * text.FontScale;
                }
                else
                {
                    // use the bottom (of a wrapped row, the row's bottom) as baseline:
                    value = parentHeight;
                }
                wasHandledY = true;
            }

            if (shouldAdd || wrappedLineAxis == XOrY.Y)
            {
                unitOffsetY += value;
            }
            else if (mYUnits != GeneralUnitType.PixelsFromSmall && !forcePixelsFromSmall)
            {
                unitOffsetY = value;
            }
        }
    }

    private void AdjustOffsetsByUnits(float parentWidth, float parentHeight, bool isParentFlippedHorizontally, XOrY? xOrY, ref float unitOffsetX, ref float unitOffsetY)
    {
        bool doX = xOrY == null || xOrY == XOrY.X;
        bool doY = xOrY == null || xOrY == XOrY.Y;

        if (doX)
        {
            if (mXUnits == GeneralUnitType.Percentage)
            {
                // Under a flipped parent the percentage runs leftward from the right edge.
                unitOffsetX = (isParentFlippedHorizontally ? -1 : 1) * parentWidth * mX / 100.0f;
            }
            else if (mXUnits == GeneralUnitType.PercentageOfFile)
            {
                bool wasSet = false;

                if (mContainedObjectAsIpso is ITextureCoordinate asITextureCoordinate)
                {
                    if (asITextureCoordinate.TextureWidth != null)
                    {
                        unitOffsetX = asITextureCoordinate.TextureWidth.Value * mX / 100.0f;
                        wasSet = true;
                    }
                }

                if (!wasSet)
                {
                    unitOffsetX = 64 * mX / 100.0f;
                }
            }
            else
            {
                if (isParentFlippedHorizontally)
                {
                    unitOffsetX -= mX;
                }
                else
                {
                    unitOffsetX += mX;
                }
            }
        }

        if (doY)
        {
            if (mYUnits == GeneralUnitType.Percentage)
            {
                unitOffsetY = parentHeight * mY / 100.0f;
            }
            else if (mYUnits == GeneralUnitType.PercentageOfFile)
            {

                bool wasSet = false;


                if (mContainedObjectAsIpso is ITextureCoordinate asITextureCoordinate)
                {
                    if (asITextureCoordinate.TextureHeight != null)
                    {
                        unitOffsetY = asITextureCoordinate.TextureHeight.Value * mY / 100.0f;
                        wasSet = true;
                    }
                }

                if (!wasSet)
                {
                    unitOffsetY = 64 * mY / 100.0f;
                }
            }
#pragma warning disable CS0618 // PixelsFromMiddleInverted is obsolete but still loads from older projects
            else if (mYUnits == GeneralUnitType.PixelsFromMiddleInverted)
#pragma warning restore CS0618
            {
                unitOffsetY += -mY;
            }
            else
            {
                unitOffsetY += mY;
            }
        }
    }

    private void UpdatePosition(float parentWidth, float parentHeight, XOrY? xOrY, float parentAbsoluteRotation, bool isParentFlippedHorizontally)
    {
        // First get the position of the object without considering if this object should be wrapped.
        // This call may result in the object being placed outside of its parent's bounds. In which case
        // it will be wrapped....later
        UpdatePosition(parentWidth, parentHeight, isParentFlippedHorizontally, shouldWrap: false, xOrY: xOrY, parentRotation: parentAbsoluteRotation);

        var effectiveParent = EffectiveParentGue;

        // Wrap the object if:
        bool shouldWrap =
            effectiveParent != null &&
        // * The parent stacks
            effectiveParent.ChildrenLayout != Gum.Managers.ChildrenLayout.Regular &&

            // * And the parent wraps
            effectiveParent.WrapsChildren &&

            // * And the object is outside of parent's bounds plus any overhang its negative padding allows
            // (a flipped LeftToRightStack runs leftward, so it overflows its left edge)
            ((effectiveParent.ChildrenLayout == Gum.Managers.ChildrenLayout.LeftToRightStack &&
                (isParentFlippedHorizontally
                    ? this.GetAbsoluteLeft() < effectiveParent.GetAbsoluteLeft() - effectiveParent.GetNegativePaddingOverhang(XOrY.X)
                    : this.GetAbsoluteRight() > effectiveParent.GetAbsoluteRight() + effectiveParent.GetNegativePaddingOverhang(XOrY.X))) ||
            (effectiveParent.ChildrenLayout == Gum.Managers.ChildrenLayout.TopToBottomStack &&
                this.GetAbsoluteBottom() > effectiveParent.GetAbsoluteBottom() + effectiveParent.GetNegativePaddingOverhang(XOrY.Y)));

        if (shouldWrap)
        {
            UpdatePosition(parentWidth, parentHeight, isParentFlippedHorizontally, shouldWrap, xOrY: xOrY, parentRotation: parentAbsoluteRotation);
        }
    }

    // A wrapping stack sized from its children never breaks a line shorter than its measure did, so
    // negative padding lets that line overhang the edge instead of re-wrapping it (#5808).
    private float GetNegativePaddingOverhang(XOrY axis)
    {
        var (units, measuredLine, size) = axis == XOrY.X
            ? (mWidthUnit, _measuredLineWidth, AbsoluteWidth)
            : (mHeightUnit, _measuredLineHeight, AbsoluteHeight);
        bool isSizedFromChildren = units == DimensionUnitType.RelativeToChildren || units == DimensionUnitType.RelativeToMaxParentOrChildren;
        return isSizedFromChildren ? System.Math.Max(0, measuredLine - size) : 0;
    }

    private void UpdatePosition(float parentWidth, float parentHeight, bool isParentFlippedHorizontally, bool shouldWrap, XOrY? xOrY, float parentRotation)
    {
#if FULL_DIAGNOSTICS
        if (float.IsPositiveInfinity(parentHeight) || float.IsNegativeInfinity(parentHeight))
        {
            throw new ArgumentException(nameof(parentHeight));
        }
        if (float.IsPositiveInfinity(parentHeight) || float.IsNegativeInfinity(parentHeight))
        {
            throw new ArgumentException(nameof(parentHeight));
        }

#endif

        float parentOriginOffsetX;
        float parentOriginOffsetY;
        bool wasHandledX;
        bool wasHandledY;

        bool canWrap = EffectiveParentGue?.WrapsChildren == true;

        GetParentOffsets(canWrap, shouldWrap, parentWidth, parentHeight, isParentFlippedHorizontally,
            out parentOriginOffsetX, out parentOriginOffsetY,
            out wasHandledX, out wasHandledY, out float positionParentWidth, out float positionParentHeight);

        var lineAxis = GetIfParentStacks() ? GetWrappedLineAxis(EffectiveParentGue) : null;
        _wrappedLineSizeUsedForPosition = lineAxis != null && IsPositionedFromWrappedLineSize(lineAxis.Value)
            ? (lineAxis == XOrY.X ? positionParentWidth : positionParentHeight)
            : float.NaN;

        float unitOffsetX = 0;
        float unitOffsetY = 0;


        AdjustOffsetsByUnits(positionParentWidth, positionParentHeight, isParentFlippedHorizontally, xOrY, ref unitOffsetX, ref unitOffsetY);
#if FULL_DIAGNOSTICS
        if (float.IsNaN(unitOffsetX))
        {
            throw new Exception("Invalid unitOffsetX after AdjustOffsetsByUnits - it's NaN");
        }

        if (float.IsNaN(unitOffsetY))
        {
            throw new Exception("Invalid unitOffsetY after AdjustOffsetsByUnits - it's NaN");
        }
#endif


        AdjustOffsetsByOrigin(isParentFlippedHorizontally, ref unitOffsetX, ref unitOffsetY);
#if FULL_DIAGNOSTICS
        if (float.IsNaN(unitOffsetX))
        {
            throw new Exception("Invalid unitOffsetX after AdjustOffsetsByOrigin - it's NaN");
        }
        if (float.IsNaN(unitOffsetY))
        {
            throw new Exception("Invalid unitOffsetY after AdjustOffsetsByOrigin - it's NaN");
        }
#endif

        unitOffsetX += parentOriginOffsetX;
        unitOffsetY += parentOriginOffsetY;

        if (parentRotation != 0)
        {
            GetRightAndUpFromRotation(parentRotation, out Vector3 right, out Vector3 up);


            var rotatedOffset = unitOffsetX * right + unitOffsetY * up;


            unitOffsetX = rotatedOffset.X;
            unitOffsetY = rotatedOffset.Y;

        }


        // See if we're explicitly updating only Y. If so, skip setting X.
        if (xOrY != XOrY.Y)
        {
            this.mContainedObjectAsIpso!.X = unitOffsetX;
        }

        // See if we're explicitly updating only X. If so, skip setting Y.
        if (xOrY != XOrY.X)
        {
            this.mContainedObjectAsIpso!.Y = unitOffsetY;
        }
    }

    public void GetParentOffsets(out float parentOriginOffsetX, out float parentOriginOffsetY)
    {
        float parentWidth;
        float parentHeight;
        GetParentDimensions(out parentWidth, out parentHeight);

        bool throwaway1;
        bool throwaway2;

        bool canWrap = false;
        var effectiveParent = EffectiveParentGue;
        bool isParentFlippedHorizontally = false;
        if (effectiveParent != null)
        {
            canWrap = effectiveParent.WrapsChildren;
            isParentFlippedHorizontally = effectiveParent.GetAbsoluteFlipHorizontal();
        }


        // indicating false to wrap will reset the index on this. We don't want this method
        // to modify anything so store it off and resume:
        var oldIndex = StackedRowOrColumnIndex;


        GetParentOffsets(canWrap, false, parentWidth, parentHeight, isParentFlippedHorizontally, out parentOriginOffsetX, out parentOriginOffsetY,
            out throwaway1, out throwaway2, out _, out _);

        StackedRowOrColumnIndex = oldIndex;

    }

    // positionParentWidth/Height are what this element's X/Y units measure against: the parent's size,
    // or on the cross axis of a wrapping stack, its line's size (#5802).
    private void GetParentOffsets(bool canWrap, bool shouldWrap, float parentWidth, float parentHeight, bool isParentFlippedHorizontally, out float parentOriginOffsetX, out float parentOriginOffsetY,
        out bool wasHandledX, out bool wasHandledY, out float positionParentWidth, out float positionParentHeight)
    {
        parentOriginOffsetX = 0;
        parentOriginOffsetY = 0;

        TryAdjustOffsetsByParentLayoutType(canWrap, shouldWrap, ref parentOriginOffsetX, ref parentOriginOffsetY);

        wasHandledX = false;
        wasHandledY = false;

        positionParentWidth = parentWidth;
        positionParentHeight = parentHeight;

        // In a wrapping stack the child's line is its parent on the cross axis. The stack offset above
        // already moved it to the line's start; the units then measure within the line.
        var lineAxis = GetIfParentStacks() ? GetWrappedLineAxis(EffectiveParentGue) : null;
        if (lineAxis == XOrY.Y)
        {
            _wrappedLineStart = parentOriginOffsetY;
            positionParentHeight = GetWrappedLineSize(EffectiveParentGue!);
        }
        else if (lineAxis == XOrY.X)
        {
            _wrappedLineStart = isParentFlippedHorizontally ? -parentOriginOffsetX : parentOriginOffsetX;
            positionParentWidth = GetWrappedLineSize(EffectiveParentGue!);
            if (isParentFlippedHorizontally)
            {
                // A flipped parent measures X from its right edge, so mirror the column: its right
                // edge is the parent's right edge minus the stack offset.
                parentOriginOffsetX += parentWidth - positionParentWidth;
            }
        }

        AdjustParentOriginOffsetsByUnits(positionParentWidth, positionParentHeight, isParentFlippedHorizontally, lineAxis,
            ref parentOriginOffsetX, ref parentOriginOffsetY, ref wasHandledX, ref wasHandledY);

    }

    /// <summary>
    /// Whether this is a child after the first in a parent stacking along the given axis. The stack sets
    /// such a child's leading edge, so its main-axis units and origin are ignored (#695, #5766).
    /// </summary>
    private bool IsLaterChildInStack(ChildrenLayout stackLayout)
    {
        var effectiveParentGue = this.EffectiveParentGue;
        if (effectiveParentGue?.ChildrenLayout != stackLayout)
        {
            return false;
        }

        // With no Parent, the effective parent is the element containing this.
        // A non-GraphicalUiElement Parent has no sibling list to stack in.
        System.Collections.IList? siblings = this.Parent == null
            ? effectiveParentGue.mWhatThisContains
            : (this.Parent as GraphicalUiElement)?.Children as System.Collections.IList;

        return (siblings?.IndexOf(this) ?? -1) > 0;
    }

    private void AdjustOffsetsByOrigin(bool isParentFlippedHorizontally, ref float unitOffsetX, ref float unitOffsetY)
    {
#if FULL_DIAGNOSTICS
        if (float.IsPositiveInfinity(mRotation) || float.IsNegativeInfinity(mRotation))
        {
            throw new Exception("Rotation cannot be negative/positive infinity");
        }
#endif
        float offsetX = 0;
        float offsetY = 0;

        // Default origins skip the sibling lookup, which is linear in the sibling count.
        var xOrigin = mXOrigin != HorizontalAlignment.Left && IsLaterChildInStack(ChildrenLayout.LeftToRightStack)
            ? HorizontalAlignment.Left : mXOrigin;
        var yOrigin = mYOrigin != VerticalAlignment.Top && IsLaterChildInStack(ChildrenLayout.TopToBottomStack)
            ? VerticalAlignment.Top : mYOrigin;

        HorizontalAlignment effectiveXorigin = isParentFlippedHorizontally ? xOrigin.Flip() : xOrigin;

        if (!float.IsNaN(RequiredContainedObject.Width))
        {
            if (effectiveXorigin == HorizontalAlignment.Center)
            {
                offsetX -= RequiredContainedObject.Width / 2.0f;
            }
            else if (effectiveXorigin == HorizontalAlignment.Right)
            {
                offsetX -= RequiredContainedObject.Width;
            }
        }
        // no need to handle left


        if (yOrigin == VerticalAlignment.Center)
        {
            offsetY -= RequiredContainedObject.Height / 2.0f;
        }
        else if (yOrigin == VerticalAlignment.TextBaseline)
        {
            if (mContainedObjectAsIpso is IText text)
            {
                offsetY += -mContainedObjectAsIpso.Height + text.DescenderHeight * text.FontScale;
            }
            else
            {
                offsetY -= RequiredContainedObject.Height;
            }
        }
        else if (yOrigin == VerticalAlignment.Bottom)
        {
            offsetY -= RequiredContainedObject.Height;
        }
        // no need to handle top


        // Adjust offsets by rotation
        if (mRotation != 0)
        {
            var rotation = isParentFlippedHorizontally ? -mRotation : mRotation;

            GetRightAndUpFromRotation(rotation, out Vector3 right, out Vector3 up);

            var unrotatedX = offsetX;
            var unrotatedY = offsetY;

            offsetX = right.X * unrotatedX + up.X * unrotatedY;
            offsetY = right.Y * unrotatedX + up.Y * unrotatedY;
        }

        unitOffsetX += offsetX;
        unitOffsetY += offsetY;
    }

    private void TryAdjustOffsetsByParentLayoutType(bool canWrap, bool shouldWrap, ref float unitOffsetX, ref float unitOffsetY)
    {


        if (GetIfParentStacks())
        {
            var effectiveParent = this.EffectiveParentGue;

            // O(1) fast path: when UseFixedStackChildrenSize is true on a TopToBottomStack
            // without wrapping, compute position directly from visible index × fixed height
            // instead of walking the sibling chain.
            if (!canWrap &&
                effectiveParent != null &&
                effectiveParent.UseFixedStackChildrenSize &&
                effectiveParent.ChildrenLayout == Gum.Managers.ChildrenLayout.TopToBottomStack &&
                effectiveParent.Children?.Count > 0)
            {
                var visibleIndex = this.GetIndexInVisibleSiblings();
                if (visibleIndex > 0)
                {
                    var firstChild = effectiveParent.GetFirstVisibleChild()!;
                    // Stack after the first child's laid-out top, which includes its own Y offset.
                    var firstChildTop = ((IPositionedSizedObject)firstChild).Y;
                    unitOffsetY += firstChildTop + visibleIndex * (firstChild.AbsoluteHeight + effectiveParent.StackSpacing);
                }
                this.StackedRowOrColumnIndex = 0;
            }
            else
            {
                float whatToStackAfterX;
                float whatToStackAfterY;

                var whatToStackAfter = GetWhatToStackAfter(canWrap, shouldWrap, out whatToStackAfterX, out whatToStackAfterY);



                float xRelativeTo = 0;
                float yRelativeTo = 0;

                if (whatToStackAfter != null)
                {
                    switch (effectiveParent!.ChildrenLayout)
                    {
                        case Gum.Managers.ChildrenLayout.TopToBottomStack:

                            if (canWrap)
                            {
                                xRelativeTo = whatToStackAfterX;
                            }

                            yRelativeTo = whatToStackAfterY;

                            break;
                        case Gum.Managers.ChildrenLayout.LeftToRightStack:

                            xRelativeTo = whatToStackAfterX;

                            if (canWrap)
                            {
                                yRelativeTo = whatToStackAfterY;
                            }
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }

                // Stack offsets are measured in unflipped space; a flipped stack runs right to left.
                unitOffsetX += effectiveParent!.GetAbsoluteFlipHorizontal() ? -xRelativeTo : xRelativeTo;
                unitOffsetY += yRelativeTo;
            }
        }
        else if (GetIfParentIsAutoGrid())
        {
            var indexInSiblingList = this.GetIndexInVisibleSiblings();
            int xIndex, yIndex;
            float cellWidth, cellHeight;
            GetCellDimensions(indexInSiblingList, out xIndex, out yIndex, out cellWidth, out cellHeight);

            // EffectiveParentGue, not Parent: a parentless child of a grid component has no Parent.
            GraphicalUiElement gridParent = EffectiveParentGue!;
            var stackSpacing = gridParent.StackSpacing;
            if (gridParent.GetAbsoluteFlipHorizontal())
            {
                // Mirror the column: the child's own units and origin already mirror inside the cell.
                unitOffsetX += gridParent.AbsoluteWidth - cellWidth * (xIndex + 1) - stackSpacing * xIndex;
            }
            else
            {
                unitOffsetX += cellWidth * xIndex + stackSpacing * xIndex;
            }
            unitOffsetY += cellHeight * yIndex + stackSpacing * yIndex;
        }
    }

    #endregion

    static void GetRightAndUpFromRotation(float rotationInDegrees, out Vector3 right, out Vector3 up)
    {

        var quarterRotations = rotationInDegrees / 90;
        var radiansFromPerfectRotation = System.Math.Abs(quarterRotations - MathFunctions.RoundToInt(quarterRotations));

        const float errorToTolerate = .1f / 90f;

        if (radiansFromPerfectRotation < errorToTolerate)
        {
            var quarterRotationsAsInt = MathFunctions.RoundToInt(quarterRotations) % 4;
            if (quarterRotationsAsInt < 0)
            {
                quarterRotationsAsInt += 4;
            }

            // invert it to match how rotation works with the CreateRotationZ method:
            quarterRotationsAsInt = 4 - quarterRotationsAsInt;

            right = Vector3Extensions.Right;
            up = Vector3Extensions.Up;

            switch (quarterRotationsAsInt)
            {
                case 0:
                    right = Vector3Extensions.Right;
                    up = Vector3Extensions.Up;
                    break;
                case 1:
                    right = Vector3Extensions.Up;
                    up = Vector3Extensions.Left;
                    break;
                case 2:
                    right = Vector3Extensions.Left;
                    up = Vector3Extensions.Down;
                    break;

                case 3:
                    right = Vector3Extensions.Down;
                    up = Vector3Extensions.Right;
                    break;
            }
        }
        else
        {
            var matrix = Matrix.CreateRotationZ(-MathHelper.ToRadians(rotationInDegrees));
            right = matrix.Right();
            up = matrix.Up();
        }

    }
}
