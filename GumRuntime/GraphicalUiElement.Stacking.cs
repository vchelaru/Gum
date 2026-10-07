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

    private void RefreshParentRowColumnDimensionForThis()
    {
        // If it stacks, then update this row/column's dimensions given the index of this
        var indexToUpdate = this.StackedRowOrColumnIndex;

        if (indexToUpdate == -1)
        {
            return;
        }

        var parentGue = EffectiveParentGue;

        if (parentGue == null)
        {
            return;
        }

        if (this.Visible)
        {

            if (parentGue.StackedRowOrColumnDimensions == null)
            {
                parentGue.StackedRowOrColumnDimensions = new List<float>();
            }

            if (parentGue.StackedRowOrColumnDimensions.Count <= indexToUpdate)
            {
                parentGue.StackedRowOrColumnDimensions.Add(0);
            }

            float myDimension = GetStackedLineDimension(parentGue);

            float currentMax = parentGue.StackedRowOrColumnDimensions[indexToUpdate];

            if (myDimension >= currentMax)
            {
                // This child is the new max (or equal), no need to scan siblings
                parentGue.StackedRowOrColumnDimensions[indexToUpdate] = myDimension;
            }
            else
            {
                // This child's dimension is less than the stored max. It may have been
                // the previous max-holder and shrunk, so we must rescan all siblings
                // in this row/column up to and including this child to find the true max.
                // Indexed loop rather than foreach so iterating the Children
                // ObservableCollection does not box its enumerator on this hot path.
                parentGue.StackedRowOrColumnDimensions[indexToUpdate] = 0;
                var siblings = parentGue.Children;
                for (int i = 0; i < siblings.Count; i++)
                {
                    GraphicalUiElement child = siblings[i];
                    if (child.Visible)
                    {
                        if (child.StackedRowOrColumnIndex == indexToUpdate)
                        {
                            parentGue.StackedRowOrColumnDimensions[indexToUpdate] =
                                System.Math.Max(parentGue.StackedRowOrColumnDimensions[indexToUpdate],
                                child.GetStackedLineDimension(parentGue));

                            if (this == child)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

    }

    private void GetCellDimensions(int indexInSiblingList, out int xIndex, out int yIndex, out float cellWidth, out float cellHeight)
    {
        // Only called for a child of an auto-grid parent.
        var effectiveParent = EffectiveParentGue!;
        var columnCount = effectiveParent.AutoGridHorizontalCells;
        var rowCount = effectiveParent.AutoGridVerticalCells;
        if (columnCount < 1) columnCount = 1;
        if (rowCount < 1) rowCount = 1;

        var childCount = effectiveParent.GetVisibleChildCount();

        if (effectiveParent.ChildrenLayout == ChildrenLayout.AutoGridHorizontal)
        {
            xIndex = indexInSiblingList % columnCount;
            yIndex = indexInSiblingList / columnCount;

            // Columns are fixed by AutoGridHorizontalCells. Extra rows only shrink the cells when the
            // parent's height grows with its children; then the parent is already tall enough for every
            // row, so dividing by the grown row count keeps each cell one row tall. A fixed-height parent
            // keeps its cell size and extra rows overflow below its bounds. Matches GetParentDimensions.
            if (effectiveParent.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                var requiredRowCount = (int)Math.Ceiling((float)childCount / columnCount);
                rowCount = System.Math.Max(rowCount, requiredRowCount);
            }
        }
        else // vertical
        {
            yIndex = indexInSiblingList % rowCount;
            xIndex = indexInSiblingList / rowCount;

            // Symmetric to the horizontal case: extra columns only shrink the cells when the parent's
            // width grows with its children; a fixed-width parent overflows to the right.
            if (effectiveParent.WidthUnits == DimensionUnitType.RelativeToChildren)
            {
                var requiredColumnCount = (int)Math.Ceiling((float)childCount / rowCount);
                columnCount = System.Math.Max(columnCount, requiredColumnCount);
            }
        }
        var parentWidth = effectiveParent.AbsoluteWidth - (columnCount - 1) * effectiveParent.StackSpacing;
        var parentHeight = effectiveParent.AbsoluteHeight - (rowCount - 1) * effectiveParent.StackSpacing;

        cellWidth = (parentWidth / columnCount);
        cellHeight = (parentHeight / rowCount);

        // January 15, 2025
        // If a parent height
        // is relative to children,
        // then the largest child determines
        // the cell size. By this point, the parent
        // has already determined its own height, so we
        // should respect that height instead of relying on
        // the children to set it
        //if (effectiveParent.ChildrenLayout == ChildrenLayout.AutoGridHorizontal &&
        //    effectiveParent.HeightUnits == DimensionUnitType.RelativeToChildren)
        //{
        //    cellHeight = effectiveParent.GetMaxCellHeight(true, 0);
        //}
        //if (effectiveParent.ChildrenLayout == ChildrenLayout.AutoGridVertical &&
        //    effectiveParent.WidthUnits == DimensionUnitType.RelativeToChildren)
        //{
        //    cellWidth = effectiveParent.GetMaxCellWidth(true, 0);
        //}

    }

    private GraphicalUiElement? GetFirstVisibleChild()
    {
        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i].Visible)
            {
                return Children[i];
            }
        }
        return null;
    }

    // Counts the children a grid or stack places: visible ones, from the same list GetIndexInVisibleSiblings walks.
    private int GetVisibleChildCount()
    {
        int count = 0;
        if (mContainedObjectAsIpso != null)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Visible)
                {
                    count++;
                }
            }
        }
        else
        {
            for (int i = 0; i < mWhatThisContains.Count; i++)
            {
                var child = mWhatThisContains[i];
                if (child.Parent == null && child.Visible)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private int GetIndexInVisibleSiblings()
    {
        System.Collections.IList? siblings = null;

        if (this.Parent == null)
        {
            siblings = this.ElementGueContainingThis?.mWhatThisContains;
        }
        else if (this.Parent is GraphicalUiElement)
        {
            siblings = ((GraphicalUiElement)Parent).Children as System.Collections.IList;
        }

        var thisIndex = 0;
        if(siblings != null)
        {
            for (int i = 0; i < siblings.Count; i++)
            {
                if (siblings[i] == this)
                {
                    break;
                }
                if (((IVisible)siblings[i]!).Visible)
                {
                    thisIndex++;
                }
            }
        }

        return thisIndex;
    }

    private bool GetIfParentStacks()
    {
        return this.EffectiveParentGue != null &&
            (this.EffectiveParentGue.ChildrenLayout == ChildrenLayout.TopToBottomStack ||
            this.EffectiveParentGue.ChildrenLayout == ChildrenLayout.LeftToRightStack);
    }

    private bool GetIfParentIsAutoGrid()
    {
        return this.EffectiveParentGue != null &&
            (this.EffectiveParentGue.ChildrenLayout == ChildrenLayout.AutoGridHorizontal ||
            this.EffectiveParentGue.ChildrenLayout == ChildrenLayout.AutoGridVertical);
    }

    private bool GetIfParentWidthHeightDependOnChildren()
    {
        return (this.EffectiveParentGue as GraphicalUiElement)?.GetIfDimensionsDependOnChildren() == true;
    }

    private bool GetIfParentHasRatioChildren()
    {
        // A renderable-less element's Children is an empty collection, so read the layout siblings.
        var siblings = GetLayoutSiblings(out bool onlyParentless);
        if (siblings != null)
        {
            for (int i = 0; i < siblings.Count; i++)
            {
                var sibling = siblings[i];
                if (IsLayoutSibling(sibling, onlyParentless) &&
                    (sibling.WidthUnits == DimensionUnitType.Ratio || sibling.HeightUnits == DimensionUnitType.Ratio))
                {
                    return true;
                }
            }
        }

        return false;
    }


    /// <summary>
    /// Returns whether a layout change on this element must notify its parent — i.e. whether the
    /// parent's own layout structurally depends on this child: the parent content-sizes
    /// (GetIfDimensionsDependOnChildren), stacks its children, or has any Ratio-sized sibling.
    /// </summary>
    /// <remarks>
    /// This is a purely STRUCTURAL check — it does not consider whether anything actually changed.
    /// It is called from both parent-climb sites in UpdateLayout: the unconditional originating
    /// climb and the size-gated propagated climb (#3066). The "did anything change?" gating lives
    /// at those call sites, not here, which is why the same predicate appears in two places with
    /// different surrounding conditions.
    /// </remarks>
    bool GetIfShouldCallUpdateOnParent()
    {
        // EffectiveParentGue: a child with no Parent is laid out by the element containing it, but
        // only when that element has no renderable (it then walks its contained instances); an element
        // with a renderable lays out only its Children.
        var parentGue = this.EffectiveParentGue;

        if (parentGue == null || (Parent == null && parentGue.mContainedObjectAsIpso != null))
        {
            return false;
        }

        if (// parent needs to be resized based on this position or size
            parentGue.GetIfDimensionsDependOnChildren() ||
            // parent stacks its children, so siblings need to adjust their position based on this
            parentGue.ChildrenLayout != Gum.Managers.ChildrenLayout.Regular)
        {
            return true;
        }

        // if any siblings are ratio-based, then they need to re-split the space
        var siblings = GetLayoutSiblings(out bool onlyParentless);
        if (siblings != null)
        {
            for (int i = 0; i < siblings.Count; i++)
            {
                var sibling = siblings[i];
                if (IsLayoutSibling(sibling, onlyParentless) &&
                    (sibling.WidthUnits == DimensionUnitType.Ratio || sibling.HeightUnits == DimensionUnitType.Ratio))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // The elements this one is laid out among: its Parent's Children or, for a child with no Parent,
    // the instances of the element containing it that also have no Parent (see IsLayoutSibling).
    IList<GraphicalUiElement>? GetLayoutSiblings(out bool onlyParentless)
    {
        if (_parent != null)
        {
            onlyParentless = false;
            return _parent.Children;
        }
        onlyParentless = true;
        return ElementGueContainingThis?.mWhatThisContains;
    }

    static bool IsLayoutSibling(GraphicalUiElement candidate, bool onlyParentless) =>
        !onlyParentless || candidate.Parent == null;

    private static bool GetIfOneDimensionCanChangeOtherDimension(GraphicalUiElement gue)
    {
        var canOneDimensionChangeTheOtherOnChild = gue.RenderableComponent is IText ||
                gue.WidthUnits == DimensionUnitType.PercentageOfOtherDimension ||
                gue.HeightUnits == DimensionUnitType.PercentageOfOtherDimension ||
                gue.WidthUnits == DimensionUnitType.MaintainFileAspectRatio ||
                gue.HeightUnits == DimensionUnitType.MaintainFileAspectRatio ||


                ((gue.ChildrenLayout == ChildrenLayout.LeftToRightStack || gue.ChildrenLayout == ChildrenLayout.TopToBottomStack) && gue.WrapsChildren);

        // If the child cannot be directly changed by a dimension, it may be indirectly changed by a dimension recursively. This can happen
        // if the child either depends on its own children's widths and heights, and one of its children can have its dimension changed.

        if (!canOneDimensionChangeTheOtherOnChild && gue.GetIfDimensionsDependOnChildren())
        {
            for (int i = 0; i < gue.Children.Count; i++)
            {
                var child = gue.Children[i];

                if (GetIfOneDimensionCanChangeOtherDimension(child))
                {
                    canOneDimensionChangeTheOtherOnChild = true;
                    break;
                }

            }
        }

        return canOneDimensionChangeTheOtherOnChild;

    }

    private GraphicalUiElement? GetWhatToStackAfter(bool canWrap, bool shouldWrap, out float whatToStackAfterX, out float whatToStackAfterY)
    {
        IPositionedSizedObject? whatToStackAfter = null;
        whatToStackAfterX = 0;
        whatToStackAfterY = 0;

        var parentGue = this.EffectiveParentGue;

        ////////////////////////////////Early Out//////////////////////////////////
        if (parentGue == null)
        {
            return null;
        }

        int thisIndex = 0;

        // We used to have a static list we were populating, but that allocates memory so we
        // now use the actual list.
        System.Collections.IList? siblings = null;

        if (this.Parent == null)
        {
            // With no Parent, parentGue (the effective parent) is the element containing this.
            siblings = parentGue.mWhatThisContains;
        }
        else if (this.Parent is GraphicalUiElement)
        {
            siblings = ((GraphicalUiElement)Parent).Children as System.Collections.IList;
        }

        if (siblings == null)
        {
            return null;
        }
        /////////////////////////////End Early Out/////////////////////////////////

        if (_cachedSiblingIndex >= 0 && _cachedSiblingIndex < siblings.Count && siblings[_cachedSiblingIndex] == this)
        {
            thisIndex = _cachedSiblingIndex;
        }
        else
        {
            thisIndex = siblings.IndexOf(this);
        }


        if (parentGue.StackedRowOrColumnDimensions == null)
        {
            parentGue.StackedRowOrColumnDimensions = new List<float>();
        }

        int thisRowOrColumnIndex = 0;



        if (thisIndex > 0)
        {
            var index = thisIndex - 1;
            while (index > -1)
            {
                if (((IVisible)siblings[index]!).Visible)
                {
                    whatToStackAfter = siblings[index] as GraphicalUiElement;
                    break;
                }
                index--;
            }
        }

        if (whatToStackAfter != null)
        {
            if (shouldWrap)
            {
                // This is going to be on a new row/column. That means the following are true:
                // * It will have a previous sibling.
                // * It will be positioned at the start/end of its row/column
                this.StackedRowOrColumnIndex = ((GraphicalUiElement)whatToStackAfter).StackedRowOrColumnIndex + 1;


                thisRowOrColumnIndex = ((GraphicalUiElement)whatToStackAfter).StackedRowOrColumnIndex + 1;
                var previousRowOrColumnIndex = thisRowOrColumnIndex - 1;
                if (parentGue.ChildrenLayout == Gum.Managers.ChildrenLayout.LeftToRightStack)
                {
                    whatToStackAfterX = 0;

                    whatToStackAfterY = 0;
                    for (int i = 0; i < thisRowOrColumnIndex; i++)
                    {
                        whatToStackAfterY += parentGue.StackedRowOrColumnDimensions[i] + parentGue.StackSpacing;
                    }
                }
                else // top to bottom stack
                {
                    whatToStackAfterY = 0;
                    whatToStackAfterX = 0;
                    for (int i = 0; i < thisRowOrColumnIndex; i++)
                    {
                        whatToStackAfterX += parentGue.StackedRowOrColumnDimensions[i] + parentGue.StackSpacing;
                    }
                }

            }
            else
            {

                if (whatToStackAfter != null)
                {
                    thisRowOrColumnIndex = ((GraphicalUiElement)whatToStackAfter).StackedRowOrColumnIndex;

                    this.StackedRowOrColumnIndex = ((GraphicalUiElement)whatToStackAfter).StackedRowOrColumnIndex;
                    if (parentGue.ChildrenLayout == Gum.Managers.ChildrenLayout.LeftToRightStack)
                    {
                        // Offsets are in unflipped space. Under a flipped parent the previous sibling's X is its
                        // mirrored left edge, so its unflipped right edge is the parent width minus that X.
                        var previousRight = parentGue.GetAbsoluteFlipHorizontal()
                            ? parentGue.AbsoluteWidth - whatToStackAfter.X
                            : whatToStackAfter.X + whatToStackAfter.Width;
                        whatToStackAfterX = previousRight + parentGue.StackSpacing;

                        whatToStackAfterY = 0;
                        for (int i = 0; i < thisRowOrColumnIndex; i++)
                        {
                            whatToStackAfterY += parentGue.StackedRowOrColumnDimensions[i] + parentGue.StackSpacing;
                        }
                    }
                    else
                    {
                        whatToStackAfterY = whatToStackAfter.Y + whatToStackAfter.Height + parentGue.StackSpacing;
                        whatToStackAfterX = 0;
                        for (int i = 0; i < thisRowOrColumnIndex; i++)
                        {
                            whatToStackAfterX += parentGue.StackedRowOrColumnDimensions[i] + parentGue.StackSpacing;
                        }
                    }

                    // This is on the same row/column as its previous sibling
                }
            }
        }
        else
        {
            StackedRowOrColumnIndex = 0;
        }

        return whatToStackAfter as GraphicalUiElement;
    }
}
