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


#if !FRB
using Gum.StateAnimation.Runtime;
#endif

namespace Gum.Wireframe;

#region Enums

public enum MissingFileBehavior
{
    ConsumeSilently,
    ThrowException
}

public enum Anchor
{
    TopLeft,
    Top,
    TopRight,
    Left,
    Center,
    Right,
    BottomLeft,
    Bottom,
    BottomRight
}

public enum Dock
{
    Top,
    Left,
    Fill,
    Right,
    Bottom,
    FillHorizontally,
    FillVertically,
    SizeToChildren
}

#endregion

/// <summary>
/// The base object for all Gum runtime objects. It contains functionality for
/// setting variables, states, and performing layout. The GraphicalUiElement can
/// wrap an underlying rendering object.
/// GraphicalUiElements are also considered "Visuals" for Forms objects such as Button and TextBox.
/// </summary>
public partial class GraphicalUiElement : IRenderableIpso, IVisible, INotifyPropertyChanged
{


    #region Layout-related

    #region UpdateLayout calls
    public void UpdateLayout()
    {
        UpdateLayout(true, true);
    }

    public void UpdateLayout(bool updateParent, bool updateChildren)
    {
        int value = int.MaxValue / 2;
        if (!updateChildren)
        {
            value = 0;
        }
        UpdateLayout(updateParent, value);
    }

    public void UpdateLayout(bool updateParent, int childrenUpdateDepth, XOrY? xOrY = null)
    {
        if (updateParent)
        {
            UpdateLayout(ParentUpdateType.All, childrenUpdateDepth, xOrY);
        }
        else
        {
            UpdateLayout(ParentUpdateType.None, childrenUpdateDepth, xOrY);
        }

    }

    HashSet<GraphicalUiElement> fullyUpdatedChildren = new HashSet<GraphicalUiElement>();

