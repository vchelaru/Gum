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
}
