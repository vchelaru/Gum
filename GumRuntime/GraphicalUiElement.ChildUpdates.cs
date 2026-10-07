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

    #region Get Child Layout Type

    ChildType GetChildLayoutType(GraphicalUiElement parent)
    {
        var doesParentWrapStack = parent.WrapsChildren && (parent.ChildrenLayout == ChildrenLayout.LeftToRightStack || parent.ChildrenLayout == ChildrenLayout.TopToBottomStack);

        var parentWidthDependencyType = parent.WidthUnits.GetDependencyType();
        var parentHeightDependencyType = parent.HeightUnits.GetDependencyType();

        // RelativeToMaxParentOrChildren also depends on children, so its size isn't final until they are measured.
        var isParentWidthNoDependencyOrOnParent = (parentWidthDependencyType == HierarchyDependencyType.NoDependency || parentWidthDependencyType == HierarchyDependencyType.DependsOnParent) &&
            parent.WidthUnits != DimensionUnitType.RelativeToMaxParentOrChildren;
        var isParentHeightNoDependencyOrOnParent = (parentHeightDependencyType == HierarchyDependencyType.NoDependency || parentHeightDependencyType == HierarchyDependencyType.DependsOnParent) &&
            parent.HeightUnits != DimensionUnitType.RelativeToMaxParentOrChildren;

        // In a wrapping stack the line, not the parent, is the parent for cross-axis position (#5802),
        // so a position measured from it does not wait on a parent sized by its children.
        var wrappedLineAxis = GetWrappedLineAxis(parent);
        var isXPositionedFromLine = wrappedLineAxis == XOrY.X && IsPositionedFromWrappedLineSize(XOrY.X);
        var isYPositionedFromLine = wrappedLineAxis == XOrY.Y && IsPositionedFromWrappedLineSize(XOrY.Y);

        var isAbsolute = (mWidthUnit.GetDependencyType() != HierarchyDependencyType.DependsOnParent || isParentWidthNoDependencyOrOnParent) &&
                        (mHeightUnit.GetDependencyType() != HierarchyDependencyType.DependsOnParent || isParentHeightNoDependencyOrOnParent) &&
                        (mWidthUnit.GetDependencyType() != HierarchyDependencyType.DependsOnSiblings) &&
                        (mHeightUnit.GetDependencyType() != HierarchyDependencyType.DependsOnSiblings) &&

            (mXUnits == GeneralUnitType.PixelsFromSmall || isXPositionedFromLine ||
             (mXUnits == GeneralUnitType.PixelsFromMiddle && isParentWidthNoDependencyOrOnParent) ||
             (mXUnits == GeneralUnitType.PixelsFromLarge && isParentWidthNoDependencyOrOnParent) ||
#pragma warning disable CS0618 // PixelsFromMiddleInverted is obsolete but still loads from older projects
             (mXUnits == GeneralUnitType.PixelsFromMiddleInverted && isParentWidthNoDependencyOrOnParent)) &&
#pragma warning restore CS0618

            (mYUnits == GeneralUnitType.PixelsFromSmall || isYPositionedFromLine ||
             (mYUnits == GeneralUnitType.PixelsFromMiddle && isParentHeightNoDependencyOrOnParent) ||
             (mYUnits == GeneralUnitType.PixelsFromLarge && isParentHeightNoDependencyOrOnParent) ||
#pragma warning disable CS0618 // PixelsFromMiddleInverted is obsolete but still loads from older projects
             (mYUnits == GeneralUnitType.PixelsFromMiddleInverted && isParentHeightNoDependencyOrOnParent) ||
#pragma warning restore CS0618
             // A non-Text parent's baseline is its bottom edge, so this depends on parent height like PixelsFromLarge.
             (mYUnits == GeneralUnitType.PixelsFromBaseline && isParentHeightNoDependencyOrOnParent));

        if (doesParentWrapStack)
        {
            return isAbsolute ? ChildType.StackedWrapped : ChildType.Relative;
        }
        else
        {
            return isAbsolute ? ChildType.Absolute : ChildType.Relative;
        }
    }

    ChildType GetChildLayoutType(XOrY xOrY, GraphicalUiElement parent)
    {
        bool isAbsolute;
        var doesParentWrapStack = parent.WrapsChildren && (parent.ChildrenLayout == ChildrenLayout.LeftToRightStack || parent.ChildrenLayout == ChildrenLayout.TopToBottomStack);

        if (xOrY == XOrY.X)
        {
            var widthUnitDependencyType = mWidthUnit.GetDependencyType();
            // RelativeToMaxParentOrChildren can compute a meaningful children-based size
            // without the parent, so treat it as Absolute for parent sizing purposes.
            // A Ratio width is sized from the parent's remaining space, so it can't size the parent either.
            var isNotParentDependent = (widthUnitDependencyType != HierarchyDependencyType.DependsOnParent &&
                    widthUnitDependencyType != HierarchyDependencyType.DependsOnSiblings) ||
                this.WidthUnits.GetDependencyType() == HierarchyDependencyType.NoDependency ||
                mWidthUnit == DimensionUnitType.RelativeToMaxParentOrChildren;
            var isPositionedFromLine = GetWrappedLineAxis(parent) == XOrY.X && IsPositionedFromWrappedLineSize(XOrY.X);
            isAbsolute = isNotParentDependent &&
                (isPositionedFromLine || mXUnits == GeneralUnitType.PixelsFromLarge || mXUnits == GeneralUnitType.PixelsFromMiddle ||
#pragma warning disable CS0618 // PixelsFromMiddleInverted is obsolete but still loads from older projects
                    mXUnits == GeneralUnitType.PixelsFromSmall || mXUnits == GeneralUnitType.PixelsFromMiddleInverted);
#pragma warning restore CS0618

        }
        else // Y
        {
            var heightUnitDependencyType = mHeightUnit.GetDependencyType();
            var isNotParentDependent = (heightUnitDependencyType != HierarchyDependencyType.DependsOnParent &&
                    heightUnitDependencyType != HierarchyDependencyType.DependsOnSiblings) ||
                this.HeightUnits.GetDependencyType() == HierarchyDependencyType.NoDependency ||
                mHeightUnit == DimensionUnitType.RelativeToMaxParentOrChildren;
            // A wrapping stack measures its RelativeToChildren height from each child's laid-out Y.
            // A Y measured from the parent's height (PixelsFromLarge, etc.) isn't refreshed before that
            // measure, so counting it lets a stale parent height sustain or ratchet itself.
            // In a wrapping LeftToRightStack the Y is measured from the child's row instead (#5802).
            var parentHeightDependencyType = parent.HeightUnits.GetDependencyType();
            var canPositionFromParentHeightCount = !doesParentWrapStack ||
                parentHeightDependencyType == HierarchyDependencyType.NoDependency ||
                parentHeightDependencyType == HierarchyDependencyType.DependsOnParent;
            var isPositionedFromLine = GetWrappedLineAxis(parent) == XOrY.Y && IsPositionedFromWrappedLineSize(XOrY.Y);
            isAbsolute = isNotParentDependent &&
                (mYUnits == GeneralUnitType.PixelsFromSmall || isPositionedFromLine ||
                    ((mYUnits == GeneralUnitType.PixelsFromLarge || mYUnits == GeneralUnitType.PixelsFromMiddle ||
#pragma warning disable CS0618 // PixelsFromMiddleInverted is obsolete but still loads from older projects
                    mYUnits == GeneralUnitType.PixelsFromMiddleInverted ||
#pragma warning restore CS0618
                    mYUnits == GeneralUnitType.PixelsFromBaseline) &&
                    canPositionFromParentHeightCount));

        }

        if (doesParentWrapStack)
        {
            return isAbsolute ? ChildType.StackedWrapped : ChildType.Relative;
        }
        else
        {
            return isAbsolute ? ChildType.Absolute : ChildType.Relative;
        }
    }

    #endregion

    bool DoesDimensionNeedUpdateFirstForRatio(DimensionUnitType unitType) =>
        unitType == DimensionUnitType.RelativeToChildren ||
        unitType == DimensionUnitType.PercentageOfOtherDimension ||
        unitType == DimensionUnitType.PercentageOfSourceFile ||
        unitType == DimensionUnitType.MaintainFileAspectRatio ||
        unitType == DimensionUnitType.ScreenPixel ||
        unitType == DimensionUnitType.RelativeToMaxParentOrChildren;

    private void UpdateChildren(int childrenUpdateDepth, ChildType childrenUpdateType, bool skipIgnoreByParentSize, HashSet<GraphicalUiElement>? alreadyUpdated = null, HashSet<GraphicalUiElement>? newlyUpdated = null)
    {
        bool CanDoFullUpdate(ChildType thisChildUpdateType, GraphicalUiElement childGue)
        {

            if (skipIgnoreByParentSize && childGue.IgnoredByParentSize)
            {
                return false;
            }

            return
                childrenUpdateType == ChildType.All ||
                (childrenUpdateType == ChildType.Absolute && thisChildUpdateType == ChildType.Absolute) ||
                (childrenUpdateType == ChildType.Relative && (thisChildUpdateType == ChildType.Relative || thisChildUpdateType == ChildType.BothAbsoluteAndRelative)) ||
                (childrenUpdateType == ChildType.StackedWrapped && thisChildUpdateType == ChildType.StackedWrapped);
        }
        if (this.mContainedObjectAsIpso == null)
        {
            // Same ratio-first pass as the renderable branch below: Ratio children read their
            // siblings' sizes, so size the siblings that must be measured first.
            bool doesAnyChildUseRatio = false;
            bool doesAnyChildNeedUpdateFirst = false;
            for (int i = 0; i < mWhatThisContains.Count; i++)
            {
                var child = mWhatThisContains[i];
                if (child.Parent == null || child.Parent == this)
                {
                    doesAnyChildUseRatio |= child.WidthUnits == DimensionUnitType.Ratio || child.HeightUnits == DimensionUnitType.Ratio;
                    doesAnyChildNeedUpdateFirst |= DoesDimensionNeedUpdateFirstForRatio(child.WidthUnits) || DoesDimensionNeedUpdateFirstForRatio(child.HeightUnits);
                }
            }
            if (doesAnyChildUseRatio && doesAnyChildNeedUpdateFirst)
            {
                for (int i = 0; i < mWhatThisContains.Count; i++)
                {
                    var child = mWhatThisContains[i];
                    if ((child.Parent == null || child.Parent == this) &&
                        (DoesDimensionNeedUpdateFirstForRatio(child.WidthUnits) || DoesDimensionNeedUpdateFirstForRatio(child.HeightUnits)) &&
                        CanDoFullUpdate(child.GetChildLayoutType(this), child))
                    {
                        child.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1);
                    }
                }
            }

            for (int i = 0; i < mWhatThisContains.Count; i++)
            {
                var child = mWhatThisContains[i];
                child._cachedSiblingIndex = i;
                // Victor Chelaru
                // January 10, 2017
                // I think we may not want to update any children which
                // have parents, because they'll get updated through their
                // parents...
                if (child.Parent == null || child.Parent == this)
                {
                    // Without a renderable, children are only updated with ChildType.All, which always allows a full update.
                    if (CanDoFullUpdate(child.GetChildLayoutType(this), child))
                    {
                        child.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1);
                        newlyUpdated?.Add(child);
                    }
                }
            }

            RepositionChildrenAlignedInWrappedLines(mWhatThisContains, parentlessOnly: true);
        }
        else
        {
            // 7/17/2023 - Long explanation about this code:
            // Normally children updating can be done in index order. However, if a child uses Ratio width or height, then the 
            // height of this child depends on its siblings. Since it depends on its siblings, any sibling needs to update first 
            // if it is using a complex WidthUnit or HeightUnit. All other update types (such as absolute) can be determined on the
            // spot when calculating the width of the ratio child.
            // Therefore, we will need to do all RelativeToChildren first if:
            //
            // * Some children use WidthUnits with Ratio, and some children use WidthUnits with RelativeToChildren
            //   --or--
            // * Any children use HeightUnits with Ratios, and some children use HeightUnits with RelativeToChildren
            //
            // If either is the case, then we will first update all children that have the relative properties. Then we'll loop through all of them
            // Note about optimization - if children using relative all come first, then a normal order will satisfy the dependencies.
            // But that makes the code slightly more complex, so I'll bother with that performance optimization later.
            // Update July 6, 2025
            // The above explanation is still true, but we also need to consider that a sibling may be using WidthUnits or HeightUnits that require an update
            // first. Vic has expanded the unit types that also need to be updated first to be:
            // * RelativeToChildren
            // * PercentageOfOtherDimension
            // * PercentageOfSourceFile
            // * MaintainFileAspectRatio
            // * ScreenPixel
            // So we are going to do updates on all siblings that have these types first

            bool doesAnyChildUseRatioWidth = false;
            bool doesAnyChildUseRatioHeight = false;
            bool doesAnyChildNeedWidthUpdatedFirst = false;
            bool doesAnyChildNeedHeightUpdatedFirst = false;

            for (int i = 0; i < this.Children.Count; i++)
            {
                var child = this.Children[i];

                doesAnyChildUseRatioWidth |= child.WidthUnits == DimensionUnitType.Ratio;
                doesAnyChildUseRatioHeight |= child.HeightUnits == DimensionUnitType.Ratio;

                doesAnyChildNeedWidthUpdatedFirst |= DoesDimensionNeedUpdateFirstForRatio(child.WidthUnits);
                doesAnyChildNeedHeightUpdatedFirst |= DoesDimensionNeedUpdateFirstForRatio(child.HeightUnits);
            }

            var shouldUpdateRelativeFirst = (doesAnyChildUseRatioWidth && doesAnyChildNeedWidthUpdatedFirst) || (doesAnyChildUseRatioHeight && doesAnyChildNeedHeightUpdatedFirst);

            // Update - if this item stacks, then it cannot mark the children as updated - it needs to do another
            // pass later to update the position of the children in order from top-to-bottom. If we flag as updated,
            // then the pass later that does the actual stacking will skip anything that is flagged as updated.
            // This bug was reproduced as reported in this issue:
            // https://github.com/vchelaru/Gum/issues/141
            var shouldFlagAsUpdated = this.ChildrenLayout == ChildrenLayout.Regular;

            if (shouldUpdateRelativeFirst)
            {
                for (int i = 0; i < this.Children.Count; i++)
                {
                    var child = this.Children[i];

                    if ((alreadyUpdated == null || alreadyUpdated.Contains(child) == false))
                    {
                        if (DoesDimensionNeedUpdateFirstForRatio(child.WidthUnits) || DoesDimensionNeedUpdateFirstForRatio(child.HeightUnits))
                        {
                            UpdateChild(child, flagAsUpdated: false);
                        }
                    }
                }
            }


            // do a normal one:
            for (int i = 0; i < this.Children.Count; i++)
            {
                var child = this.Children[i];
                child._cachedSiblingIndex = i;

                if ((alreadyUpdated == null || alreadyUpdated.Contains(child) == false))
                {
                    // now do all:
                    UpdateChild(child, flagAsUpdated: shouldFlagAsUpdated);
                }
            }

            RepositionChildrenAlignedInWrappedLines(this.Children, parentlessOnly: false);


            void UpdateChild(GraphicalUiElement child, bool flagAsUpdated)
            {
                var childLayoutType = child.GetChildLayoutType(this);
                var canDoFullUpdate =
                    CanDoFullUpdate(childLayoutType, child);


                if (canDoFullUpdate)
                {
                    // Pass ParentUpdateType.None here so that children do not attempt to update their parent. `this` is
                    // the parent and it's already in an update
                    child.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1);
                    if (flagAsUpdated)
                    {
                        newlyUpdated?.Add(child);
                    }
                }
                else
                {
                    // only update absolute layout, and the child has some relative values, but let's see if 
                    // we can do only one axis:
                    if (CanDoFullUpdate(child.GetChildLayoutType(XOrY.X, this), child))
                    {
                        // todo - maybe look at the code below to see if we need to do the same thing here for
                        // width/height updates:
                        child.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1, XOrY.X);
                    }
                    else if (CanDoFullUpdate(child.GetChildLayoutType(XOrY.Y, this), child))
                    {
                        // in this case, the child's Y is going to be updated, but the child's X may depend on 
                        // the parent's width. If so, the parent's width should already be updated, so long as
                        // the width doesn't depend on the children. So...let's see if that's the case:
                        var widthDependencyType = this.WidthUnits.GetDependencyType();
                        if (widthDependencyType != HierarchyDependencyType.DependsOnChildren &&
                            (child.HeightUnits == DimensionUnitType.PercentageOfOtherDimension) || (child.HeightUnits == DimensionUnitType.MaintainFileAspectRatio))
                        {
                            child.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1);
                        }
                        else
                        {
                            child.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1, XOrY.Y);

                        }
                    }
                }

            }

        }
    }

    /// <summary>
    /// Repositions the children of this wrapping stack whose cross-axis position is measured from their
    /// line and whose line is now a different size than when they were positioned. A line's size is final
    /// only after its last child is measured, so a child earlier in the line can be placed against a
    /// partial or stale size (#5802). Children aligned to the line's start never need this.
    /// </summary>
    private void RepositionChildrenAlignedInWrappedLines(IList<GraphicalUiElement> children, bool parentlessOnly)
    {
        if (!WrapsChildren || (ChildrenLayout != ChildrenLayout.LeftToRightStack && ChildrenLayout != ChildrenLayout.TopToBottomStack))
        {
            return;
        }

        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (float.IsNaN(child._wrappedLineSizeUsedForPosition) || child.mContainedObjectAsIpso == null || !child.Visible ||
                (parentlessOnly && child.Parent != null && child.Parent != this))
            {
                continue;
            }

            if (child.GetWrappedLineSize(this) != child._wrappedLineSizeUsedForPosition)
            {
                child._cachedSiblingIndex = i;
                child.UpdatePositionOnly();
            }
        }
    }

    private void UpdatePositionOnly()
    {
        GetParentLayoutInputs(out float parentWidth, out float parentHeight, out float parentAbsoluteRotation,
            out bool isParentFlippedHorizontally);

        float xBefore = mContainedObjectAsIpso!.X;
        float yBefore = mContainedObjectAsIpso.Y;

        UpdatePosition(parentWidth, parentHeight, xOrY: null, parentAbsoluteRotation, isParentFlippedHorizontally);

        if (xBefore != mContainedObjectAsIpso.X || yBefore != mContainedObjectAsIpso.Y)
        {
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// The axis on which this child's wrapped line, not the whole parent, is its parent for position:
    /// Y in a wrapping LeftToRightStack, X in a wrapping TopToBottomStack, otherwise null (#5802).
    /// </summary>
    private static XOrY? GetWrappedLineAxis(GraphicalUiElement? parent)
    {
        if (parent == null || !parent.WrapsChildren)
        {
            return null;
        }
        return parent.ChildrenLayout switch
        {
            ChildrenLayout.LeftToRightStack => XOrY.Y,
            ChildrenLayout.TopToBottomStack => XOrY.X,
            _ => null,
        };
    }

    /// <summary>
    /// Whether this child's cross-axis position reads its line's size: units measured from the line's
    /// middle, far edge, baseline or as a percentage of it. An origin alone does not.
    /// </summary>
    private bool IsPositionedFromWrappedLineSize(XOrY lineAxis)
    {
        var units = lineAxis == XOrY.X ? mXUnits : mYUnits;
        return units != GeneralUnitType.PixelsFromSmall && units != GeneralUnitType.PercentageOfFile;
    }

    /// <summary>
    /// This child's contribution to its line's cross-axis size, as recorded in the parent's
    /// <see cref="StackedRowOrColumnDimensions"/>. The line counts its children the way a parent sized
    /// to its children does (Width Units docs, "Ignored Width Values"): the offset counts from the edge
    /// it is measured from, ignoring any portion outside the line, and a Percentage-positioned child
    /// counts as 0. A size that depends on the parent counts only when the parent's cross axis is not
    /// sized to its children, since otherwise the two depend on each other (#5802).
    /// </summary>
    private float GetStackedLineDimension(GraphicalUiElement parent)
    {
        if (parent.ChildrenLayout == ChildrenLayout.LeftToRightStack)
        {
            if (IsSizeDependentOnParent(mHeightUnit) && DependsOnChildren(parent.HeightUnits))
            {
                return 0;
            }
            return GetRequiredParentHeightFromEdges();
        }

        if (IsSizeDependentOnParent(mWidthUnit) && DependsOnChildren(parent.WidthUnits))
        {
            return 0;
        }
        return GetRequiredParentWidthFromEdges();
    }

    private static bool DependsOnChildren(DimensionUnitType units) =>
        units.GetDependencyType() == HierarchyDependencyType.DependsOnChildren ||
        units == DimensionUnitType.RelativeToMaxParentOrChildren;

    // Matches GetChildLayoutType(XOrY, parent): a parent sized to its children ignores such a child.
    private static bool IsSizeDependentOnParent(DimensionUnitType units)
    {
        var dependency = units.GetDependencyType();
        return (dependency == HierarchyDependencyType.DependsOnParent || dependency == HierarchyDependencyType.DependsOnSiblings) &&
            units != DimensionUnitType.RelativeToMaxParentOrChildren;
    }

    /// <summary>
    /// The cross-axis size of the line this child is in so far: the largest child recorded for the line,
    /// including this one.
    /// </summary>
    private float GetWrappedLineSize(GraphicalUiElement parent)
    {
        var ownDimension = GetStackedLineDimension(parent);
        var dimensions = parent.StackedRowOrColumnDimensions;
        var index = StackedRowOrColumnIndex;
        if (dimensions != null && index >= 0 && index < dimensions.Count)
        {
            return System.Math.Max(dimensions[index], ownDimension);
        }
        return ownDimension;
    }
}