    /// <summary>
    /// Performs an update to this, and optionally to its parent and children depending on the parameters.
    /// </summary>
    /// <param name="parentUpdateType">A filter determining whether whether to update the parent. If All is passed, then
    /// the parent will always update. If other properties are passed, then the update happens only if the parent matches
    /// the update type. For example if ParentUpdateType.IfParentStacks is passed, then an update happens if the parent stacks its children.</param>
    /// <param name="childrenUpdateDepth"></param>
    /// <param name="xOrY"></param>
    public void UpdateLayout(ParentUpdateType parentUpdateType, int childrenUpdateDepth, XOrY? xOrY = null)
    {
        var updateParent =
            ((parentUpdateType & ParentUpdateType.All) == ParentUpdateType.All) ||
            ((parentUpdateType & ParentUpdateType.IfParentStacks) == ParentUpdateType.IfParentStacks && GetIfParentStacks()) ||
            ((parentUpdateType & ParentUpdateType.IfParentIsAutoGrid) == ParentUpdateType.IfParentIsAutoGrid && GetIfParentIsAutoGrid()) ||
            ((parentUpdateType & ParentUpdateType.IfParentWidthHeightDependOnChildren) == ParentUpdateType.IfParentWidthHeightDependOnChildren && (Parent as GraphicalUiElement)?.GetIfDimensionsDependOnChildren() == true) ||
            ((parentUpdateType & ParentUpdateType.IfParentHasRatioSizedChildren) == ParentUpdateType.IfParentHasRatioSizedChildren && GetIfParentHasRatioChildren())
            ;

        #region Early Out - Suspended or invisible

        var asIVisible = this as IVisible;

        var isSuspended = mIsLayoutSuspended || IsAllLayoutSuspended;
        if (!isSuspended)
        {
            isSuspended = !AreUpdatesAppliedWhenInvisible &&
                (mContainedObjectAsIVisible != null && asIVisible.AbsoluteVisible == false && this.IsInRenderTargetRecursively() == false);
        }

        if (isSuspended)
        {
            MakeDirty(parentUpdateType, childrenUpdateDepth, xOrY);
            return;
        }

        if (!AreUpdatesAppliedWhenInvisible &&
            // If this is a render target, we still want to do updates when it is invisible because
            // it may be used by something else:
            !this.IsRenderTarget)
        {
            var parentAsIVisible = Parent as IVisible;
            if (Visible == false && parentAsIVisible?.AbsoluteVisible == false && !this.IsInRenderTargetRecursively())
            {
                return;
            }
        }

        #endregion

        #region Early Out - Update Parent and exit

        currentDirtyState = null;


        // May 15, 2014
        // Parent needs to be
        // set before we start
        // doing the updates because
        // we use foreaches internally
        // in the updates.
        if (mContainedObjectAsIpso != null)
        {
            // If we assign the Parent, then the Parent will have the 
            // mContainedObjectAsIpso added to its children, which will
            // result in it being rendered. But this GraphicalUiElement is
            // already a child of the Parent, so adding the mContainedObjectAsIpso
            // as well would result in a double-render. Instead, we'll set the parent
            // direct, so the parent doesn't know about this child:
            //mContainedObjectAsIpso.Parent = mParent;
            mContainedObjectAsIpso.SetParentDirect(_parent);
        }


        // Not sure why we use the ParentGue and not the Parent itself...
        // We want to do it on the actual Parent so that objects attached to components
        // should update the components
        if (updateParent && GetIfShouldCallUpdateOnParent())
        {
            var asGue = this.Parent as GraphicalUiElement;
            // Just climb up one and update from there
            asGue.UpdateLayout(parentUpdateType, childrenUpdateDepth + 1);
            ChildrenUpdatingParentLayoutCalls++;
            return;
        }
        // This should be *after* the return when updating the parent otherwise we double-count layouts
        UpdateLayoutCallCount++;

        #endregion

        float widthBeforeLayout = 0;
        float heightBeforeLayout = 0;
        float xBeforeLayout = 0;
        float yBeforeLayout = 0;
        // Victor Chelaru
        // March 1, 2015
        // We tested not doing "deep" UpdateLayouts
        // if the object doesn't actually need it. This
        // is the case if the if-statement below evaluates to true. But in practice
        // we got very minor reduction in calls, but we incurred a lot of if-checks, so I don't
        // think this is worth it at this time.
        //if(this.mXOrigin == HorizontalAlignment.Left && mXUnits == GeneralUnitType.PixelsFromSmall &&
        //    this.mYOrigin == VerticalAlignment.Top && mYUnits == GeneralUnitType.PixelsFromSmall &&
        //    this.mWidthUnit == DimensionUnitType.Absolute && this.mWidth > 0 &&
        //    this.mHeightUnit == DimensionUnitType.Absolute && this.mHeight > 0)
        //{
        //    var parent = EffectiveParentGue;
        //    if (parent == null || parent.ChildrenLayout == Gum.Managers.ChildrenLayout.Regular)
        //    {
        //        UnnecessaryUpdateLayouts++;
        //    }
        //}

        float parentWidth;
        float parentHeight;

        GetParentDimensions(out parentWidth, out parentHeight);

        float absoluteParentRotation = 0;
        bool isParentFlippedHorizontally = false;
        if (this.Parent != null)
        {
            absoluteParentRotation = this.Parent.GetAbsoluteRotation();
            isParentFlippedHorizontally = Parent.GetAbsoluteFlipHorizontal();
        }
        else if (this.ElementGueContainingThis != null && this.ElementGueContainingThis.mContainedObjectAsIpso != null)
        {
            parentWidth = this.ElementGueContainingThis.mContainedObjectAsIpso.Width;
            parentHeight = this.ElementGueContainingThis.mContainedObjectAsIpso.Height;

            absoluteParentRotation = this.ElementGueContainingThis.GetAbsoluteRotation();
        }

        if (mContainedObjectAsIpso != null)
        {
            if (mContainedObjectAsIpso is ISetClipsChildren clipsChildrenChild)
            {
                clipsChildrenChild.ClipsChildren = ClipsChildren;
            }

            widthBeforeLayout = mContainedObjectAsIpso.Width;
            heightBeforeLayout = mContainedObjectAsIpso.Height;

            xBeforeLayout = mContainedObjectAsIpso.X;
            yBeforeLayout = mContainedObjectAsIpso.Y;

            // The texture dimensions may need to be set before
            // updating width if we are using % of texture width/height.
            // However, if the texture coordinates depend on the dimensions
            // (like for a tiling background) then this also needs to be set
            // after UpdateDimensions. 
            if (mContainedObjectAsIpso is ITextureCoordinate)
            {
                UpdateTextureCoordinatesNotDimensionBased();
            }

            // August 12, 2021
            // If we can update one
            // of the dimensions first
            // (if it doesn't depend on
            // any children), we should, since
            // it can make the children update have
            // the real width/height set properly
            // May 26, 2023
            // If a dimension doesn't depend on any children, then we are already
            // in a state where we can update that dimension now before doing any children
            // updates. Let's do that.
            var widthDependencyType = this.WidthUnits.GetDependencyType();
            var heightDependencyType = this.HeightUnits.GetDependencyType();

            var hasChildDependency = widthDependencyType == HierarchyDependencyType.DependsOnChildren ||
                heightDependencyType == HierarchyDependencyType.DependsOnChildren;

            if (widthDependencyType != HierarchyDependencyType.DependsOnChildren && heightDependencyType != HierarchyDependencyType.DependsOnChildren)
            {
                UpdateDimensions(parentWidth, parentHeight, null, true);
            }
            else if (widthDependencyType != HierarchyDependencyType.DependsOnChildren)
            {
                UpdateDimensions(parentWidth, parentHeight, XOrY.X, considerWrappedStacked: false);
            }
            if (heightDependencyType != HierarchyDependencyType.DependsOnChildren)
            {
                UpdateDimensions(parentWidth, parentHeight, XOrY.Y, considerWrappedStacked: false);
            }

            fullyUpdatedChildren.Clear();

            if (hasChildDependency && childrenUpdateDepth > 0)
            {
                // This causes a double-update of children. For list boxes, this can be expensive.
                // We can special-case this IF all are true:
                // 1. This depends on children
                // 2. This stacks in the same axis as the children
                // 3. This is using FixedStackSpacing
                // 4. This has more than one child
                // for now, let's do this only on the vertical axis as a test:
                if (this.ChildrenLayout == Gum.Managers.ChildrenLayout.TopToBottomStack &&
                    this.HeightUnits.GetDependencyType() == HierarchyDependencyType.DependsOnChildren &&
                    this.UseFixedStackChildrenSize &&
                    this.Children?.Count > 1)
                {

                    //UpdateDimensions(parentWidth, parentHeight, XOrY.Y, considerWrappedStacked: false);
                    var firstChild = this.Children[0];
                    var childLayout = firstChild.GetChildLayoutType(this);

                    if (childLayout == ChildType.Absolute)
                    {
                        firstChild?.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1);
                        fullyUpdatedChildren.Add(firstChild);
                    }
                    else
                    {
                        firstChild?.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1, XOrY.Y);
                    }
                }
                else
                {
                    UpdateChildren(childrenUpdateDepth, ChildType.Absolute, skipIgnoreByParentSize: true, newlyUpdated: fullyUpdatedChildren);
                }
            }

