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

    // Records the type of update needed when layout resumes
    private void MakeDirty(ParentUpdateType parentUpdateType, int childrenUpdateDepth, XOrY? xOrY)
    {
        RecordGlobalDeferral();
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

    public void SuspendLayout(bool recursive = false)
    {
        mIsLayoutSuspended = true;

        if (recursive)
        {
            if (this.Children?.Count > 0)
            {
                var count = Children.Count;
                for (int i = 0; i < count; i++)
                {
                    var asGraphicalUiElement = Children[i];
                    asGraphicalUiElement?.SuspendLayout(true);
                }
            }
            else
            {
                for (int i = mWhatThisContains.Count - 1; i > -1; i--)
                {
                    mWhatThisContains[i].SuspendLayout(true);
                }

            }
        }
    }

    /// <summary>
    /// Clears the layout and font dirty state, resulting in no layout logic being
    /// performed on the next resume layout. This method should only be used 
    /// if you intend to manually perform layouts after a layout resume. Otherwise, calling
    /// this can cause layouts to behave incorrectly
    /// </summary>
    public void ClearDirtyLayoutState()
    {
        currentDirtyState = null;
        isFontDirty = false;
        resumeDescendantsOnResume = false;
    }

    public void ResumeLayout(bool recursive = false)
    {
        mIsLayoutSuspended = false;

        if (recursive)
        {
            if (!IsAllLayoutSuspended)
            {

                ResumeLayoutUpdateIfDirtyRecursive();
            }
            else
            {
                // The global suspension defers the layout and font flush to whoever lifts it, but this
                // call is still the explicit end of a SuspendLayout(true), which suspended every
                // descendant. Without clearing them here they stay suspended forever, every later
                // UpdateLayout early-outs for them, and they keep a size of 0 (#5707). Dirty state is
                // left in place so the eventual flush lays them out.
                ClearLayoutSuspensionRecursive();
            }
        }
        else
        {
            if (isFontDirty)
            {
                if (!IsAllLayoutSuspended)
                {
                    // Cleared first so the layout the load may run doesn't load the font again.
                    isFontDirty = false;
                    this.UpdateToFontValues();
                }
            }
            bool resumeDescendants = resumeDescendantsOnResume && !IsAllLayoutSuspended;
            if (resumeDescendants)
            {
                resumeDescendantsOnResume = false;
            }
            if (currentDirtyState != null)
            {
                UpdateLayout(EffectiveDirtyStateParentUpdateType,
                    currentDirtyState.ChildrenUpdateDepth,
                    currentDirtyState.XOrY);
            }
            if (resumeDescendants)
            {
                // What the user suspended on a descendant stays suspended.
                ResumeChildrenIfDirty(skipSuspendedChildren: true);
            }

            if (!IsAllLayoutSuspended)
            {
                LayoutResumed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    // While RunWithAllLayoutSuspended runs its change, every element given deferred layout or font work
    // under IsAllLayoutSuspended is listed here, so that work runs afterwards even when it lies outside
    // the subtree the caller flushes, such as a ComboBox's text set by a selection change (#6736).
    private static List<GraphicalUiElement>? s_globallyDeferred;
    private bool isListedAsGloballyDeferred;

    private void RecordGlobalDeferral()
    {
        if (s_globallyDeferred != null && IsAllLayoutSuspended && !isListedAsGloballyDeferred)
        {
            isListedAsGloballyDeferred = true;
            s_globallyDeferred.Add(this);
        }
    }

    /// <summary>
    /// Runs <paramref name="change"/> with every layout deferred. Unless an outer caller is already
    /// deferring, it then runs the deferred work under <paramref name="flushRoot"/> and on every other
    /// element the change gave deferred work, ancestors first. A SuspendLayout a caller set stays in
    /// place with its work left for its ResumeLayout.
    /// </summary>
    internal static void RunWithAllLayoutSuspended(Action change, GraphicalUiElement? flushRoot)
    {
        if (IsAllLayoutSuspended)
        {
            change();
            return;
        }

        List<GraphicalUiElement> deferred = new List<GraphicalUiElement>();
        IsAllLayoutSuspended = true;
        s_globallyDeferred = deferred;
        bool completed = false;
        try
        {
            change();
            completed = true;
        }
        finally
        {
            s_globallyDeferred = null;
            IsAllLayoutSuspended = false;
            if (!completed)
            {
                // The flush below won't run, so the elements must be recordable by the next change.
                foreach (GraphicalUiElement element in deferred)
                {
                    element.isListedAsGloballyDeferred = false;
                }
            }
        }

        flushRoot?.ResumeDeferredLayoutKeepingSuspensions();
        RunRecordedDeferredLayout(deferred);
    }

    private static void RunRecordedDeferredLayout(List<GraphicalUiElement> deferred)
    {
        List<(int depth, GraphicalUiElement element)>? pending = null;
        foreach (GraphicalUiElement element in deferred)
        {
            element.isListedAsGloballyDeferred = false;
            // Layout left on a hidden element is resolved when it is shown, so only its font work runs here.
            bool hasWork = element.isFontDirty ||
                (element.currentDirtyState != null && ((IVisible)element).AbsoluteVisible);
            if (hasWork)
            {
                int depth = 0;
                for (GraphicalUiElement? parent = element.Parent; parent != null; parent = parent.Parent)
                {
                    depth++;
                }
                (pending ??= new List<(int, GraphicalUiElement)>()).Add((depth, element));
            }
        }
        if (pending == null)
        {
            return;
        }

        // Ancestors first, so a descendant whose work an ancestor's flush already ran is skipped.
        pending.Sort((a, b) => a.depth.CompareTo(b.depth));
        foreach ((int _, GraphicalUiElement element) in pending)
        {
            if (element.currentDirtyState == null && !element.isFontDirty)
            {
                continue;
            }
            // A suspended element keeps its work, and a suspended ancestor stops the climb, so a
            // SuspendLayout the caller set still holds its work for its ResumeLayout.
            element.ResumeDeferredLayoutKeepingSuspensions();
        }
    }

    /// <summary>
    /// Runs the layout and font work deferred on this element and its descendants while
    /// <see cref="IsAllLayoutSuspended"/> was set, without ending any <see cref="SuspendLayout"/>
    /// a caller set on them. A suspended element keeps its pending work, and its descendants' work,
    /// for its own <see cref="ResumeLayout"/>.
    /// </summary>
    internal void ResumeDeferredLayoutKeepingSuspensions()
    {
        if (IsAllLayoutSuspended)
        {
            return;
        }
        if (mIsLayoutSuspended)
        {
            resumeDescendantsOnResume = true;
            return;
        }
        ResumeLayoutUpdateIfDirtyRecursive(skipSuspendedChildren: true);
    }

    private void ClearLayoutSuspensionRecursive()
    {
        mIsLayoutSuspended = false;

        if (this.Children?.Count > 0)
        {
            var count = Children.Count;
            for (int i = 0; i < count; i++)
            {
                Children[i].ClearLayoutSuspensionRecursive();
            }
        }
        else
        {
            for (int i = mWhatThisContains.Count - 1; i > -1; i--)
            {
                mWhatThisContains[i].ClearLayoutSuspensionRecursive();
            }
        }
    }

    private bool ResumeLayoutUpdateIfDirtyRecursive(bool skipSuspendedChildren = false)
    {
        // mIsLayoutSuspended must be cleared BEFORE calling UpdateFontRecursive so that this
        // element's isFontDirty check inside UpdateFontRecursive passes. Children are still
        // suspended at this point; their suspension is cleared when we recurse below.
        mIsLayoutSuspended = false;
        resumeDescendantsOnResume = false;
        UpdateFontRecursive();

        var didCallUpdateLayout = false;

        if (currentDirtyState != null)
        {
            bool isHidden = IsHiddenForResume();
            // A hidden element's own layout is resolved when it is shown. What its parent has to hear about (a
            // change made to it while suspended) is passed on once; a record left by its parent visiting it while
            // hidden asks nothing of the parent, so it is not passed on again on every resume (#6702).
            if (!isHidden || currentDirtyState.ParentUpdateType != ParentUpdateType.None)
            {
                didCallUpdateLayout = true;
                UpdateLayout(EffectiveDirtyStateParentUpdateType,
                    currentDirtyState.ChildrenUpdateDepth,
                    currentDirtyState.XOrY);
                if (isHidden && currentDirtyState != null)
                {
                    // The parent has been told; what is left is this element's own layout for when it is shown.
                    currentDirtyState.ParentUpdateType = ParentUpdateType.None;
                }
            }
        }

        ResumeChildrenIfDirty(skipSuspendedChildren);

        LayoutResumed?.Invoke(this, EventArgs.Empty);

        return didCallUpdateLayout;
    }

    private bool IsHiddenForResume() =>
        !AreUpdatesAppliedWhenInvisible && mContainedObjectAsIVisible != null &&
        ((IVisible)this).AbsoluteVisible == false && !IsRenderTarget && !this.IsInRenderTargetRecursively();

    private void ResumeChildrenIfDirty(bool skipSuspendedChildren)
    {
        if (this.Children?.Count > 0)
        {
            var count = Children.Count;
            for (int i = 0; i < count; i++)
            {
                var asGraphicalUiElement = Children[i];
                if (skipSuspendedChildren && asGraphicalUiElement.mIsLayoutSuspended)
                {
                    // Left pending as the user suspended it, but its descendants are not the user's: whoever
                    // resumes it gives them what they skipped.
                    asGraphicalUiElement.resumeDescendantsOnResume = true;
                    continue;
                }
                asGraphicalUiElement.ResumeLayoutUpdateIfDirtyRecursive(skipSuspendedChildren);
            }
        }
        else
        {
            int count = mWhatThisContains.Count;
            for (int i = 0; i < count; i++)
            {
                if (skipSuspendedChildren && mWhatThisContains[i].mIsLayoutSuspended)
                {
                    mWhatThisContains[i].resumeDescendantsOnResume = true;
                    continue;
                }
                mWhatThisContains[i].ResumeLayoutUpdateIfDirtyRecursive(skipSuspendedChildren);
            }
        }
    }
}
