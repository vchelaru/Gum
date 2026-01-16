using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    public partial class GraphicalUiElement
    {
        #region Width/Height

        private void UpdateDimensions(float parentWidth, float parentHeight, XOrY? xOrY, bool considerWrappedStacked)
        {
            // special case - if the user has set both values to depend on the other value, we don't want to have an infinite recursion so we'll just apply the width and height values as pixel values.
            // This really doesn't make much sense but...the alternative would be an object that may grow or shrink infinitely, which may cause lots of other problems:
            if ((mWidthUnit == DimensionUnitType.PercentageOfOtherDimension && mHeightUnit == DimensionUnitType.PercentageOfOtherDimension) ||
                (mWidthUnit == DimensionUnitType.MaintainFileAspectRatio && mHeightUnit == DimensionUnitType.MaintainFileAspectRatio)
                )
            {
                mContainedObjectAsIpso.Width = mWidth;
                mContainedObjectAsIpso.Height = mHeight;
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

        public void UpdateHeight(float parentHeight, bool considerWrappedStacked)
        {
            float pixelHeightToSet = mHeight;

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

                            if (useFixedStackChildrenSize && this.ChildrenLayout == ChildrenLayout.TopToBottomStack && this.Children.Count > 1)
                            {
                                var element = Children[0];

                                maxHeight = element.GetRequiredParentHeight();
                                var elementHeight = element.GetAbsoluteHeight();
                                maxHeight += (StackSpacing + elementHeight) * (Children.Count - 1);
                            }
                            else
                            {
                                float maxCellHeight = GetMaxCellHeight(considerWrappedStacked, maxHeight);

                                maxHeight = maxCellHeight;

                                if (this.ChildrenLayout == ChildrenLayout.AutoGridHorizontal || this.ChildrenLayout == ChildrenLayout.AutoGridVertical)
                                {
                                    var numberOfVerticalCells =
                                        this.AutoGridVerticalCells;

                                    if (this.AutoGridHorizontalCells > 0 &&
                                        this.ChildrenLayout == ChildrenLayout.AutoGridHorizontal)
                                    {
                                        var requiredRowCount = (int)Math.Ceiling((float)Children.Count / this.AutoGridHorizontalCells);
                                        numberOfVerticalCells = System.Math.Max(numberOfVerticalCells, requiredRowCount);
                                    }

                                    // We got the largest size for one child, but that child must be contained within a cell, and all cells must be
                                    // at least that same size, so we multiply the size by the number of cells tall
                                    maxHeight = maxCellHeight * numberOfVerticalCells + (numberOfVerticalCells - 1) * StackSpacing;
                                }

                            }
                        }
                        else
                        {
                            for (int i = 0; i < mWhatThisContains.Count; i++)
                            {
                                var element = mWhatThisContains[i];
                                var childLayout = element.GetChildLayoutType(XOrY.Y, this);
                                var considerChild = (childLayout == ChildType.Absolute || (considerWrappedStacked && childLayout == ChildType.StackedWrapped)) && element.IgnoredByParentSize == false;

                                if (considerChild && element.Visible)
                                {
                                    var elementHeight = element.GetRequiredParentHeight();
                                    if (this.ChildrenLayout == ChildrenLayout.TopToBottomStack)
                                    {
                                        // The first item in the stack doesn't consider the stack spacing, but all subsequent ones do:
                                        if (i != 0)
                                        {
                                            maxHeight += StackSpacing;
                                        }
                                        maxHeight += elementHeight;
                                    }
                                    else
                                    {
                                        maxHeight = System.Math.Max(maxHeight, elementHeight);
                                    }
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


                        if (mContainedObjectAsIpso is IAspectRatio aspectRatioObject)
                        {
                            //if(sprite.AtlasedTexture != null)
                            //{
                            //    throw new NotImplementedException();
                            //}
                            //else 
                            pixelHeightToSet = GetAbsoluteWidth() * (mHeight / 100.0f) / aspectRatioObject.AspectRatio;
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
                                        scale = GetAbsoluteWidth() / textureCoordinate.SourceRectangle.Value.Width;
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
                        pixelHeightToSet = mContainedObjectAsIpso.Width * mHeight / 100.0f;
                    }
                    break;
                #endregion

                #region Ratio
                case DimensionUnitType.Ratio:
                    {
                        if (this.Height == 0)
                        {
                            pixelHeightToSet = 0;
                        }
                        else
                        {
                            var heightToSplit = parentHeight;

                            var numberOfVisibleChildren = 0;

                            if (_parent != null)
                            {
                                for (int i = 0; i < _parent.Children.Count; i++)
                                {
                                    var child = _parent.Children[i];
                                    if (child != this && child is GraphicalUiElement gue && gue.Visible)
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
                                            gue.HeightUnits == DimensionUnitType.ScreenPixel)
                                        {
                                            var childAbsoluteWidth = gue.GetAbsoluteHeight();
                                            heightToSplit -= childAbsoluteWidth;
                                        }
                                        numberOfVisibleChildren++;
                                    }
                                }
                            }

                            if (_parent is GraphicalUiElement parentGue && parentGue.ChildrenLayout == ChildrenLayout.TopToBottomStack && parentGue.StackSpacing != 0)
                            {
                                var numberOfSpaces = numberOfVisibleChildren;
                                heightToSplit -= numberOfSpaces * parentGue.StackSpacing;
                            }

                            float totalRatio = 0;
                            if (_parent != null)
                            {
                                for (int i = 0; i < _parent.Children.Count; i++)
                                {
                                    var child = _parent.Children[i];
                                    if (child is GraphicalUiElement gue && gue.HeightUnits == DimensionUnitType.Ratio && gue.Visible)
                                    {
                                        totalRatio += gue.Height;
                                    }
                                }
                            }
                            if (totalRatio > 0)
                            {
                                pixelHeightToSet = heightToSplit * (this.Height / totalRatio);
                            }
                            else
                            {
                                pixelHeightToSet = heightToSplit;
                            }
                        }
                    }
                    break;
                    #endregion
            }


            if (pixelHeightToSet > _maxHeight)
            {
                pixelHeightToSet = _maxHeight.Value;
            }
            if (pixelHeightToSet < _minHeight)
            {
                pixelHeightToSet = _minHeight.Value;
            }

            mContainedObjectAsIpso.Height = pixelHeightToSet;
        }

        private float GetMaxCellHeight(bool considerWrappedStacked, float maxHeight)
        {
            float maxCellHeight = maxHeight;
            for (int i = 0; i < Children!.Count; i++)
            {
                var element = Children[i];
                var childLayout = element.GetChildLayoutType(XOrY.Y, this);
                var considerChild = (childLayout == ChildType.Absolute || (considerWrappedStacked && childLayout == ChildType.StackedWrapped)) && element.IgnoredByParentSize == false;
                if (considerChild && element.Visible)
                {
                    var elementHeight = element.GetRequiredParentHeight();

                    if (this.ChildrenLayout == ChildrenLayout.TopToBottomStack)
                    {
                        // The first item in the stack doesn't consider the stack spacing, but all subsequent ones do:
                        if (i != 0)
                        {
                            var maxHeightWithSpacing = maxCellHeight + StackSpacing;

                            if (maxHeightWithSpacing > this.MaxHeight)
                            {
                                // don't do anything, we can't expand any further so leave the height wherever it was before
                                // because this item should wrap:
                                //maxCellHeight = this.MaxHeight.Value;
                                break;
                            }
                            else
                            {
                                maxCellHeight = maxHeightWithSpacing;
                            }
                        }

                        var maxHeightWithElement = maxCellHeight + elementHeight;
                        if (maxHeightWithElement > this.MaxHeight)
                        {
                            // don't do anything, we can't expand any further so leave the height wherever it was before
                            // because this item should wrap:
                            //maxCellHeight = this.MaxHeight.Value;
                            break;
                        }
                        else
                        {
                            maxCellHeight = maxHeightWithElement;
                        }
                    }
                    else
                    {
                        maxCellHeight = System.Math.Max(maxCellHeight, elementHeight);
                    }
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

                        List<GraphicalUiElement> childrenToUse = mWhatThisContains;

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

                            maxWidth = maxCellWidth;

                            if (this.ChildrenLayout == ChildrenLayout.AutoGridHorizontal || this.ChildrenLayout == ChildrenLayout.AutoGridVertical)
                            {
                                var numberOfHorizontalCells =
                                    this.AutoGridHorizontalCells;

                                if (this.AutoGridVerticalCells > 0 &&
                                    // If auto grid vertical, then it can expand horizontally
                                    ChildrenLayout == ChildrenLayout.AutoGridVertical)
                                {
                                    var requiredColumnCount = (int)Math.Ceiling((float)Children.Count / this.autoGridVerticalCells);
                                    numberOfHorizontalCells = System.Math.Max(numberOfHorizontalCells, requiredColumnCount);
                                }
                                // We got the largest size for one child, but that child must be contained within a cell, and all cells must be
                                // at least that same size, so we multiply the size by the number of cells wide
                                maxWidth = maxCellWidth * numberOfHorizontalCells + (numberOfHorizontalCells - 1) * StackSpacing;
                            }
                        }
                        else
                        {
                            for (int i = 0; i < mWhatThisContains.Count; i++)
                            {
                                var element = mWhatThisContains[i];
                                var childLayout = element.GetChildLayoutType(XOrY.X, this);
                                var considerChild = (childLayout == ChildType.Absolute || (considerWrappedStacked && childLayout == ChildType.StackedWrapped)) && element.IgnoredByParentSize == false;

                                if (considerChild && element.Visible)
                                {
                                    var elementWidth = element.GetRequiredParentWidth();

                                    if (this.ChildrenLayout == ChildrenLayout.LeftToRightStack)
                                    {
                                        // The first item in the stack doesn't consider the stack spacing, but all subsequent ones do:
                                        if (i != 0)
                                        {
                                            maxWidth += StackSpacing;
                                        }
                                        maxWidth += elementWidth;
                                    }
                                    else
                                    {
                                        maxWidth = System.Math.Max(maxWidth, elementWidth);
                                    }
                                }
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

                        if (mContainedObjectAsIpso is IAspectRatio aspectRatioObject)
                        {
                            // mWidth is a percent where 100 means maintain aspect ratio
                            pixelWidthToSet = GetAbsoluteHeight() * aspectRatioObject.AspectRatio * (mWidth / 100.0f);
                            wasSet = true;

                            if (wasSet && mContainedObjectAsIpso is ITextureCoordinate iTextureCoordinate)
                            {
                                if (iTextureCoordinate.SourceRectangle.HasValue && mTextureAddress != TextureAddress.DimensionsBased)
                                {
                                    var scale = 1f;
                                    if (iTextureCoordinate.SourceRectangle.Value.Height != 0)
                                    {
                                        scale = GetAbsoluteHeight() / iTextureCoordinate.SourceRectangle.Value.Height;
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
                        pixelWidthToSet = mContainedObjectAsIpso.Height * mWidth / 100.0f;
                    }
                    break;
                #endregion

                #region Ratio

                case DimensionUnitType.Ratio:
                    {
                        if (this.Width == 0)
                        {
                            pixelWidthToSet = 0;
                        }
                        else
                        {
                            var widthToSplit = parentWidth;

                            var numberOfVisibleChildren = 0;

                            if (_parent != null)
                            {
                                for (int i = 0; i < _parent.Children.Count; i++)
                                {
                                    var child = _parent.Children[i];
                                    if (child != this && child is GraphicalUiElement gue && gue.Visible)
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
                                            gue.WidthUnits == DimensionUnitType.ScreenPixel)
                                        {
                                            var childAbsoluteWidth = gue.GetAbsoluteWidth();
                                            widthToSplit -= childAbsoluteWidth;
                                        }
                                        numberOfVisibleChildren++;
                                    }
                                }
                            }

                            if (_parent is GraphicalUiElement parentGue && parentGue.ChildrenLayout == ChildrenLayout.LeftToRightStack && parentGue.StackSpacing != 0)
                            {
                                var numberOfSpaces = numberOfVisibleChildren;

                                widthToSplit -= numberOfSpaces * parentGue.StackSpacing;
                            }

                            float totalRatio = 0;
                            if (_parent != null)
                            {
                                for (int i = 0; i < _parent.Children.Count; i++)
                                {
                                    var child = _parent.Children[i];
                                    if (child is GraphicalUiElement gue && gue.WidthUnits == DimensionUnitType.Ratio && gue.Visible)
                                    {
                                        totalRatio += gue.Width;
                                    }
                                }
                            }
                            if (totalRatio > 0)
                            {
                                pixelWidthToSet = widthToSplit * (this.Width / totalRatio);

                            }
                            else
                            {
                                pixelWidthToSet = widthToSplit;
                            }
                        }
                    }
                    break;
                    #endregion
            }


            if (pixelWidthToSet > _maxWidth)
            {
                pixelWidthToSet = _maxWidth.Value;
            }
            if (pixelWidthToSet < _minWidth)
            {
                pixelWidthToSet = _minWidth.Value;
            }

            mContainedObjectAsIpso.Width = pixelWidthToSet;


        }

        private float GetMaxCellWidth(bool considerWrappedStacked, float maxWidth)
        {
            float maxCellWidth = maxWidth;
            for (int i = 0; i < this.Children!.Count; i++)
            {
                var element = this.Children[i];
                var childLayout = element.GetChildLayoutType(XOrY.X, this);
                var considerChild = (childLayout == ChildType.Absolute || (considerWrappedStacked && childLayout == ChildType.StackedWrapped)) && element.IgnoredByParentSize == false;

                if (considerChild && element.Visible)
                {
                    var elementWidth = element.GetRequiredParentWidth();

                    if (this.ChildrenLayout == ChildrenLayout.LeftToRightStack)
                    {
                        // The first item in the stack doesn't consider the stack spacing, but all subsequent ones do:
                        if (i != 0)
                        {
                            var maxWidthWithSpacing = maxCellWidth + StackSpacing;

                            if (maxWidthWithSpacing > this.MaxWidth)
                            {
                                // don't do anything, we can't expand any further so leave the width wherever it was before
                                // because this item should wrap:
                                //maxCellWidth = this.MaxWidth.Value;
                                break;
                            }
                            else
                            {
                                maxCellWidth = maxWidthWithSpacing;
                            }
                        }

                        var maxWidthWithElement = maxCellWidth + elementWidth;
                        if (maxWidthWithElement > this.MaxWidth)
                        {
                            // don't do anything, we can't expand any further so leave the width wherever it was before
                            // because this item should wrap:
                            //maxCellWidth = this.MaxWidth.Value;
                            break;
                        }
                        else
                        {
                            maxCellWidth = maxWidthWithElement;
                        }
                    }
                    else
                    {
                        maxCellWidth = System.Math.Max(maxCellWidth, elementWidth);
                    }
                }
            }

            return maxCellWidth;
        }

        float GetRequiredParentWidth()
        {
            var effectiveParent = this.EffectiveParentGue;
            if (effectiveParent != null && effectiveParent.ChildrenLayout == ChildrenLayout.TopToBottomStack && effectiveParent.WrapsChildren)
            {
                var asIpso = this as IPositionedSizedObject;
                return asIpso.X + asIpso.Width;
            }
            else
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
        }

        float GetRequiredParentHeight()
        {
            var effectiveParent = this.EffectiveParentGue;
            if (effectiveParent != null && effectiveParent.ChildrenLayout == ChildrenLayout.LeftToRightStack && effectiveParent.WrapsChildren)
            {
                var asIpso = this as IPositionedSizedObject;
                return asIpso.Y + asIpso.Height;
            }
            else
            {
                float positionValue = mY;

                // This GUE hasn't been set yet so it can't give
                // valid widths/heights
                if (this.mContainedObjectAsIpso == null)
                {
                    return 0;
                }
                float smallEdge = positionValue;

                var units = mYUnits;
                if (units == GeneralUnitType.PixelsFromMiddleInverted)
                {
                    smallEdge *= -1;
                }

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

                    if (Parent.Children?.Count > setCellCount)
                    {
                        if (Parent.ChildrenLayout == ChildrenLayout.AutoGridVertical)
                        {
                            // If stacking vertically, the number of rows (vertical cell count) depends on the children count
                            // if the parent's size depends on its children
                            if (Parent.HeightUnits == DimensionUnitType.RelativeToChildren)
                            {
                                effectiveVerticalCells = (int)System.Math.Ceiling((float)Parent.Children.Count / effectiveHorizontalCells);
                            }
                        }
                        else
                        {
                            if (Parent.WidthUnits == DimensionUnitType.RelativeToChildren)
                            {
                                effectiveHorizontalCells = (int)System.Math.Ceiling((float)Parent.Children.Count / effectiveVerticalCells);
                            }
                        }
                    }

                    parentWidth = (Parent.GetAbsoluteWidth() - (effectiveHorizontalCells - 1) * Parent.StackSpacing) / effectiveHorizontalCells;

                    parentHeight = (Parent.GetAbsoluteHeight() - (effectiveVerticalCells - 1) * Parent.StackSpacing) / effectiveVerticalCells;
                }
                else
                {
                    parentWidth = Parent.GetAbsoluteWidth();
                    parentHeight = Parent.GetAbsoluteHeight();
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
                width = (int)(mContainedObjectAsIpso.Width / mTextureWidthScale);
            }

            int height = 0;

            if (mTextureHeightScale != 0)
            {
                height = (int)(mContainedObjectAsIpso.Height / mTextureHeightScale);
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
                units == GeneralUnitType.PixelsFromMiddleInverted)
            {
                // use the full width
                float abs1 = System.Math.Abs(smallEdge);
                float abs2 = System.Math.Abs(bigEdge);

                dimensionToReturn = 2 * System.Math.Max(abs1, abs2);
            }
            else if (units == GeneralUnitType.PixelsFromLarge)
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
                this.HeightUnits.GetDependencyType() == HierarchyDependencyType.DependsOnChildren);
        }

        #endregion
    }
}