            // This will update according to all absolute children
            // Now that the children have been updated, we can do any dimensions that still need updating based on the children changes:

            if (widthDependencyType == HierarchyDependencyType.DependsOnChildren)
            {
                UpdateDimensions(parentWidth, parentHeight, XOrY.X, considerWrappedStacked: false);
            }
            if (heightDependencyType == HierarchyDependencyType.DependsOnChildren)
            {
                UpdateDimensions(parentWidth, parentHeight, XOrY.Y, considerWrappedStacked: false);
            }

            if (this.WrapsChildren && (this.ChildrenLayout == ChildrenLayout.LeftToRightStack || this.ChildrenLayout == ChildrenLayout.TopToBottomStack))
            {
                // Now we can update all children that are wrapped:
                UpdateChildren(childrenUpdateDepth, ChildType.StackedWrapped, skipIgnoreByParentSize: false);
                if (this.WidthUnits.GetDependencyType() == HierarchyDependencyType.DependsOnChildren ||
                    this.HeightUnits.GetDependencyType() == HierarchyDependencyType.DependsOnChildren)
                {
                    UpdateDimensions(parentWidth, parentHeight, xOrY, considerWrappedStacked: true);
                }
            }

            if (mContainedObjectAsIpso is ITextureCoordinate)
            {
                UpdateTextureCoordinatesDimensionBased();
            }

            // If the update is "deep" then we want to refresh the text texture.
            // Otherwise it may have been something shallow like a reposition.
            // -----------------------------------------------------------------------------
            // Update December 3, 2022 - This if-check causes lots of performance issues
            // If a text object is updating itself and its parent needs to update, then if
            // children depth > 0, then the parent update will cause all other children to update
            // which is very expensive. We now do enough checks at the property level to prevent the
            // text from updating unnecessarily, so let's change this to prevent parents from updating
            // all of their children:
            //if (mContainedObjectAsIpso is Text asText && childrenUpdateDepth > 0)
            if (mContainedObjectAsIpso is IText asText)
            {
                // Only if the width or height have changed:
                if (mContainedObjectAsIpso.Width != widthBeforeLayout ||
                    mContainedObjectAsIpso.Height != heightBeforeLayout)
                {
                    asText.SetNeedsRefreshToTrue();
                    asText.UpdatePreRenderDimensions();
                }
            }

