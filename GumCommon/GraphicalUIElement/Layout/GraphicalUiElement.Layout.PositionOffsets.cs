using Gum.Converters;
using Gum.Managers;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    public partial class GraphicalUiElement
    {
        #region Position/Offsets

        private void AdjustParentOriginOffsetsByUnits(float parentWidth, float parentHeight, bool isParentFlippedHorizontally,
            ref float unitOffsetX, ref float unitOffsetY, ref bool wasHandledX, ref bool wasHandledY)
        {

            var shouldAdd = Parent is GraphicalUiElement parentGue &&
                (parentGue.ChildrenLayout == Gum.Managers.ChildrenLayout.AutoGridVertical || parentGue.ChildrenLayout == Gum.Managers.ChildrenLayout.AutoGridHorizontal);

            if (!wasHandledX)
            {
                var units = isParentFlippedHorizontally ? mXUnits.Flip() : mXUnits;

                // For information on why this force exists, see https://github.com/vchelaru/Gum/issues/695
                bool forcePixelsFromSmall = false;

                if (mXUnits == GeneralUnitType.PixelsFromMiddle || mXUnits == GeneralUnitType.PixelsFromMiddleInverted ||
                    mXUnits == GeneralUnitType.PixelsFromLarge)
                {
                    if (this.EffectiveParentGue?.ChildrenLayout == ChildrenLayout.LeftToRightStack)
                    {
                        System.Collections.IList siblings = null;

                        if (this.Parent == null)
                        {
                            siblings = this.ElementGueContainingThis.mWhatThisContains;
                        }
                        else if (this.Parent is GraphicalUiElement)
                        {
                            siblings = ((GraphicalUiElement)Parent).Children as System.Collections.IList;
                        }
                        var thisIndex = siblings.IndexOf(this);
                        if (thisIndex > 0)
                        {
                            forcePixelsFromSmall = true;


                            if (mXUnits == GeneralUnitType.Percentage)
                            {
                                shouldAdd = true;
                            }
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
                else if (units == GeneralUnitType.PixelsFromSmall)
                {
                    // no need to do anything
                }

                if (shouldAdd)
                {
                    unitOffsetX += value;
                }
                else if (mXUnits != GeneralUnitType.PixelsFromSmall && !forcePixelsFromSmall)
                {
                    unitOffsetX = value;
                }
            }

            if (!wasHandledY)
            {
                var value = 0f;

                // For information on why this force exists, see https://github.com/vchelaru/Gum/issues/695
                bool forcePixelsFromSmall = false;

                if (mYUnits == GeneralUnitType.PixelsFromMiddle || mYUnits == GeneralUnitType.PixelsFromMiddleInverted ||
                    mYUnits == GeneralUnitType.PixelsFromLarge || mYUnits == GeneralUnitType.PixelsFromBaseline ||
                    mYUnits == GeneralUnitType.Percentage)
                {
                    if (this.EffectiveParentGue?.ChildrenLayout == ChildrenLayout.TopToBottomStack)
                    {
                        System.Collections.IList siblings = null;

                        if (this.Parent == null)
                        {
                            siblings = this.ElementGueContainingThis.mWhatThisContains;
                        }
                        else if (this.Parent is GraphicalUiElement)
                        {
                            siblings = ((GraphicalUiElement)Parent).Children as System.Collections.IList;
                        }
                        var thisIndex = siblings.IndexOf(this);
                        if (thisIndex > 0)
                        {
                            forcePixelsFromSmall = true;

                            if (mYUnits == GeneralUnitType.Percentage)
                            {
                                shouldAdd = true;
                            }
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
                else if (mYUnits == GeneralUnitType.PixelsFromMiddle || mYUnits == GeneralUnitType.PixelsFromMiddleInverted)
                {
                    value = parentHeight / 2.0f;
                    wasHandledY = true;
                }
                else if (mYUnits == GeneralUnitType.PixelsFromBaseline)
                {
                    if (Parent is GraphicalUiElement gue && gue.RenderableComponent is IText text)
                    {
                        // January 9, 2025 - breaking layout logic to address this:
                        // https://github.com/vchelaru/Gum/issues/473
                        //value = parentHeight - text.DescenderHeight;
                        value = text.WrappedTextHeight - text.DescenderHeight;
                    }
                    else
                    {
                        // use the bottom as baseline:
                        value = parentHeight;
                    }
                    wasHandledY = true;
                }

                if (shouldAdd)
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
                    unitOffsetX = parentWidth * mX / 100.0f;
                }
                else if (mXUnits == GeneralUnitType.PercentageOfFile)
                {
                    bool wasSet = false;

                    if (mContainedObjectAsIpso is ITextureCoordinate asITextureCoordinate)
                    {
                        if (asITextureCoordinate.TextureWidth != null)
                        {
                            unitOffsetX = asITextureCoordinate.TextureWidth.Value * mX / 100.0f;
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
                        }
                    }

                    if (!wasSet)
                    {
                        unitOffsetY = 64 * mY / 100.0f;
                    }
                }
                else if (mYUnits == GeneralUnitType.PixelsFromMiddleInverted)
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

                // * And the object is outside of parent's bounds
                ((effectiveParent.ChildrenLayout == Gum.Managers.ChildrenLayout.LeftToRightStack && this.GetAbsoluteRight() > effectiveParent.GetAbsoluteRight()) ||
                (effectiveParent.ChildrenLayout == Gum.Managers.ChildrenLayout.TopToBottomStack && this.GetAbsoluteBottom() > effectiveParent.GetAbsoluteBottom()));

            if (shouldWrap)
            {
                UpdatePosition(parentWidth, parentHeight, isParentFlippedHorizontally, shouldWrap, xOrY: xOrY, parentRotation: parentAbsoluteRotation);
            }
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
                out wasHandledX, out wasHandledY);


            float unitOffsetX = 0;
            float unitOffsetY = 0;


            AdjustOffsetsByUnits(parentWidth, parentHeight, isParentFlippedHorizontally, xOrY, ref unitOffsetX, ref unitOffsetY);
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
                this.mContainedObjectAsIpso.X = unitOffsetX;
            }

            // See if we're explicitly updating only X. If so, skip setting Y.
            if (xOrY != XOrY.X)
            {
                this.mContainedObjectAsIpso.Y = unitOffsetY;
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
                out throwaway1, out throwaway2);

            StackedRowOrColumnIndex = oldIndex;

        }

        private void GetParentOffsets(bool canWrap, bool shouldWrap, float parentWidth, float parentHeight, bool isParentFlippedHorizontally, out float parentOriginOffsetX, out float parentOriginOffsetY,
            out bool wasHandledX, out bool wasHandledY)
        {
            parentOriginOffsetX = 0;
            parentOriginOffsetY = 0;

            TryAdjustOffsetsByParentLayoutType(canWrap, shouldWrap, ref parentOriginOffsetX, ref parentOriginOffsetY);

            wasHandledX = false;
            wasHandledY = false;

            AdjustParentOriginOffsetsByUnits(parentWidth, parentHeight, isParentFlippedHorizontally, ref parentOriginOffsetX, ref parentOriginOffsetY,
                ref wasHandledX, ref wasHandledY);

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

            HorizontalAlignment effectiveXorigin = isParentFlippedHorizontally ? mXOrigin.Flip() : mXOrigin;

            if (!float.IsNaN(mContainedObjectAsIpso.Width))
            {
                if (effectiveXorigin == HorizontalAlignment.Center)
                {
                    offsetX -= mContainedObjectAsIpso.Width / 2.0f;
                }
                else if (effectiveXorigin == HorizontalAlignment.Right)
                {
                    offsetX -= mContainedObjectAsIpso.Width;
                }
            }
            // no need to handle left


            if (mYOrigin == VerticalAlignment.Center)
            {
                offsetY -= mContainedObjectAsIpso.Height / 2.0f;
            }
            else if (mYOrigin == VerticalAlignment.TextBaseline)
            {
                if (mContainedObjectAsIpso is IText text)
                {
                    offsetY += -mContainedObjectAsIpso.Height + text.DescenderHeight * text.FontScale;
                }
                else
                {
                    offsetY -= mContainedObjectAsIpso.Height;
                }
            }
            else if (mYOrigin == VerticalAlignment.Bottom)
            {
                offsetY -= mContainedObjectAsIpso.Height;
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
                float whatToStackAfterX;
                float whatToStackAfterY;

                var whatToStackAfter = GetWhatToStackAfter(canWrap, shouldWrap, out whatToStackAfterX, out whatToStackAfterY);



                float xRelativeTo = 0;
                float yRelativeTo = 0;

                if (whatToStackAfter != null)
                {
                    var effectiveParent = this.EffectiveParentGue;
                    switch (effectiveParent.ChildrenLayout)
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

                unitOffsetX += xRelativeTo;
                unitOffsetY += yRelativeTo;
            }
            else if (GetIfParentIsAutoGrid())
            {
                var indexInSiblingList = this.GetIndexInVisibleSiblings();
                int xIndex, yIndex;
                float cellWidth, cellHeight;
                GetCellDimensions(indexInSiblingList, out xIndex, out yIndex, out cellWidth, out cellHeight);

                unitOffsetX += cellWidth * xIndex + Parent.StackSpacing * (xIndex);
                unitOffsetY += cellHeight * yIndex + Parent.StackSpacing * (yIndex);
            }
        }

        #endregion
    }
}