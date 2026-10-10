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

    private void UpdateDimensions(float parentWidth, float parentHeight, XOrY? xOrY, bool considerWrappedStacked)
    {
        // special case - if the user has set both values to depend on the other value, we don't want to have an infinite recursion so we'll just apply the width and height values as pixel values.
        // This really doesn't make much sense but...the alternative would be an object that may grow or shrink infinitely, which may cause lots of other problems:
        if (IsSizedFromOtherDimension(mWidthUnit) && IsSizedFromOtherDimension(mHeightUnit))
        {
            RequiredContainedObject.Width = mWidth;
            RequiredContainedObject.Height = mHeight;
        }
        else
        {
            var doHeightFirst = mWidthUnit == DimensionUnitType.PercentageOfOtherDimension ||
                mWidthUnit == DimensionUnitType.MaintainFileAspectRatio;

            // Explanation on why we use this:
            // Whenever an UpdateLayout happens,
            // the parent may tell its children to
            // update on only one axis. This allows
            // the child to update its absolute dimension
            // along that axis which the parent can then use
            // to update its own dimensions. However, if the axis
            // that the parent requested depends on the other axis on
            // the child, then the child will not be able to properly update
            // the requested axis until it updates the other axis. Therefore,
            // we should attempt to update both, but ONLY if the other axis is
            var widthUnitDependencyType = mWidthUnit.GetDependencyType();
            var heightUnitDependencyType = mHeightUnit.GetDependencyType();

            if (doHeightFirst)
            {
                // if width depends on height, do height first:
                if (xOrY == null || xOrY == XOrY.Y || heightUnitDependencyType == HierarchyDependencyType.NoDependency)
                {
                    UpdateHeight(parentHeight, considerWrappedStacked);
                }
                if (xOrY == null || xOrY == XOrY.X || widthUnitDependencyType == HierarchyDependencyType.NoDependency)
                {
                    UpdateWidth(parentWidth, considerWrappedStacked);
                }
            }
            else // either width needs to be first, or it doesn't matter so we just do width first arbitrarily
            {
                // If height depends on width, do width first
                if (xOrY == null || xOrY == XOrY.X || widthUnitDependencyType == HierarchyDependencyType.NoDependency)
                {
                    UpdateWidth(parentWidth, considerWrappedStacked);
                }
                if (xOrY == null || xOrY == XOrY.Y || heightUnitDependencyType == HierarchyDependencyType.NoDependency)
                {
                    UpdateHeight(parentHeight, considerWrappedStacked);
                }
            }
        }
    }

    static bool IsSizedFromOtherDimension(DimensionUnitType unit) =>
        unit == DimensionUnitType.PercentageOfOtherDimension || unit == DimensionUnitType.MaintainFileAspectRatio;

    static bool IsMeasuredFromChildren(DimensionUnitType unit) =>
        unit.GetDependencyType() == HierarchyDependencyType.DependsOnChildren || unit == DimensionUnitType.RelativeToMaxParentOrChildren;

    public void UpdateHeight(float parentHeight, bool considerWrappedStacked)
    {
        float pixelHeightToSet = mHeight;

        // Tell a contained text whether its Height is derived from its own lines, so
        // TextOverflowVerticalMode.TruncateLine does not cap the lines by a height that came from
        // those lines (issue #3372). Set before the switch because the RelativeToChildren branch below
        // temporarily reassigns the text's Width, which re-wraps it. Mirrors how a RelativeToChildren
        // width pushes a null wrap Width onto the renderable.
        if (mContainedObjectAsIpso is IWrappedText wrappedText)
        {
            wrappedText.IsHeightDependentOnLines = mHeightUnit == DimensionUnitType.RelativeToChildren;
        }

        switch (mHeightUnit)
        {

            #region AbsoluteMultipliedByFontScale

            case DimensionUnitType.AbsoluteMultipliedByFontScale:
                {
                    pixelHeightToSet *= GlobalFontScale;
                }
                break;

            #endregion

            #region ScreenPixel

            case DimensionUnitType.ScreenPixel:
                {
                    var effectiveManagers = this.EffectiveManagers;
                    if (effectiveManagers != null)
                    {
                        pixelHeightToSet /= effectiveManagers.Renderer.Camera.Zoom;
                    }
                }
                break;

            #endregion

            #region RelativeToChildren

            case DimensionUnitType.RelativeToChildren:
                {
                    float maxHeight = 0;


                    if (this.mContainedObjectAsIpso != null)
                    {
                        if (mContainedObjectAsIpso is IText asText)
                        {
                            // Note: UpdateWidth's equivalent text path wraps WrappedTextWidth in a
                            // try/catch for BadImageFormatException (Skia platform bug). If a similar
                            // crash is ever observed here for WrappedTextHeight, add the same handling.
                            var oldWidth = mContainedObjectAsIpso.Width;
                            if (WidthUnits == DimensionUnitType.RelativeToChildren)
                            {
                                if (MaxWidth != null)
                                {
                                    mContainedObjectAsIpso.Width = MaxWidth.Value;
                                }
                                else
                                {
                                    mContainedObjectAsIpso.Width = float.PositiveInfinity;
                                }
                            }
                            maxHeight = asText.WrappedTextHeight;
                            mContainedObjectAsIpso.Width = oldWidth;
                        }

                        if (useFixedStackChildrenSize && this.ChildrenLayout == ChildrenLayout.TopToBottomStack && this.Children.Count > 1 &&
                            GetFirstVisibleChild() is GraphicalUiElement element)
                        {
                            maxHeight = element.GetRequiredParentHeight();
                            var elementHeight = element.AbsoluteHeight;
                            maxHeight += (StackSpacing + elementHeight) * (GetVisibleChildCount() - 1);
                        }
                        else
                        {
                            float maxCellHeight = GetMaxCellHeight(considerWrappedStacked, maxHeight);
                            _measuredLineHeight = maxCellHeight;

                            maxHeight = maxCellHeight;

                            if (this.ChildrenLayout == ChildrenLayout.AutoGridHorizontal || this.ChildrenLayout == ChildrenLayout.AutoGridVertical)
                            {
                                // Cell counts below 1 are placed as 1 (see GetCellDimensions), so size them the same way.
                                var numberOfVerticalCells = System.Math.Max(1, this.AutoGridVerticalCells);

                                if (this.ChildrenLayout == ChildrenLayout.AutoGridHorizontal)
                                {
                                    var columnCount = System.Math.Max(1, this.AutoGridHorizontalCells);
                                    var requiredRowCount = (int)Math.Ceiling((float)GetVisibleChildCount() / columnCount);
                                    numberOfVerticalCells = System.Math.Max(numberOfVerticalCells, requiredRowCount);
                                }

                                // We got the largest size for one child, but that child must be contained within a cell, and all cells must be
                                // at least that same size, so we multiply the size by the number of cells tall
                                maxHeight = maxCellHeight * numberOfVerticalCells + (numberOfVerticalCells-1) * StackSpacing;
                            }

                        }
                    }

                    pixelHeightToSet = maxHeight + mHeight;
                }
                break;

            #endregion

            #region Percentage (of parent)

            case DimensionUnitType.PercentageOfParent:
                {
                    pixelHeightToSet = parentHeight * mHeight / 100.0f;
                }
                break;
            #endregion

            #region PercentageOfSourceFile

            case DimensionUnitType.PercentageOfSourceFile:
                {
                    bool wasSet = false;

                    if (mTextureHeight > 0 && TextureAddress != TextureAddress.EntireTexture)
                    {
                        pixelHeightToSet = mTextureHeight * mHeight / 100.0f;
                        wasSet = true;
                    }

                    if (mContainedObjectAsIpso is ITextureCoordinate iTextureCoordinate)
                    {
                        if (iTextureCoordinate.TextureHeight != null)
                        {
                            pixelHeightToSet = iTextureCoordinate.TextureHeight.Value * mHeight / 100.0f;
                            wasSet = true;
                        }

                        // If the address is dimension based, then that means texture coords depend on dimension...but we
                        // can't make dimension based on texture coords as that would cause a circular reference
                        if (iTextureCoordinate.SourceRectangle.HasValue && mTextureAddress != TextureAddress.DimensionsBased)
                        {
                            pixelHeightToSet = iTextureCoordinate.SourceRectangle.Value.Height * mHeight / 100.0f;
                            wasSet = true;
                        }
                    }

                    if (!wasSet)
                    {
                        pixelHeightToSet = 64 * mHeight / 100.0f;
                    }
                }
                break;
            #endregion

            #region MaintainFileAspectRatio

            case DimensionUnitType.MaintainFileAspectRatio:
                {
                    bool wasSet = false;


                    if (mContainedObjectAsIpso is IAspectRatio aspectRatioObject && IsUsableAspectRatio(aspectRatioObject.AspectRatio))
                    {
                        pixelHeightToSet = AbsoluteWidth * (mHeight / 100.0f) / aspectRatioObject.AspectRatio;
                        wasSet = true;

                        if (wasSet && mContainedObjectAsIpso is ITextureCoordinate textureCoordinate)
                        {
                            // If the address is dimension based, then that means texture coords depend on dimension...but we
                            // can't make dimension based on texture coords as that would cause a circular reference
                            if (textureCoordinate.SourceRectangle.HasValue && mTextureAddress != TextureAddress.DimensionsBased)
                            {
                                var scale = 1f;
                                if (textureCoordinate.SourceRectangle.Value.Width != 0)
                                {
                                    scale = AbsoluteWidth / textureCoordinate.SourceRectangle.Value.Width;
                                }
                                pixelHeightToSet = textureCoordinate.SourceRectangle.Value.Height * scale * mHeight / 100.0f;
                            }
                        }
                    }
                    if (!wasSet)
                    {
                        pixelHeightToSet = 64 * mHeight / 100.0f;
                    }
                }
                break;
            #endregion

            #region RelativeToParent (in pixels)

            case DimensionUnitType.RelativeToParent:
                {
                    pixelHeightToSet = parentHeight + mHeight;
                }
                break;
            #endregion

            #region PercentageOfOtherDimension

            case DimensionUnitType.PercentageOfOtherDimension:
                {
                    pixelHeightToSet = RequiredContainedObject.Width * mHeight / 100.0f;
                }
                break;
            #endregion

            #region Ratio
            case DimensionUnitType.Ratio:
                {
                    pixelHeightToSet = GetRatioHeight(parentHeight);
                }
                break;
                #endregion

            #region RelativeToMaxParentOrChildren

            case DimensionUnitType.RelativeToMaxParentOrChildren:
                {
                    float parentBasedSize = parentHeight;

                    float childrenBasedSize = 0;
                    if (this.mContainedObjectAsIpso != null)
                    {
                        childrenBasedSize = GetMaxCellHeight(considerWrappedStacked, childrenBasedSize);
                    }
                    _measuredLineHeight = childrenBasedSize;
                    // mHeight acts as padding on children, matching RelativeToChildren behavior
                    childrenBasedSize += mHeight;

                    pixelHeightToSet = System.Math.Max(parentBasedSize, childrenBasedSize);
                }
                break;

            #endregion
        }


        pixelHeightToSet = ClampToMinMax(pixelHeightToSet, _minHeight, _maxHeight);

        RequiredContainedObject.Height = pixelHeightToSet;
    }

    /// <summary>
    /// The unclamped width a <see cref="DimensionUnitType.Ratio"/> width takes from the space its siblings leave in
    /// <paramref name="parentWidth"/>. Reads the siblings as they are now.
    /// </summary>
    private float GetRatioWidth(float parentWidth)
    {
        // A negative ratio takes no space, like 0.
        if (this.Width <= 0)
        {
            return 0;
        }
        else if (GetIfParentIsAutoGrid())
        {
            // Each grid child has its own cell, so there are no siblings to share it with.
            return parentWidth;
        }
        else
        {
            var widthToSplit = parentWidth;

            var numberOfVisibleChildren = 0;

            var ratioSiblings = GetLayoutSiblings(out bool onlyParentless);

            if (ratioSiblings != null)
            {
                for (int i = 0; i < ratioSiblings.Count; i++)
                {
                    var child = ratioSiblings[i];
                    if (child != this && IsLayoutSibling(child, onlyParentless) && child is GraphicalUiElement gue && gue.Visible)
                    {
                        if (gue.WidthUnits == DimensionUnitType.Absolute)
                        {
                            widthToSplit -= gue.Width;
                        }
                        else if (gue.WidthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
                        {
                            widthToSplit -= gue.Width * GlobalFontScale;
                        }
                        else if (gue.WidthUnits == DimensionUnitType.RelativeToParent)
                        {
                            var childAbsoluteWidth = parentWidth + gue.Width;
                            widthToSplit -= childAbsoluteWidth;
                        }
                        else if (gue.WidthUnits == DimensionUnitType.PercentageOfParent)
                        {
                            var childAbsoluteWidth = (parentWidth * gue.Width) / 100f;
                            widthToSplit -= childAbsoluteWidth;
                        }
                        // this depends on the sibling being updated before this:
                        else if (gue.WidthUnits == DimensionUnitType.RelativeToChildren ||
                            gue.WidthUnits == DimensionUnitType.PercentageOfOtherDimension ||
                            gue.WidthUnits == DimensionUnitType.PercentageOfSourceFile ||
                            gue.WidthUnits == DimensionUnitType.MaintainFileAspectRatio ||
                            gue.WidthUnits == DimensionUnitType.ScreenPixel ||
                            gue.WidthUnits == DimensionUnitType.RelativeToMaxParentOrChildren)
                        {
                            var childAbsoluteWidth = gue.AbsoluteWidth;
                            widthToSplit -= childAbsoluteWidth;
                        }
                        numberOfVisibleChildren++;
                    }
                }
            }

            if (EffectiveParentGue is GraphicalUiElement parentGue && parentGue.ChildrenLayout == ChildrenLayout.LeftToRightStack && parentGue.StackSpacing != 0)
            {
                var numberOfSpaces = numberOfVisibleChildren;

                widthToSplit -= numberOfSpaces * parentGue.StackSpacing;
            }

            // Siblings that overflow the parent leave no space, not negative space.
            widthToSplit = System.Math.Max(0, widthToSplit);

            float totalRatio = 0;
            if (ratioSiblings != null)
            {
                for (int i = 0; i < ratioSiblings.Count; i++)
                {
                    var child = ratioSiblings[i];
                    if (IsLayoutSibling(child, onlyParentless) && child is GraphicalUiElement gue && gue.WidthUnits == DimensionUnitType.Ratio && gue.Visible)
                    {
                        totalRatio += System.Math.Max(0, gue.Width);
                    }
                }
            }
            if (totalRatio > 0)
            {
                return widthToSplit * (this.Width / totalRatio);

            }
            else
            {
                return widthToSplit;
            }
        }
    }

    /// <summary>
    /// The unclamped height a <see cref="DimensionUnitType.Ratio"/> height takes from the space its siblings leave in
    /// <paramref name="parentHeight"/>. Reads the siblings as they are now.
    /// </summary>
    private float GetRatioHeight(float parentHeight)
    {
        // A negative ratio takes no space, like 0.
        if (this.Height <= 0)
        {
            return 0;
        }
        else if (GetIfParentIsAutoGrid())
        {
            // Each grid child has its own cell, so there are no siblings to share it with.
            return parentHeight;
        }
        else
        {
            var heightToSplit = parentHeight;

            var numberOfVisibleChildren = 0;

            var ratioSiblings = GetLayoutSiblings(out bool onlyParentless);

            if (ratioSiblings != null)
            {
                for (int i = 0; i < ratioSiblings.Count; i++)
                {
                    var child = ratioSiblings[i];
                    if (child != this && IsLayoutSibling(child, onlyParentless) && child is GraphicalUiElement gue && gue.Visible)
                    {
                        if (gue.HeightUnits == DimensionUnitType.Absolute)
                        {
                            heightToSplit -= gue.Height;
                        }
                        else if (gue.HeightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
                        {
                            heightToSplit -= gue.Height * GlobalFontScale;
                        }
                        else if (gue.HeightUnits == DimensionUnitType.RelativeToParent)
                        {
                            var childAbsoluteWidth = parentHeight + gue.Height;
                            heightToSplit -= childAbsoluteWidth;
                        }
                        else if (gue.HeightUnits == DimensionUnitType.PercentageOfParent)
                        {
                            var childAbsoluteWidth = (parentHeight * gue.Height) / 100f;
                            heightToSplit -= childAbsoluteWidth;
                        }
                        // this depends on the sibling being updated before this:
                        else if (gue.HeightUnits == DimensionUnitType.RelativeToChildren ||
                            gue.HeightUnits == DimensionUnitType.PercentageOfOtherDimension ||
                            gue.HeightUnits == DimensionUnitType.PercentageOfSourceFile ||
                            gue.HeightUnits == DimensionUnitType.MaintainFileAspectRatio ||
                            gue.HeightUnits == DimensionUnitType.ScreenPixel ||
                            gue.HeightUnits == DimensionUnitType.RelativeToMaxParentOrChildren)
                        {
                            var childAbsoluteWidth = gue.AbsoluteHeight;
                            heightToSplit -= childAbsoluteWidth;
                        }
                        numberOfVisibleChildren++;
                    }
                }
            }

            if (EffectiveParentGue is GraphicalUiElement parentGue && parentGue.ChildrenLayout == ChildrenLayout.TopToBottomStack && parentGue.StackSpacing != 0)
            {
                var numberOfSpaces = numberOfVisibleChildren;
                heightToSplit -= numberOfSpaces * parentGue.StackSpacing;
            }

            // Siblings that overflow the parent leave no space, not negative space.
            heightToSplit = System.Math.Max(0, heightToSplit);

            float totalRatio = 0;
            if (ratioSiblings != null)
            {
                for (int i = 0; i < ratioSiblings.Count; i++)
                {
                    var child = ratioSiblings[i];
                    if (IsLayoutSibling(child, onlyParentless) && child is GraphicalUiElement gue && gue.HeightUnits == DimensionUnitType.Ratio && gue.Visible)
                    {
                        totalRatio += System.Math.Max(0, gue.Height);
                    }
                }
            }
            if (totalRatio > 0)
            {
                return heightToSplit * (this.Height / totalRatio);
            }
            else
            {
                return heightToSplit;
            }
        }
    }

    // Min wins over max when they conflict.
    static float ClampToMinMax(float value, float? min, float? max)
    {
        if (value > max)
        {
            value = max.Value;
        }
        if (value < min)
        {
            value = min.Value;
        }
        return value;
    }

    // A zero, negative or non-finite aspect ratio (such as from an empty texture) cannot size the
    // other axis, so MaintainFileAspectRatio falls back as if the renderable had no aspect ratio.
    static bool IsUsableAspectRatio(float aspectRatio) => aspectRatio > 0 && !float.IsInfinity(aspectRatio);

    private float GetMaxCellHeight(bool considerWrappedStacked, float maxHeight)
    {
        float maxCellHeight = maxHeight;
        float lineHeight = maxHeight;
        bool hasCountedVisibleChild = false;
        for (int i = 0; i < Children!.Count; i++)
        {
            var element = Children[i];
            var childLayout = element.GetChildLayoutType(XOrY.Y, this);
            var considerChild = (childLayout == ChildType.Absolute || (considerWrappedStacked && childLayout == ChildType.StackedWrapped)) && element.IgnoredByParentSize == false;
            if (considerChild && element.Visible)
            {
                var elementHeight = element.GetRequiredParentHeight();

                // If the child uses RelativeToMaxParentOrChildren, its absolute height
                // includes max(parentHeight, childrenHeight). Using that here creates a
                // circular dependency where stale parent values ratchet upward. Instead,
                // use only the children-based size. Position offsets on the child are
                // intentionally not considered — RelativeToMaxParentOrChildren is designed
                // for siblings that fill their parent, not for positioned children.
                if (element.HeightUnits == DimensionUnitType.RelativeToMaxParentOrChildren &&
                    element.mContainedObjectAsIpso != null)
                {
                    elementHeight = ClampToMinMax(element.GetMaxCellHeight(considerWrappedStacked, 0) + element.mHeight,
                        element.MinHeight, element.MaxHeight);
                }

                if (this.ChildrenLayout == ChildrenLayout.TopToBottomStack)
                {
                    // Stack spacing is only added between visible children, so skip it
                    // for the first visible child in its line regardless of its index in Children.
                    var lineWithElement = lineHeight + elementHeight;
                    if (hasCountedVisibleChild)
                    {
                        lineWithElement += StackSpacing;
                    }

                    // A wrapping stack moves a child that crosses the max to a new line, so the size is
                    // its longest line (#5806). Lines break against the max, not the current size, so the
                    // measured size does not change where they break. A non-wrapping stack keeps the
                    // child in this line and the caller clamps the full size to the max.
                    if (WrapsChildren && hasCountedVisibleChild && lineWithElement > this.MaxHeight)
                    {
                        lineWithElement = elementHeight;
                    }
                    lineHeight = lineWithElement;
                    maxCellHeight = System.Math.Max(maxCellHeight, lineHeight);
                }
                else
                {
                    maxCellHeight = System.Math.Max(maxCellHeight, elementHeight);
                }
                hasCountedVisibleChild = true;
            }
        }

        return maxCellHeight;
    }

    public void UpdateWidth(float parentWidth, bool considerWrappedStacked)
    {
        float pixelWidthToSet = mWidth;

        switch (mWidthUnit)
        {

            #region AbsoluteMultipliedByFontScale

            case DimensionUnitType.AbsoluteMultipliedByFontScale:
                {
                    pixelWidthToSet *= GlobalFontScale;
                }
                break;
            #endregion

            #region ScreenPixel

            case DimensionUnitType.ScreenPixel:
                {
                    var effectiveManagers = this.EffectiveManagers;
                    if (effectiveManagers != null)
                    {
                        pixelWidthToSet /= effectiveManagers.Renderer.Camera.Zoom;
                    }
                }
                break;
            #endregion

            #region RelativeToChildren

            case DimensionUnitType.RelativeToChildren:
                {
                    float maxWidth = 0;

                    if (this.mContainedObjectAsIpso != null)
                    {
                        if (mContainedObjectAsIpso is IText asText)
                        {

                            // Sometimes this crashes in Skia.
                            // Not sure why, but I think it is some kind of internal error. We can tolerate it instead of blow up:
                            try
                            {
                                // It's possible that the text has itself wrapped, but the dimensions changed.
                                if (
                                    // Skia text doesn't have a wrapped text, but we can just check if the text itself is not null or empty
                                    //asText.WrappedText.Count > 0 &&
                                    !string.IsNullOrEmpty(asText.RawText) && asText.Width != null)
                                {
                                    // this could be either because it wrapped, or because the raw text
                                    // actually has newlines. Vic says - this difference could maybe be tested
                                    // but I'm not sure it's worth the extra code for the minor savings here, so just
                                    // set the wrap width to positive infinity and refresh the text
                                    asText.Width = null;
                                }

                                maxWidth = asText.WrappedTextWidth;
                            }
                            catch (BadImageFormatException)
                            {
                                // not sure why but let's tolerate:
                                // https://appcenter.ms/orgs/Mtn-Green-Engineering/apps/BioCheck-2/crashes/errors/738313670/overview
                                maxWidth = 64;

                                //        // It's possible that the text has itself wrapped, but the dimensions changed.
                                //        if (asText.WrappedText.Count > 0 &&
                                //            (asText.Width != 0 && float.IsPositiveInfinity(asText.Width) == false))
                                //        {
                                //            // this could be either because it wrapped, or because the raw text
                                //            // actually has newlines. Vic says - this difference could maybe be tested
                                //            // but I'm not sure it's worth the extra code for the minor savings here, so just
                                //            // set the wrap width to positive infinity and refresh the text
                                //            asText.Width = float.PositiveInfinity;
                                //        }

                                //        maxWidth = asText.WrappedTextWidth;
                            }
                        }

                        float maxCellWidth = GetMaxCellWidth(considerWrappedStacked, maxWidth);
                        _measuredLineWidth = maxCellWidth;

                        maxWidth = maxCellWidth;

                        if (this.ChildrenLayout == ChildrenLayout.AutoGridHorizontal || this.ChildrenLayout == ChildrenLayout.AutoGridVertical)
                        {
                            // Cell counts below 1 are placed as 1 (see GetCellDimensions), so size them the same way.
                            var numberOfHorizontalCells = System.Math.Max(1, this.AutoGridHorizontalCells);

                            // If auto grid vertical, then it can expand horizontally
                            if (ChildrenLayout == ChildrenLayout.AutoGridVertical)
                            {
                                var rowCount = System.Math.Max(1, this.AutoGridVerticalCells);
                                var requiredColumnCount = (int)Math.Ceiling((float)GetVisibleChildCount() / rowCount);
                                numberOfHorizontalCells = System.Math.Max(numberOfHorizontalCells, requiredColumnCount);
                            }
                            // We got the largest size for one child, but that child must be contained within a cell, and all cells must be
                            // at least that same size, so we multiply the size by the number of cells wide
                            maxWidth = maxCellWidth * numberOfHorizontalCells + (numberOfHorizontalCells-1) * StackSpacing;
                        }
                    }

                    pixelWidthToSet = maxWidth + mWidth;
                }
                break;
            #endregion

            #region Percentage (of parent)

            case DimensionUnitType.PercentageOfParent:
                {
                    pixelWidthToSet = parentWidth * mWidth / 100.0f;
                }
                break;
            #endregion

            #region PercentageOfSourceFile

            case DimensionUnitType.PercentageOfSourceFile:
                {
                    bool wasSet = false;

                    if (mTextureWidth > 0 && TextureAddress != TextureAddress.EntireTexture)
                    {
                        pixelWidthToSet = mTextureWidth * mWidth / 100.0f;
                        wasSet = true;
                    }

                    if (mContainedObjectAsIpso is ITextureCoordinate iTextureCoordinate)
                    {
                        var width = iTextureCoordinate.TextureWidth;
                        if (width != null)
                        {
                            pixelWidthToSet = width.Value * mWidth / 100.0f;
                            wasSet = true;
                        }

                        // If the address is dimension based, then that means texture coords depend on dimension...but we
                        // can't make dimension based on texture coords as that would cause a circular reference
                        if (iTextureCoordinate.SourceRectangle.HasValue && mTextureAddress != TextureAddress.DimensionsBased)
                        {
                            pixelWidthToSet = iTextureCoordinate.SourceRectangle.Value.Width * mWidth / 100.0f;
                            wasSet = true;
                        }
                    }

                    if (!wasSet)
                    {
                        pixelWidthToSet = 64 * mWidth / 100.0f;
                    }
                }
                break;
            #endregion

            #region MaintainFileAspectRatio

            case DimensionUnitType.MaintainFileAspectRatio:
                {
                    bool wasSet = false;

                    if (mContainedObjectAsIpso is IAspectRatio aspectRatioObject && IsUsableAspectRatio(aspectRatioObject.AspectRatio))
                    {
                        // mWidth is a percent where 100 means maintain aspect ratio
                        pixelWidthToSet = AbsoluteHeight * aspectRatioObject.AspectRatio * (mWidth / 100.0f);
                        wasSet = true;

                        if (wasSet && mContainedObjectAsIpso is ITextureCoordinate iTextureCoordinate)
                        {
                            if (iTextureCoordinate.SourceRectangle.HasValue && mTextureAddress != TextureAddress.DimensionsBased)
                            {
                                var scale = 1f;
                                if (iTextureCoordinate.SourceRectangle.Value.Height != 0)
                                {
                                    scale = AbsoluteHeight / iTextureCoordinate.SourceRectangle.Value.Height;
                                }
                                pixelWidthToSet = iTextureCoordinate.SourceRectangle.Value.Width * scale * mWidth / 100.0f;
                            }
                        }
                    }
                    if (!wasSet)
                    {
                        pixelWidthToSet = 64 * mWidth / 100.0f;
                    }
                }
                break;
            #endregion

            #region RelativeToParent (in pixels)

            case DimensionUnitType.RelativeToParent:
                {
                    pixelWidthToSet = parentWidth + mWidth;
                }
                break;
            #endregion

            #region PercentageOfOtherDimension

            case DimensionUnitType.PercentageOfOtherDimension:
                {
                    pixelWidthToSet = RequiredContainedObject.Height * mWidth / 100.0f;
                }
                break;
            #endregion

            #region Ratio

            case DimensionUnitType.Ratio:
                {
                    pixelWidthToSet = GetRatioWidth(parentWidth);
                }
                break;
                #endregion

            #region RelativeToMaxParentOrChildren

            case DimensionUnitType.RelativeToMaxParentOrChildren:
                {
                    float parentBasedSize = parentWidth;

                    float childrenBasedSize = 0;
                    if (this.mContainedObjectAsIpso != null)
                    {
                        childrenBasedSize = GetMaxCellWidth(considerWrappedStacked, childrenBasedSize);
                    }
                    _measuredLineWidth = childrenBasedSize;
                    // mWidth acts as padding on children, matching RelativeToChildren behavior
                    childrenBasedSize += mWidth;

                    pixelWidthToSet = System.Math.Max(parentBasedSize, childrenBasedSize);
                }
                break;

            #endregion
        }


        pixelWidthToSet = ClampToMinMax(pixelWidthToSet, _minWidth, _maxWidth);

        RequiredContainedObject.Width = pixelWidthToSet;


    }

    private float GetMaxCellWidth(bool considerWrappedStacked, float maxWidth)
    {
        float maxCellWidth = maxWidth;
        float lineWidth = maxWidth;
        bool hasCountedVisibleChild = false;
        for (int i = 0; i < this.Children!.Count; i++)
        {
            var element = this.Children[i];
            var childLayout = element.GetChildLayoutType(XOrY.X, this);
            var considerChild = (childLayout == ChildType.Absolute || (considerWrappedStacked && childLayout == ChildType.StackedWrapped)) && element.IgnoredByParentSize == false;

            if (considerChild && element.Visible)
            {
                var elementWidth = element.GetRequiredParentWidth();

                // If the child uses RelativeToMaxParentOrChildren, its absolute width
                // includes max(parentWidth, childrenWidth). Using that here creates a
                // circular dependency where stale parent values ratchet upward. Instead,
                // use only the children-based size. Position offsets on the child are
                // intentionally not considered — RelativeToMaxParentOrChildren is designed
                // for siblings that fill their parent, not for positioned children.
                if (element.WidthUnits == DimensionUnitType.RelativeToMaxParentOrChildren &&
                    element.mContainedObjectAsIpso != null)
                {
                    elementWidth = ClampToMinMax(element.GetMaxCellWidth(considerWrappedStacked, 0) + element.mWidth,
                        element.MinWidth, element.MaxWidth);
                }

                if (this.ChildrenLayout == ChildrenLayout.LeftToRightStack)
                {
                    // Stack spacing is only added between visible children, so skip it
                    // for the first visible child in its line regardless of its index in Children.
                    var lineWithElement = lineWidth + elementWidth;
                    if (hasCountedVisibleChild)
                    {
                        lineWithElement += StackSpacing;
                    }

                    // A wrapping stack moves a child that crosses the max to a new line, so the size is
                    // its longest line (#5806). Lines break against the max, not the current size, so the
                    // measured size does not change where they break. A non-wrapping stack keeps the
                    // child in this line and the caller clamps the full size to the max.
                    if (WrapsChildren && hasCountedVisibleChild && lineWithElement > this.MaxWidth)
                    {
                        lineWithElement = elementWidth;
                    }
                    lineWidth = lineWithElement;
                    maxCellWidth = System.Math.Max(maxCellWidth, lineWidth);
                }
                else
                {
                    maxCellWidth = System.Math.Max(maxCellWidth, elementWidth);
                }
                hasCountedVisibleChild = true;
            }
        }

        return maxCellWidth;
    }
}
