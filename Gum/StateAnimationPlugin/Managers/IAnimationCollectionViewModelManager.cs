using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;
using StateAnimationPlugin.ViewModels;

namespace StateAnimationPlugin.Managers;

/// <summary>
/// Loads and saves an element's animations, bridging the on-disk <see cref="ElementAnimationsSave"/>
/// and the editable <see cref="ElementAnimationsViewModel"/>.
/// </summary>
public interface IAnimationCollectionViewModelManager
{
    /// <summary>
    /// Builds an <see cref="ElementAnimationsViewModel"/> for the given element, loading any
    /// persisted animations. Returns null when <paramref name="element"/> is null.
    /// </summary>
    ElementAnimationsViewModel? GetAnimationCollectionViewModel(ElementSave? element);

    /// <summary>
    /// Loads the persisted <see cref="ElementAnimationsSave"/> for the given element, or null when
    /// there is no animation file (or it fails to deserialize).
    /// </summary>
    ElementAnimationsSave? GetElementAnimationsSave(ElementSave element);

    /// <summary>
    /// Persists the given view model's animations to the selected element's animation file.
    /// </summary>
    void Save(ElementAnimationsViewModel viewModel);

    /// <summary>
    /// Persists an already-built <see cref="ElementAnimationsSave"/> to the given element's animation
    /// file, suppressing the resulting file-watch event. Used by undo/redo, which restores a captured
    /// animations snapshot (not a live view model) for the element it is applying to.
    /// </summary>
    void SaveElementAnimations(ElementSave element, ElementAnimationsSave save);
}
