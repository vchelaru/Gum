using Gum.DataTypes;
using Gum.DataTypes.Variables;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace StateAnimationPlugin;

/// <summary>
/// Pure decision logic for refreshing/reloading the Animations tab and reselecting the animation +
/// keyframe across a forced view-model rebuild (undo/redo, an external <c>.ganx</c> edit, or a state
/// rename). Extracted from <c>MainStateAnimationPlugin</c> (the WPF-hosted plugin) since none of it
/// touches WPF types — the plugin calls into this class and only owns the WPF-side repaint
/// (assigning the rebuilt view model to the window's DataContext).
/// </summary>
public static class AnimationTabRefreshLogic
{
    /// <summary>
    /// Decides whether an on-disk file change should live-reload the Animations tab (issue #3410):
    /// true only when <paramref name="changedFile"/> is a <c>.ganx</c> and is the selected element's
    /// own animation sidecar (<paramref name="selectedElementAnimationFile"/>). Other elements' .ganx
    /// files reload lazily when that element is next selected, so they are ignored here.
    /// </summary>
    public static bool ShouldReloadAnimationsForChangedFile(FilePath changedFile,
        FilePath? selectedElementAnimationFile)
    {
        if (changedFile.Extension != "ganx")
        {
            return false;
        }

        return selectedElementAnimationFile != null && changedFile == selectedElementAnimationFile;
    }

    /// <summary>
    /// Applies a state rename to <paramref name="viewModel"/>'s keyframe references and then
    /// recomputes errors, in that order. The order matters: recomputing before the rewrite leaves a
    /// stale "references a missing state" error on the renamed keyframe (issue #3383).
    /// </summary>
    public static void RefreshAfterStateRename(IRenameManager renameManager,
        ElementAnimationsViewModel viewModel, ElementSave element, StateSave stateSave, string oldName)
    {
        renameManager.HandleRename(stateSave, oldName, viewModel);
        viewModel.RefreshErrors(element);
    }

    /// <summary>
    /// The animation/keyframe selection captured before a forced view-model rebuild (undo/redo or an
    /// external .ganx reload). Carries both the identity (animation name + keyframe content) and the
    /// position (animation/keyframe index + sibling counts) so <see cref="RestoreAnimationSelection"/>
    /// can fall back to the slot when the identity changed — e.g. the selected animation was renamed.
    /// </summary>
    public readonly record struct AnimationSelectionState(
        string? AnimationName,
        AnimatedKeyframeViewModel? Keyframe,
        int KeyframeIndex,
        int KeyframeCount,
        int AnimationIndex = -1,
        int AnimationCount = 0);

    /// <summary>
    /// Snapshots the currently-selected animation and keyframe (identity + position) so the selection
    /// can be reapplied after a forced view-model rebuild replaces every animation/keyframe instance.
    /// Pairs with <see cref="RestoreAnimationSelection"/>.
    /// </summary>
    public static AnimationSelectionState CaptureAnimationSelection(ElementAnimationsViewModel? viewModel)
    {
        var selectedAnimation = viewModel?.SelectedAnimation;
        var selectedKeyframe = selectedAnimation?.SelectedKeyframe;
        return new AnimationSelectionState(
            selectedAnimation?.Name,
            selectedKeyframe,
            selectedKeyframe != null ? selectedAnimation!.Keyframes.IndexOf(selectedKeyframe) : -1,
            selectedAnimation?.Keyframes.Count ?? 0,
            selectedAnimation != null && viewModel != null ? viewModel.Animations.IndexOf(selectedAnimation) : -1,
            viewModel?.Animations.Count ?? 0);
    }

