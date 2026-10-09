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
            if (currentDirtyState != null)
            {
                UpdateLayout(EffectiveDirtyStateParentUpdateType,
                    currentDirtyState.ChildrenUpdateDepth,
                    currentDirtyState.XOrY);
            }
        }
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

    private bool ResumeLayoutUpdateIfDirtyRecursive()
    {
        // mIsLayoutSuspended must be cleared BEFORE calling UpdateFontRecursive so that this
        // element's isFontDirty check inside UpdateFontRecursive passes. Children are still
        // suspended at this point; their suspension is cleared when we recurse below.
        mIsLayoutSuspended = false;
        UpdateFontRecursive();

        var didCallUpdateLayout = false;

        if (currentDirtyState != null)
        {
            didCallUpdateLayout = true;
            UpdateLayout(EffectiveDirtyStateParentUpdateType,
                currentDirtyState.ChildrenUpdateDepth,
                currentDirtyState.XOrY);
        }

        if (this.Children?.Count > 0)
        {
            var count = Children.Count;
            for (int i = 0; i < count; i++)
            {
                var asGraphicalUiElement = Children[i];
                asGraphicalUiElement.ResumeLayoutUpdateIfDirtyRecursive();
            }
        }
        else
        {
            int count = mWhatThisContains.Count;
            for (int i = 0; i < count; i++)
            {
                mWhatThisContains[i].ResumeLayoutUpdateIfDirtyRecursive();
            }
        }

        return didCallUpdateLayout;
    }
}
