using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;

namespace Gum.Undo;

/// <summary>
/// Headless seam that lets <see cref="ElementUndoStrategy"/> capture and restore an element's
/// animations without knowing how they are stored (the <c>.ganx</c> sidecar) or rendered (the WPF
/// Animations tab). Implemented by the animation plugin and injected into the strategy via DI.
/// Animations are folded into the element's undo snapshot so element edits and animation edits
/// share one atomic timeline — required because animation keyframes reference element states by
/// name and would otherwise desync from a rename/delete. See #3406 (PR 2 of 2 toward #3399).
/// </summary>
public interface IAnimationUndoProvider
{
    /// <summary>
    /// Returns the element's live animation edit state — the in-memory tab contents when that
    /// element's Animations tab is loaded, otherwise the deserialized <c>.ganx</c>. Returns null
    /// when the element has no animations (so a null-vs-null diff compares equal and records nothing).
    /// </summary>
    ElementAnimationsSave? GetCurrentAnimations(ElementSave element);

    /// <summary>
    /// Writes <paramref name="animations"/> back as the element's animations (persisting the
    /// <c>.ganx</c> and repainting the tab). An empty <see cref="ElementAnimationsSave"/> restores
    /// the element to having no animations.
    /// </summary>
    void ApplyAnimations(ElementSave element, ElementAnimationsSave animations);
}

/// <summary>
/// Lets the animation plugin register itself as the active <see cref="IAnimationUndoProvider"/> at
/// plugin StartUp — after <see cref="UndoManager"/> (and its <see cref="ElementUndoStrategy"/>) have
/// already been constructed by DI. The registrar and the provider resolve to the same late-bound
/// relay singleton (see <c>AnimationUndoProviderRelay</c>).
/// </summary>
public interface IAnimationUndoProviderRegistrar
{
    void Register(IAnimationUndoProvider provider);
}
