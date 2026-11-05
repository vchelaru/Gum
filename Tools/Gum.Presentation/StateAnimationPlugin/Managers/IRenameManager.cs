using Gum.DataTypes;
using Gum.DataTypes.Variables;
using StateAnimationPlugin.ViewModels;
using System.Collections.Generic;

namespace StateAnimationPlugin.Managers;

/// <summary>
/// Propagates element/instance/state/animation renames into the affected animation view models and
/// animation files.
/// </summary>
public interface IRenameManager
{
    void HandleRename(ElementSave elementSave, string oldName, ElementAnimationsViewModel viewModel);
    void HandleRename(InstanceSave instanceSave, string oldName, ElementAnimationsViewModel viewModel);
    void HandleRename(StateSave stateSave, string oldName, ElementAnimationsViewModel viewModel);
    void HandleRename(StateSaveCategory category, string oldName, ElementAnimationsViewModel viewModel);
    void HandleRename(AnimationViewModel animationViewModel, string oldAnimationName,
        IEnumerable<AnimationViewModel> animations, ElementSave element);

    /// <summary>
    /// Rewrites the keyframes of every other element that plays <paramref name="element"/>'s animation
    /// <paramref name="oldAnimationName"/> through an instance of it, to play
    /// <paramref name="newAnimationName"/>, saving each changed sidecar. Enumerates the references
    /// that exist now, so it serves a rename, its undo and its redo alike.
    /// </summary>
    void RenameAnimationReferencesInOtherElements(ElementSave element, string oldAnimationName, string newAnimationName);
}
