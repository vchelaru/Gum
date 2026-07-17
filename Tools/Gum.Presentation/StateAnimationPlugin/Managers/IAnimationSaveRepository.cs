using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;

namespace StateAnimationPlugin.Managers;

/// <summary>
/// Loads and persists an element's raw <see cref="ElementAnimationsSave"/> data. This is the
/// pure-data slice of the Gum tool's <c>IAnimationCollectionViewModelManager</c> (which also
/// exposes VM-typed members that stay tool-side), split out so headless ViewModels living in
/// <c>Gum.Presentation</c> can depend on animation persistence without pulling in any WPF-coupled
/// type (ADR-0005, issue #3754).
/// </summary>
public interface IAnimationSaveRepository
{
    /// <summary>
    /// Loads the persisted <see cref="ElementAnimationsSave"/> for the given element, or null when
    /// there is no animation file (or it fails to deserialize).
    /// </summary>
    ElementAnimationsSave? GetElementAnimationsSave(ElementSave element);

    /// <summary>
    /// Persists an already-built <see cref="ElementAnimationsSave"/> to the given element's animation
    /// file, suppressing the resulting file-watch event. Used by undo/redo, which restores a captured
    /// animations snapshot (not a live view model) for the element it is applying to.
    /// </summary>
    void SaveElementAnimations(ElementSave element, ElementAnimationsSave save);
}
