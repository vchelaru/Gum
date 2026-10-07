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

    #region AnimationChain 

#if !FRB
    /// <summary>
    /// Updates the current animation state based on the elapsed time.
    /// </summary>
    /// <param name="secondDifference">The time elapsed since the last update, in seconds.</param>
    private void RunAnimation(double secondDifference)
    {
        AnimationController.Update(secondDifference, this);
    }
#endif

    /// <summary>
    /// Performs AnimationChain (.achx) animation on this and all children recurisvely.
    /// This is typically called on the top-level object (usually Screen) when Gum is running
    /// in a game.
    /// </summary>
    public void AnimateSelf(double secondDifference)
    {
        var asSprite = mContainedObjectAsIpso as ITextureCoordinate;
        var asAnimatable = mContainedObjectAsIpso as IAnimatable;
        //////////////////Early Out/////////////////////
        // Check mContainedObjectAsIVisible - if it's null, then this is a Screen and we should animate it

        // December 6, 2023 - Not sure why this was added here
        // but by checking if this is null, we skip animating screens
        // which breaks recursive animations. We need to early out only
        // if the contained object is not null.
        //if(asSprite== null || asAnimatable == null)
        //{
        //    return;
        //}

        if (mContainedObjectAsIVisible != null && Visible == false)
        {
            return;
        }
        ////////////////End Early Out///////////////////
#if !FRB
        RunAnimation(secondDifference);
#endif

        var didSpriteUpdate = asAnimatable?.AnimateSelf(secondDifference) ?? false;

        if (didSpriteUpdate)
        {
            // update this texture coordinates:

            UpdateTextureValuesFrom(asSprite!);
        }

#if !FRB
        if (this is InteractiveGue interactive
            && interactive.FormsControlAsObject is IUpdateEveryFrame updatable)
        {
            updatable.Activity(secondDifference);
        }
#endif

        if (mContainedObjectAsIpso != null)
        {
            AnimateEach(Children, secondDifference);
        }
        else
        {
            // With no renderable (a code-only element built without one), Children is the empty fallback, so the
            // held instances are reached through mWhatThisContains. Only the parentless ones: the rest
            // are reached through their parent's Children.
            AnimateEach(mWhatThisContains, secondDifference, parentlessOnly: true);
        }
    }

    /// <summary>
    /// Calls <see cref="AnimateSelf(double)"/> on each element in <paramref name="elements"/>, in order.
    /// Animation event handlers (such as AnimationChainFinished) may add or remove elements in the
    /// list while it runs: every element still in the list that was not yet animated advances exactly
    /// once, and no element advances twice. This is the loop Gum uses for an element's children and
    /// for the roots passed to GumService.Update.
    /// </summary>
    /// <param name="elements">The elements to animate.</param>
    /// <param name="secondDifference">The number of seconds to advance animations.</param>
    public static void AnimateEach(IList<GraphicalUiElement> elements, double secondDifference) =>
        AnimateEach(elements, secondDifference, parentlessOnly: false);

    static void AnimateEach(IList<GraphicalUiElement> elements, double secondDifference, bool parentlessOnly)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            GraphicalUiElement element = elements[i];
            if (parentlessOnly && element.Parent != null)
            {
                continue;
            }
            GraphicalUiElement? next = i + 1 < elements.Count ? elements[i + 1] : null;
            element.AnimateSelf(secondDifference);
            i = GetIndexToResumeAfter(elements, element, next, i);
        }
    }

    // Returns where element now sits in elements after it was processed at index, so a loop
    // continuing from there visits each remaining element once even if the list changed meanwhile.
    // If element was removed, returns the index just before next (the element that followed it).
    static int GetIndexToResumeAfter(IList<GraphicalUiElement> elements, GraphicalUiElement element, GraphicalUiElement? next, int index)
    {
        if (index < elements.Count && ReferenceEquals(elements[index], element))
        {
            return index;
        }
        int found = IndexOfReference(elements, element);
        if (found >= 0)
        {
            return found;
        }
        if (next == null)
        {
            // The removed element was last when it was processed.
            return elements.Count - 1;
        }
        found = IndexOfReference(elements, next);
        if (found >= 0)
        {
            return found - 1;
        }
        return System.Math.Min(index, elements.Count) - 1;
    }

    static int IndexOfReference(IList<GraphicalUiElement> elements, GraphicalUiElement element)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (ReferenceEquals(elements[i], element))
            {
                return i;
            }
        }
        return -1;
    }

    public void UpdateTextureValuesFrom(ITextureCoordinate asSprite)
    {
        // suspend layouts while we do this so that previou values don't apply:
        var isSuspended = this.IsLayoutSuspended;
        this.SuspendLayout();

        // The AnimationChain (source file) could get set before the name desired name is set, so tolerate 
        // if there's a missing source rectangle:
        if (asSprite.SourceRectangle != null)
        {
            this.TextureLeft = asSprite.SourceRectangle.Value.Left;
            this.TextureWidth = asSprite.SourceRectangle.Value.Width;

            this.TextureTop = asSprite.SourceRectangle.Value.Top;
            this.TextureHeight = asSprite.SourceRectangle.Value.Height;
        }

        this.FlipHorizontal = asSprite.FlipHorizontal;

        if (this.TextureAddress == TextureAddress.EntireTexture)
        {
            this.TextureAddress = TextureAddress.Custom; // If it's not custom, then the animation chain won't apply. I think we should force this.
        }
        if (isSuspended == false)
        {
            this.ResumeLayout();
        }
    }

#endregion
}