    /// <summary>
    /// Reselects, on a freshly-rebuilt <paramref name="viewModel"/>, the animation and keyframe captured
    /// in <paramref name="selection"/>. The animation is matched by name; if that fails because it was
    /// renamed (e.g. an external .ganx edit), it falls back to the captured animation index when the
    /// animation count is unchanged. The keyframe is then matched by content; if that fails because the
    /// undo reverted the selected keyframe's <em>own</em> value (e.g. its time), it falls back to the
    /// captured index — but only when the keyframe count is unchanged, so an add/delete (which
    /// changes the count) drops that selection rather than grabbing a neighbor. Returns the matched
    /// keyframe (or null). Best-effort, mirroring element-undo's silent selection drop when the selected
    /// object no longer exists.
    /// </summary>
    /// <remarks>
    /// The keyframe is selected on the animation <em>before</em> the animation is made the active
    /// SelectedAnimation. That ordering matters: setting SelectedAnimation rebinds the keyframes
    /// ListBox's ItemsSource, and the ListBox initializes its SelectedItem from the (two-way) bound
    /// SelectedKeyframe at bind time. If SelectedKeyframe is still null then, the ListBox settles on no
    /// selection and a later assignment gets reset; pre-setting it means the ListBox binds straight to
    /// the right, already-present keyframe — so the selection (and the right-side property panel) sticks
    /// without any dispatcher timing games (#3406).
    /// </remarks>
    public static AnimatedKeyframeViewModel? RestoreAnimationSelection(ElementAnimationsViewModel viewModel,
        AnimationSelectionState selection)
    {
        if (selection.AnimationName == null)
        {
            return null;
        }

        var animation = viewModel.Animations.FirstOrDefault(item => item.Name == selection.AnimationName);

        // The selected animation may have been renamed by an external .ganx edit, so the captured name
        // matches nothing. Fall back to the captured slot when the animation count is unchanged, keeping
        // the same row selected through the rename (#3410). A count change means an add/delete, where
        // grabbing a neighbor by index would be wrong, so the selection drops instead.
        if (animation == null
            && viewModel.Animations.Count == selection.AnimationCount
            && selection.AnimationIndex >= 0
            && selection.AnimationIndex < viewModel.Animations.Count)
        {
            animation = viewModel.Animations[selection.AnimationIndex];
        }

        if (animation == null)
        {
            return null;
        }

        AnimatedKeyframeViewModel? matched = null;
        if (selection.Keyframe != null)
        {
            matched = animation.Keyframes.FirstOrDefault(item => AreSameKeyframe(item, selection.Keyframe));

            if (matched == null
                && animation.Keyframes.Count == selection.KeyframeCount
                && selection.KeyframeIndex >= 0
                && selection.KeyframeIndex < animation.Keyframes.Count)
            {
                matched = animation.Keyframes[selection.KeyframeIndex];
            }
        }

        animation.SelectedKeyframe = matched;
        viewModel.SelectedAnimation = animation;

        return matched;
    }

    /// <summary>
    /// Identity match for a keyframe across a view-model rebuild: same discriminator (state /
    /// sub-animation / event name) and time. Used to reselect the previously-selected keyframe on the
    /// new instances after an undo/redo reload.
    /// </summary>
    private static bool AreSameKeyframe(AnimatedKeyframeViewModel first, AnimatedKeyframeViewModel second)
    {
        return first.StateName == second.StateName
            && first.AnimationName == second.AnimationName
            && first.EventName == second.EventName
            && first.Time == second.Time;
    }

    /// <summary>
    /// Builds the state names shown in each keyframe's state ComboBox: every state on the element,
    /// plus any state still referenced by a keyframe even though it no longer exists on the element.
    /// Keeping a referenced-but-missing state in the list matters because the ComboBox is editable
    /// with its Text bound to the keyframe's StateName — if the referenced item left the ItemsSource,
    /// the ComboBox would coerce its Text (and thus StateName) to empty, collapsing a broken state
    /// keyframe into an event (issue #3392). The keyframe stays flagged as broken via
    /// HasValidState/RefreshErrors, which is computed against the element, not this list.
    /// </summary>
    public static List<string> GetAvailableStates(ElementSave? element, ElementAnimationsViewModel? viewModel)
    {
        var states = new List<string>();

        if (element != null)
        {
            states.AddRange(element.States.Select(item => item.Name));

            foreach (var category in element.Categories)
            {
                states.AddRange(category.States.Select(item => category.Name + "/" + item.Name));
            }
        }

        // Keep any state still referenced by a keyframe even though it no longer exists on the
        // element (e.g. its category was just deleted). The editable state ComboBox binds its Text to
        // the keyframe's StateName; if the referenced item left this list it would coerce StateName to
        // empty, collapsing the broken state keyframe into an event (issue #3392).
        if (viewModel != null)
        {
            foreach (var animation in viewModel.Animations)
            {
                foreach (var keyframe in animation.Keyframes)
                {
                    if (!string.IsNullOrEmpty(keyframe.StateName) && !states.Contains(keyframe.StateName))
                    {
                        states.Add(keyframe.StateName);
                    }
                }
            }
        }

        return states;
    }

    /// <summary>
    /// Decides whether <c>MainStateAnimationPlugin.CreateViewModel</c> should rebuild the view model
    /// from the element's .ganx. Normally the view model is only reloaded when the selected element
    /// changes; an in-place undo/redo restores the .ganx without changing the selection, so the
    /// after-undo path passes <paramref name="forceReload"/> to repaint the tab immediately rather than
    /// keeping the stale view model until the element is reselected (#3406).
    /// </summary>
    public static bool ShouldReloadViewModel(ElementSave? currentlyReferencedElement,
        ElementSave? selectedElement, bool forceReload)
    {
        return currentlyReferencedElement != selectedElement || forceReload;
    }
}
