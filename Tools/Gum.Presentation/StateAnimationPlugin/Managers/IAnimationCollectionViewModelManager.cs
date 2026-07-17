using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;
using StateAnimationPlugin.ViewModels;

namespace StateAnimationPlugin.Managers;

/// <summary>
/// Loads and saves an element's animations, bridging the on-disk <see cref="ElementAnimationsSave"/>
/// and the editable <see cref="ElementAnimationsViewModel"/>. The pure-data members
/// (<see cref="IAnimationSaveRepository.GetElementAnimationsSave"/>/
/// <see cref="IAnimationSaveRepository.SaveElementAnimations"/>) live on the base
/// <see cref="IAnimationSaveRepository"/> so callers that only need persistence can depend on that
/// narrower slice. Implemented by the tool-side <c>AnimationCollectionViewModelManager</c>
/// (ADR-0005, issue #3754).
/// </summary>
public interface IAnimationCollectionViewModelManager : IAnimationSaveRepository
{
    /// <summary>
    /// Builds an <see cref="ElementAnimationsViewModel"/> for the given element, loading any
    /// persisted animations. Returns null when <paramref name="element"/> is null.
    /// </summary>
    ElementAnimationsViewModel? GetAnimationCollectionViewModel(ElementSave? element);

    /// <summary>
    /// Persists the given view model's animations to the selected element's animation file.
    /// </summary>
    void Save(ElementAnimationsViewModel viewModel);
}