            // See the above call to UpdateTextureCoordiantes
            // on why this is called both before and after UpdateDimensions
            if (mContainedObjectAsIpso is ITextureCoordinate)
            {
                UpdateTextureCoordinatesNotDimensionBased();
            }


            UpdatePosition(parentWidth, parentHeight, xOrY, absoluteParentRotation, isParentFlippedHorizontally);

            if (GetIfParentStacks())
            {
                RefreshParentRowColumnDimensionForThis();
            }

            if (this.Parent == null)
            {
                mContainedObjectAsIpso.Rotation = mRotation;
            }
            else
            {
                if (isParentFlippedHorizontally)
                {
                    mContainedObjectAsIpso.Rotation =
                        -mRotation;// + Parent.GetAbsoluteRotation();
                }
                else
                {
                    mContainedObjectAsIpso.Rotation =
                        mRotation;// + Parent.GetAbsoluteRotation();
                }
            }

        }

        if (childrenUpdateDepth > 0)
        {
            UpdateChildren(childrenUpdateDepth, ChildType.All, skipIgnoreByParentSize: false, alreadyUpdated: fullyUpdatedChildren);

            var sizeDependsOnChildren = this.WidthUnits == DimensionUnitType.RelativeToChildren ||
                this.HeightUnits == DimensionUnitType.RelativeToChildren;

            var canOneDimensionChangeOtherDimension = false;

            if (this.mContainedObjectAsIpso == null)
            {
                for (int i = 0; i < this.mWhatThisContains.Count; i++)
                {
                    canOneDimensionChangeOtherDimension = GetIfOneDimensionCanChangeOtherDimension(mWhatThisContains[i]);

                    if (canOneDimensionChangeOtherDimension)
                    {
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < this.Children.Count; i++)
                {
                    var child = Children[i];

                    canOneDimensionChangeOtherDimension = GetIfOneDimensionCanChangeOtherDimension(child);

                    if (canOneDimensionChangeOtherDimension)
                    {
                        break;
                    }
                }
            }

            if (sizeDependsOnChildren && canOneDimensionChangeOtherDimension)
            {
                float widthBeforeSecondLayout = mContainedObjectAsIpso.Width;
                float heightBeforeSecondLayout = mContainedObjectAsIpso.Height;

                UpdateDimensions(parentWidth, parentHeight, xOrY, considerWrappedStacked: true);

                if (widthBeforeSecondLayout != mContainedObjectAsIpso.Width ||
                    heightBeforeSecondLayout != mContainedObjectAsIpso.Height)
                {
                    UpdateChildren(childrenUpdateDepth, ChildType.BothAbsoluteAndRelative, skipIgnoreByParentSize: true);
                }

            }
        }

        // Eventually add more conditions here to make it fire less often
        // like check the width/height of the parent to see if they're 0
        if (updateParent && GetIfShouldCallUpdateOnParent())
        {
            (this.Parent as GraphicalUiElement).UpdateLayout(false, false);
            ChildrenUpdatingParentLayoutCalls++;
        }
        if (this.mContainedObjectAsIpso != null)
        {
            if (widthBeforeLayout != mContainedObjectAsIpso.Width ||
                heightBeforeLayout != mContainedObjectAsIpso.Height)
            {
                if (!isInSizeChange)
                {
                    isInSizeChange = true;
                    SizeChanged?.Invoke(this, null);
                    isInSizeChange = false;
                }
            }

            if (xBeforeLayout != mContainedObjectAsIpso.X ||
                    yBeforeLayout != mContainedObjectAsIpso.Y)
            {
                PositionChanged?.Invoke(this, null);
            }
        }

    }

    #endregion





    bool DoesDimensionNeedUpdateFirstForRatio(DimensionUnitType unitType) =>
        unitType == DimensionUnitType.RelativeToChildren ||
        unitType == DimensionUnitType.PercentageOfOtherDimension ||
        unitType == DimensionUnitType.PercentageOfSourceFile ||
        unitType == DimensionUnitType.MaintainFileAspectRatio ||
        unitType == DimensionUnitType.ScreenPixel;

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
            for (int i = 0; i < mWhatThisContains.Count; i++)
            {
                var child = mWhatThisContains[i];
                // Victor Chelaru
                // January 10, 2017
                // I think we may not want to update any children which
                // have parents, because they'll get updated through their
                // parents...
                if (child.Parent == null || child.Parent == this)
                {
                    if (CanDoFullUpdate(child.GetChildLayoutType(this), child))
                    {
                        child.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1);
                        newlyUpdated?.Add(child);
                    }
                    else
                    {
                        // only update absolute layout, and the child has some relative values, but let's see if 
                        // we can do only one axis:
                        if (CanDoFullUpdate(child.GetChildLayoutType(XOrY.X, this), child))
                        {
                            child.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1, XOrY.X);
                        }
                        else if (CanDoFullUpdate(child.GetChildLayoutType(XOrY.Y, this), child))
                        {
                            child.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1, XOrY.Y);
                        }
                    }
                }
            }
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

                if ((alreadyUpdated == null || alreadyUpdated.Contains(child) == false))
                {
                    // now do all:
                    UpdateChild(child, flagAsUpdated: shouldFlagAsUpdated);
                }
            }


            void UpdateChild(GraphicalUiElement child, bool flagAsUpdated)
            {

                var canDoFullUpdate =
                    CanDoFullUpdate(child.GetChildLayoutType(this), child);


                if (canDoFullUpdate)
                {
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
            unitOffsetY += cellHeight * yIndex + Parent.StackSpacing * (yIndex );
        }
    }

    #endregion

    private void RefreshParentRowColumnDimensionForThis()
    {
        // If it stacks, then update this row/column's dimensions given the index of this
        var indexToUpdate = this.StackedRowOrColumnIndex;

        if (indexToUpdate == -1)
        {
            return;
        }

        var parentGue = EffectiveParentGue;

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
            else
            {
                if (indexToUpdate >= 0 && indexToUpdate < parentGue.StackedRowOrColumnDimensions.Count)
                {
                    parentGue.StackedRowOrColumnDimensions[indexToUpdate] = 0;
                }
            }
            foreach (GraphicalUiElement child in parentGue.Children)
            {
                if (child.Visible)
                {
                    if (child.StackedRowOrColumnIndex == indexToUpdate)
                    {
                        if (parentGue.ChildrenLayout == ChildrenLayout.LeftToRightStack)
                        {
                            parentGue.StackedRowOrColumnDimensions[indexToUpdate] =
                                System.Math.Max(parentGue.StackedRowOrColumnDimensions[indexToUpdate],
                                child.Y + child.GetAbsoluteHeight());
                        }
                        else
                        {
                            parentGue.StackedRowOrColumnDimensions[indexToUpdate] =
                                System.Math.Max(parentGue.StackedRowOrColumnDimensions[indexToUpdate],
                                child.X + child.GetAbsoluteWidth());
                        }

                        // We don't need to worry about the children after this, because the siblings will get updated in order:
                        // This can (on average) make this run 2x as fast
                        if (this == child)
                        {
                            break;
                        }
                    }
                }
            }
        }

    }

    private void GetCellDimensions(int indexInSiblingList, out int xIndex, out int yIndex, out float cellWidth, out float cellHeight)
    {
        var effectiveParent = EffectiveParentGue;
        var xRows = effectiveParent.AutoGridHorizontalCells;
        var yRows = effectiveParent.AutoGridVerticalCells;
        if (xRows < 1) xRows = 1;
        if (yRows < 1) yRows = 1;

        if (effectiveParent.ChildrenLayout == ChildrenLayout.AutoGridHorizontal)
        {
            xIndex = indexInSiblingList % xRows;
            yIndex = indexInSiblingList / xRows;
        }
        else // vertical
        {
            yIndex = indexInSiblingList % yRows;
            xIndex = indexInSiblingList / yRows;
        }
        var parentWidth = effectiveParent.GetAbsoluteWidth() - (xRows - 1) * effectiveParent.StackSpacing;
        var parentHeight = effectiveParent.GetAbsoluteHeight() - (yRows - 1) * effectiveParent.StackSpacing;

        cellWidth = (parentWidth / xRows) ;
        cellHeight = (parentHeight / yRows);

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
                if (((IVisible)siblings[i]).Visible)
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

    private bool GetIfParentHasRatioChildren()
    {
        var effectiveParentGue = this.EffectiveParentGue;

        if (effectiveParentGue?.Children != null)
        {
            // do we care about situations with no parent?
            foreach (var child in effectiveParentGue.Children)
            {
                if (child.WidthUnits == DimensionUnitType.Ratio || child.HeightUnits == DimensionUnitType.Ratio)
                {
                    return true;
                }
            }
        }
        else if (effectiveParentGue != null)
        {
            foreach (var child in effectiveParentGue.ContainedElements)
            {
                if (child.Parent == null && (child.WidthUnits == DimensionUnitType.Ratio || child.HeightUnits == DimensionUnitType.Ratio))
                {
                    return true;
                }
            }
        }

        return false;
    }


    bool GetIfShouldCallUpdateOnParent()
    {
        var asGue = this.Parent as GraphicalUiElement;

        if (asGue != null)
        {
            var shouldUpdateParent =
                // parent needs to be resized based on this position or size
                asGue.GetIfDimensionsDependOnChildren() ||
                // parent stacks its children, so siblings need to adjust their position based on this
                asGue.ChildrenLayout != Gum.Managers.ChildrenLayout.Regular;

            if (!shouldUpdateParent)
            {
                // if any siblings are ratio-based, then we need to
                if (this.Parent == null)
                {
                    if(ElementGueContainingThis != null)
                    {
                        for (int i = 0; i < this.ElementGueContainingThis.mWhatThisContains.Count; i++)
                        {
                            var sibling = this.ElementGueContainingThis.mWhatThisContains[i];
                            if (sibling.WidthUnits == DimensionUnitType.Ratio || sibling.HeightUnits == DimensionUnitType.Ratio)
                            {
                                return true;
                            }
                        }
                    }
                }
                else if (this.Parent is GraphicalUiElement parentGue && parentGue.Children != null)
                {
                    var siblings = parentGue.Children;
                    for (int i = 0; i < siblings.Count; i++)
                    {
                        var siblingAsGraphicalUiElement = siblings[i];
                        if (siblingAsGraphicalUiElement.WidthUnits == DimensionUnitType.Ratio || siblingAsGraphicalUiElement.HeightUnits == DimensionUnitType.Ratio)
                        {
                            return true;
                        }
                    }
                }

            }
            return shouldUpdateParent;
        }
        else
        {
            return false;
        }
    }

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

    // Records the type of update needed when layout resumes
    private void MakeDirty(ParentUpdateType parentUpdateType, int childrenUpdateDepth, XOrY? xOrY)
    {
        if (currentDirtyState == null)
        {
            currentDirtyState = new DirtyState();

            currentDirtyState.XOrY = xOrY;
        }

        currentDirtyState.ParentUpdateType = currentDirtyState.ParentUpdateType | parentUpdateType;
        currentDirtyState.ChildrenUpdateDepth = Math.Max(
            currentDirtyState.ChildrenUpdateDepth, childrenUpdateDepth);

        // If the update is supposed to update all associations, make it null...
        if (xOrY == null)
        {
            currentDirtyState.XOrY = null;
        }
        // If neither are null and they differ, then that means update both, so set it to null
        else if (currentDirtyState.XOrY != null && currentDirtyState.XOrY != xOrY)
        {
            currentDirtyState.XOrY = null;
        }
        // It's not possible to set either X or Y here. That can only happen on initialization
        // of the currentDirtyState
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
            siblings = this.ElementGueContainingThis.mWhatThisContains;
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

        thisIndex = siblings.IndexOf(this);


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
                if (((IVisible)siblings[index]).Visible)
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
                this.StackedRowOrColumnIndex = (whatToStackAfter as GraphicalUiElement).StackedRowOrColumnIndex + 1;


                thisRowOrColumnIndex = (whatToStackAfter as GraphicalUiElement).StackedRowOrColumnIndex + 1;
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
                    thisRowOrColumnIndex = (whatToStackAfter as GraphicalUiElement).StackedRowOrColumnIndex;

                    this.StackedRowOrColumnIndex = (whatToStackAfter as GraphicalUiElement).StackedRowOrColumnIndex;
                    if (parentGue.ChildrenLayout == Gum.Managers.ChildrenLayout.LeftToRightStack)
                    {
                        whatToStackAfterX = whatToStackAfter.X + whatToStackAfter.Width + parentGue.StackSpacing;

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

    #endregion

}



#region Interfaces

// additional interfaces, added here to make it easier to manage multiple projects.
public interface IManagedObject
{
    void AddToManagers();
    void RemoveFromManagers();
}

#endregion