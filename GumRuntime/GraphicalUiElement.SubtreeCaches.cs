using System;
using System.Threading;

namespace Gum.Wireframe;

// Facts about an element's subtree that the per-frame walks (AnimateSelf, the cursor walk) cache so an
// unchanged subtree costs nothing (#6177). A cached fact stays valid until something that could change it
// calls InvalidateSubtreeCaches on the affected element, which clears the facts on it and its ancestors:
// adding a child, showing a hidden element, replacing the renderable, assigning a Forms control, turning
// HasEvents on or off, starting or resuming a state animation, and turning an AnimationChain's Animate on.
// Removing a child never adds work or events to the old parent, so it does not invalidate.
public partial class GraphicalUiElement
{
    // Set at the end of AnimateSelf when this element and everything under it had nothing to animate.
    bool _isIdle;

    // Whether this element or anything visible under it has HasEvents set, computed by the cursor walk.
    // 0 is unknown, 1 is none, 2 is some.
    internal byte _subtreeEventsState;

    // Cached so SetContainedObject can hand the same delegate to each renderable it holds.
    Action? _invalidateSubtreeCachesAction;

    // Counts invalidations so a walk can tell that something changed while it ran.
    static int s_invalidationCount;

    internal void InvalidateSubtreeCaches()
    {
        Interlocked.Increment(ref s_invalidationCount);
        for (GraphicalUiElement? element = this; element != null; element = element._parent)
        {
            element._isIdle = false;
            element._subtreeEventsState = 0;
        }
    }

    // A hidden element is skipped by both walks; showing it invalidates, so the caches above it can ignore it.
    internal bool IsHiddenForCaches => mContainedObjectAsIVisible != null && Visible == false;

    // True when AnimateSelf would do nothing for this element's subtree right now.
    bool IsIdle => _isIdle || IsHiddenForCaches;
}
