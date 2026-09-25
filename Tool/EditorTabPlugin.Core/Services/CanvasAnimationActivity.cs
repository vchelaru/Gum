using System.Collections.Generic;
using Gum.Wireframe;
using RenderingLibrary.Graphics;

namespace EditorTabPlugin_XNA.Services;

/// <summary>
/// Whether the editor canvas shows anything that changes on its own over time, so a canvas that
/// otherwise draws only on demand keeps drawing every frame while it does (#4989).
/// </summary>
public static class CanvasAnimationActivity
{
    /// <summary>
    /// True when a visible element under <paramref name="root"/> plays a multi-frame animation chain
    /// or a runtime animation. Hidden subtrees are skipped, matching
    /// <see cref="GraphicalUiElement.AnimateSelf"/>.
    /// </summary>
    public static bool IsAnimating(GraphicalUiElement? root)
    {
        if (root == null || (root.RenderableComponent != null && !root.Visible))
        {
            return false;
        }

        if (root.AnimationController.IsPlaying || IsPlayingChain(root.RenderableComponent))
        {
            return true;
        }

        IList<GraphicalUiElement> children = (IList<GraphicalUiElement>?)root.Children ?? root.ContainedElements;
        for (int i = 0; i < children.Count; i++)
        {
            if (IsAnimating(children[i]))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsPlayingChain(object? renderable) => renderable switch
    {
        Sprite sprite => sprite.Animate && sprite.CurrentChain?.Count > 1,
        NineSlice nineSlice => nineSlice.Animate && nineSlice.CurrentChain?.Count > 1,
        _ => false,
    };
}
