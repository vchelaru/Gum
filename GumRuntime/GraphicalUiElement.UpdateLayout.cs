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

    #region Repeated child layouts (#5927)

    // A parent lays each child out more than once per UpdateLayout (to measure it, then to place it), and a
    // child that does the same for its own children doubles the work at every level. The later visit of a
    // child is cut down to placing it when the earlier one, in the same session, left everything in the
    // child as it found it and the child would read the same from its parent now: laying out an element
    // that is already laid out, from the same inputs, changes nothing.
    //
    // A session is one outermost UpdateLayout. Any entry other than a parent laying out its child (a property
    // setter, a climb from a descendant, an event handler's edit) starts a new one, so nothing recorded
    // before it is trusted after it.

    /// <summary>
    /// Test hook. When false every visit of a child is a full layout, which is the result the shortcut has to
    /// reproduce exactly.
    /// </summary>
    internal static bool SkipRepeatedChildLayouts = true;

    // Session numbers come from one counter so that a number recorded on one thread never matches another's.
    private static long s_lastSession;
    [ThreadStatic] private static long s_session;

    // Counts every size or position change a layout makes. A visit that leaves it alone (other than placing the
    // visited element itself) changed nothing inside the element.
    [ThreadStatic] private static int s_layoutChangeCount;

    // Set once UpdateLayout gets past its early outs, so a parent can tell a layout from a skipped call.
    private bool _layoutRan;

    // Whether the last layout placed this element while it was already the size it ended at. A layout measures
    // twice, and where the element is placed follows the size at that moment.
    private bool _placedAtFinalSize;

    // What the last layout read from the parent, and (once the parent finds that the visit changed nothing)
    // the session and axis it ran for. A session of -1 means there is nothing to skip.
    private float _settledParentWidth;
    private float _settledParentHeight;
    private long _settledSession = -1;
    private int _settledAxis;

    #endregion

    /// <summary>
    /// Performs an update to this, and optionally to its parent and children depending on the parameters.
    /// </summary>
    /// <param name="parentUpdateType">A filter determining whether whether to update the parent. If All is passed, then
    /// the parent will always update. If other properties are passed, then the update happens only if the parent matches
    /// the update type. For example if ParentUpdateType.IfParentStacks is passed, then an update happens if the parent stacks its children.</param>
    /// <param name="childrenUpdateDepth"></param>
    /// <param name="xOrY"></param>
    /// <param name="gateClimbOnSizeChange">#3066: false for the originating call (the element whose
    /// property changed / was added), which always climbs one level so its parent re-measures; true
    /// for the propagated climbs above that, which only continue upward if their own measured size or
    /// position actually changed. This suppresses the O(N) relayout of a parent's other children when
    /// a descendant change does not alter the intermediate element's contribution to the parent.</param>
    public void UpdateLayout(ParentUpdateType parentUpdateType, int childrenUpdateDepth, XOrY? xOrY = null, bool gateClimbOnSizeChange = false)
    {
        // Anything other than a parent laying out its child starts a new session (see Repeated child layouts).
        s_session = System.Threading.Interlocked.Increment(ref s_lastSession);
        UpdateLayoutWithinSession(parentUpdateType, childrenUpdateDepth, xOrY, gateClimbOnSizeChange);
    }

    /// <summary>
    /// <see cref="UpdateLayout(ParentUpdateType, int, XOrY?, bool)"/> for a parent laying out its child, which
    /// continues the parent's session instead of starting one.
    /// </summary>
    private void UpdateLayoutWithinSession(ParentUpdateType parentUpdateType, int childrenUpdateDepth, XOrY? xOrY, bool gateClimbOnSizeChange)
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
            isSuspended = !updateParent && !AreUpdatesAppliedWhenInvisible &&
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

        #region Originating-call parent climb (early-out; see #3066)

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
            // #3066: The originating call (gateClimbOnSizeChange == false) climbs unconditionally so
            // the parent re-measures and accounts for this change (add/remove/visibility/size/etc.) —
            // this preserves the original eager behavior for the element that actually changed. Each
            // propagated climb above that runs with gateClimbOnSizeChange == true and falls through to
            // measure itself; it only continues upward (at the end of this method) if its own size or
            // position changed, so a parent's other children aren't relaid out when nothing material
            // changed for them.
            if (!gateClimbOnSizeChange)
            {
                // GetIfShouldCallUpdateOnParent only returns true when there is an effective parent.
                var asGue = this.EffectiveParentGue!;
                // Just climb up one and update from there
                asGue.UpdateLayout(parentUpdateType, childrenUpdateDepth + 1, gateClimbOnSizeChange: true);
                ChildrenUpdatingParentLayoutCalls++;
                return;
            }
        }
        // This should be *after* the return when updating the parent otherwise we double-count layouts
        UpdateLayoutCallCount++;

        #endregion

        // #2999 — realize any font deferred while layout was suspended, now, before this element
        // measures itself. Font properties set under suspension only flag isFontDirty; a bare
        // UpdateLayout() (what callers and the font-performance docs use on resume) must load the
        // font so the element sizes from the real glyphs instead of the fallback font. isFontDirty
        // is cleared first so the font assignment's own (suppressed) re-entrant layout can't re-load.
        if (isFontDirty)
        {
            isFontDirty = false;
            bool wasSuppressingLayoutFromFontChange = SuppressLayoutFromFontChange;
            SuppressLayoutFromFontChange = true;
            try
            {
                UpdateToFontValues();
            }
            finally
            {
                SuppressLayoutFromFontChange = wasSuppressingLayoutFromFontChange;
            }
        }

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

        GetParentLayoutInputs(out float parentWidth, out float parentHeight, out float absoluteParentRotation,
            out bool isParentFlippedHorizontally);

        _layoutRan = true;
        _settledSession = -1;
        _settledParentWidth = parentWidth;
        _settledParentHeight = parentHeight;
        float widthAtPlacement = 0;
        float heightAtPlacement = 0;

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

            var widthDependsOnChildren = widthDependencyType == HierarchyDependencyType.DependsOnChildren ||
                this.WidthUnits == DimensionUnitType.RelativeToMaxParentOrChildren;
            var heightDependsOnChildren = heightDependencyType == HierarchyDependencyType.DependsOnChildren ||
                this.HeightUnits == DimensionUnitType.RelativeToMaxParentOrChildren;
            var hasChildDependency = widthDependsOnChildren || heightDependsOnChildren;

            if (!widthDependsOnChildren && !heightDependsOnChildren)
            {
                UpdateDimensions(parentWidth, parentHeight, null, true);
            }
            else if (!widthDependsOnChildren)
            {
                UpdateDimensions(parentWidth, parentHeight, XOrY.X, considerWrappedStacked: false);
            }
            if (!heightDependsOnChildren)
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
                    this.Children.Count > 1)
                {

                    var firstChild = GetFirstVisibleChild();

                    if (firstChild == null)
                    {
                        // Nothing visible to measure.
                    }
                    else if (firstChild.GetChildLayoutType(this) == ChildType.Absolute)
                    {
                        firstChild.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1);
                        fullyUpdatedChildren.Add(firstChild);
                    }
                    else
                    {
                        firstChild.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1, XOrY.Y);
                    }
                }
                else
                {
                    UpdateChildren(childrenUpdateDepth, ChildType.Absolute, skipIgnoreByParentSize: true, newlyUpdated: fullyUpdatedChildren);
                }
            }

            // This will update according to all absolute children
            // Now that the children have been updated, we can do any dimensions that still need updating based on the children changes:

            if (widthDependsOnChildren)
            {
                UpdateDimensions(parentWidth, parentHeight, XOrY.X, considerWrappedStacked: false);
            }
            if (heightDependsOnChildren)
            {
                UpdateDimensions(parentWidth, parentHeight, XOrY.Y, considerWrappedStacked: false);
            }

            if (this.WrapsChildren && (this.ChildrenLayout == ChildrenLayout.LeftToRightStack || this.ChildrenLayout == ChildrenLayout.TopToBottomStack))
            {
                var mainSizeUsedForWrapping = this.ChildrenLayout == ChildrenLayout.LeftToRightStack
                    ? mContainedObjectAsIpso.Width
                    : mContainedObjectAsIpso.Height;
                // Now we can update all children that are wrapped:
                UpdateChildren(childrenUpdateDepth, ChildType.StackedWrapped, skipIgnoreByParentSize: false);
                if (widthDependsOnChildren || heightDependsOnChildren)
                {
                    // Both axes: wrapping couples them, so a main-axis change moves children across lines
                    // and changes the cross-axis size even when only one axis was requested.
                    UpdateDimensions(parentWidth, parentHeight, xOrY: null, considerWrappedStacked: true);

                    var mainSize = this.ChildrenLayout == ChildrenLayout.LeftToRightStack
                        ? mContainedObjectAsIpso.Width
                        : mContainedObjectAsIpso.Height;
                    if (mainSize != mainSizeUsedForWrapping)
                    {
                        // The children wrapped against a main-axis size measured before they were (a
                        // stack sized to its children with a max), so wrap and measure again at the
                        // measured size.
                        UpdateChildren(childrenUpdateDepth, ChildType.StackedWrapped, skipIgnoreByParentSize: false);
                        UpdateDimensions(parentWidth, parentHeight, xOrY: null, considerWrappedStacked: true);
                    }
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


            widthAtPlacement = mContainedObjectAsIpso.Width;
            heightAtPlacement = mContainedObjectAsIpso.Height;
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
                this.WidthUnits == DimensionUnitType.RelativeToMaxParentOrChildren ||
                this.HeightUnits == DimensionUnitType.RelativeToChildren ||
                this.HeightUnits == DimensionUnitType.RelativeToMaxParentOrChildren;

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

            // A wrapping stack's lines form against the positions its children had when it wrapped. A child
            // that was not part of the wrap pass (a Ratio or percent size, a position from the parent)
            // is only laid out in the pass above, and it moves the children stacked after it, so the
            // lines and with them the stack's size can differ from what was measured.
            var isWrappingStack = WrapsChildren &&
                (ChildrenLayout == ChildrenLayout.LeftToRightStack || ChildrenLayout == ChildrenLayout.TopToBottomStack);

            // Without a renderable (such as a Screen) there is no size of our own to re-measure.
            if (sizeDependsOnChildren && (canOneDimensionChangeOtherDimension || isWrappingStack) && mContainedObjectAsIpso != null)
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

        // #3066: propagated climb (see the gate near the top of this method). We were reached via a
        // descendant's climb (gateClimbOnSizeChange == true) and have now laid out locally; continue
        // upward only if our own measured size or position actually changed. Otherwise our
        // contribution to the parent is unchanged and relaying out the parent's other children (e.g.
        // every sibling in a stack) would be wasted O(N) work. Without a renderable we can't measure
        // a delta, so climb to stay safe.
        var sizeOrPositionChanged = mContainedObjectAsIpso == null
            || widthBeforeLayout != mContainedObjectAsIpso.Width
            || heightBeforeLayout != mContainedObjectAsIpso.Height
            || xBeforeLayout != mContainedObjectAsIpso.X
            || yBeforeLayout != mContainedObjectAsIpso.Y;

        // RelativeToMaxParentOrChildren takes the max of this element's own content size and its
        // parent's size, so a content change here can be masked by the (stale) parent size — our own
        // measured size won't change even though the parent's RelativeToChildren size must. Always
        // climb in that case so the parent re-measures from content.
        var dependsOnMaxOfParentOrChildren = this.WidthUnits == DimensionUnitType.RelativeToMaxParentOrChildren
            || this.HeightUnits == DimensionUnitType.RelativeToMaxParentOrChildren;

        if (gateClimbOnSizeChange && updateParent && GetIfShouldCallUpdateOnParent()
            && (sizeOrPositionChanged || dependsOnMaxOfParentOrChildren))
        {
            this.EffectiveParentGue?.UpdateLayout(parentUpdateType, childrenUpdateDepth + 1, gateClimbOnSizeChange: true);
            ChildrenUpdatingParentLayoutCalls++;
        }
        if (this.mContainedObjectAsIpso != null)
        {
            _placedAtFinalSize = widthAtPlacement == mContainedObjectAsIpso.Width && heightAtPlacement == mContainedObjectAsIpso.Height;

            if (widthBeforeLayout != mContainedObjectAsIpso.Width ||
                heightBeforeLayout != mContainedObjectAsIpso.Height)
            {
                s_layoutChangeCount++;
                if (!isInSizeChange)
                {
                    isInSizeChange = true;
                    SizeChanged?.Invoke(this, EventArgs.Empty);
                    isInSizeChange = false;
                }
            }

            if (xBeforeLayout != mContainedObjectAsIpso.X ||
                    yBeforeLayout != mContainedObjectAsIpso.Y)
            {
                s_layoutChangeCount++;
                PositionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

    }

    #endregion
}
