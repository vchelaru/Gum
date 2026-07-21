using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Responses;
using Gum.Services.Dialogs;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Gum.Undo;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ToolsUtilities;

namespace StateAnimationPlugin;

/// <summary>
/// Owns the Animations tab's editable state and all of the WPF-free business logic that reacts to
/// element/instance/state edits, undo/redo, on-disk .ganx changes, and the tab's own view-model events
/// (property changes, add-keyframe requests, delete confirmations). Extracted from
/// <c>MainStateAnimationPlugin</c> (issue #3866): none of this touches a WPF type, but every method
/// here used to be an instance method reading the plugin's own private fields rather than taking its
/// dependencies as constructor parameters, which is what blocked the extraction until now.
///
/// <para>
/// The plugin still owns the real platform glue this class deliberately has no seam for: the WPF
/// <c>MainWindow</c>/menu/tab-visibility wiring, and pushing <see cref="ViewModel"/> onto the window's
/// <c>DataContext</c>. That push (and the "please recheck errors" message send, which needs a
/// <c>PluginBase</c> identity this class doesn't have) happens in the plugin's <see cref="ViewModelRefreshed"/>
/// and <see cref="DataSaved"/> subscribers, kept in lockstep with this class's own refresh/save calls.
/// </para>
/// </summary>
public class AnimationTabController
{
    private readonly ISelectedState _selectedState;
    private readonly IUndoManager _undoManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IDialogService _dialogService;
    private readonly IProjectState _projectState;
    private readonly IAnimationCollectionViewModelManager _animationCollectionViewModelManager;
    private readonly IRenameManager _renameManager;
    private readonly IDuplicateService _duplicateService;
    private readonly IAnimationFilePathService _animationFilePathService;
    private readonly Func<ElementAnimationsViewModel> _animationVmFactory;

    private readonly ObservableCollection<string> _availableStates = new();

    // True while an undo/redo is restoring animations to disk and the tab is repainting from it.
    // Guards HandleDataChange from flushing a *new* undo for the change the undo itself just made.
    private bool _isApplyingUndo;

    /// <summary>
    /// The Animations tab's current view model, or null before it has ever been created. Pushed onto
    /// the WPF window's DataContext by the plugin whenever <see cref="ViewModelRefreshed"/> fires.
    /// </summary>
    public ElementAnimationsViewModel? ViewModel { get; private set; }

    /// <summary>
    /// Raised whenever <see cref="RefreshViewModel"/> rebuilds/repaints the tab, so the plugin can push
    /// the (possibly-same-instance) <see cref="ViewModel"/> onto the WPF window's DataContext and
    /// request a scoped error recheck - both real platform/host concerns this class has no seam for.
    /// </summary>
    public event Action? ViewModelRefreshed;

    /// <summary>
    /// Raised after a persisted edit is saved (<see cref="HandleDataChange"/>), so the plugin can
    /// request a full (unscoped) error recheck.
    /// </summary>
    public event Action? DataSaved;

    public AnimationTabController(
        ISelectedState selectedState,
        IUndoManager undoManager,
        IGuiCommands guiCommands,
        IDialogService dialogService,
        IProjectState projectState,
        IAnimationCollectionViewModelManager animationCollectionViewModelManager,
        IRenameManager renameManager,
        IDuplicateService duplicateService,
        IAnimationFilePathService animationFilePathService,
        Func<ElementAnimationsViewModel> animationVmFactory)
    {
        _selectedState = selectedState;
        _undoManager = undoManager;
        _guiCommands = guiCommands;
        _dialogService = dialogService;
        _projectState = projectState;
        _animationCollectionViewModelManager = animationCollectionViewModelManager;
        _renameManager = renameManager;
        _duplicateService = duplicateService;
        _animationFilePathService = animationFilePathService;
        _animationVmFactory = animationVmFactory;
    }

    /// <summary>
    /// Forces a brand-new <see cref="ViewModel"/> from the factory, bypassing <see cref="RefreshViewModel"/>'s
    /// same-element early-out. Used once, on startup, before any element has been selected.
    /// </summary>
    public void CreateInitialViewModel()
    {
        ViewModel = _animationVmFactory();
    }

    /// <summary>
    /// Adds one <see cref="TopLevelName"/> per persisted animation on <paramref name="element"/>, for
    /// the tool's element-navigation search. Mirrors <c>PluginBase.FillTopLevelNames</c>.
    /// </summary>
    public void FillTopLevelNames(ElementSave element, List<TopLevelName> names)
    {
        var animationsSave = _animationCollectionViewModelManager.GetElementAnimationsSave(element);
        if (animationsSave != null)
        {
            foreach (var anim in animationsSave.Animations)
            {
                names.Add(new TopLevelName(anim.Name, "Animation", anim));
            }
        }
    }

    /// <summary>
    /// Implements <c>IAnimationUndoProvider.GetCurrentAnimations</c>: the element-undo snapshot source
    /// for this element's animations (live tab contents when loaded, else the persisted .ganx).
    /// </summary>
    public ElementAnimationsSave? GetCurrentAnimations(ElementSave element)
    {
        // Prefer the live tab contents when this element is the one loaded; otherwise read the .ganx.
        ElementAnimationsSave? save = ViewModel?.Element == element
            ? ViewModel!.ToSave()
            : _animationCollectionViewModelManager.GetElementAnimationsSave(element);

        // Normalize "no animations" to null so a null-vs-null diff compares equal (no spurious undo).
        if (save == null || save.Animations.Count == 0)
        {
            return null;
        }

        return save;
    }

    /// <summary>
    /// Implements <c>IAnimationUndoProvider.ApplyAnimations</c>: restores a captured animations
    /// snapshot during undo/redo, suppressing the undo-lock that <see cref="HandleDataChange"/> would
    /// otherwise take for the resulting save.
    /// </summary>
    public void ApplyAnimations(ElementSave element, ElementAnimationsSave animations)
    {
        // Suppress undo recording for the write itself and the after-undo tab repaint that follows
        // (HandleAfterUndo clears the flag once the repaint is done).
        _isApplyingUndo = true;
        _animationCollectionViewModelManager.SaveElementAnimations(element, animations);
    }

    /// <summary>
    /// Wired to <c>PluginBase.ReactToFileChanged</c>: live-reloads the tab when the selected element's
    /// own .ganx sidecar is edited on disk (issue #3410).
    /// </summary>
    public void HandleFileChanged(FilePath filePath)
    {
        var selectedElement = _selectedState.SelectedElement;
        var selectedElementAnimationFile = selectedElement != null
            ? _animationFilePathService.GetAbsoluteAnimationFileNameFor(selectedElement)
            : null;

        if (AnimationTabRefreshLogic.ShouldReloadAnimationsForChangedFile(filePath, selectedElementAnimationFile))
        {
            // Capture the selection before the rebuild replaces every animation/keyframe instance, then
            // reapply it so the external edit doesn't deselect the user's animation + keyframe (#3410).
            var selection = AnimationTabRefreshLogic.CaptureAnimationSelection(ViewModel);

            // forceReload bypasses CreateViewModel's same-element early-out: the external edit doesn't
            // change the selection, so without forcing it the stale view model would survive and the
            // tab would keep showing the pre-edit animations until the element was reselected. Mirrors
            // the after-undo repaint (#3406).
            RefreshViewModel(forceReload: true);

            if (ViewModel != null)
            {
                AnimationTabRefreshLogic.RestoreAnimationSelection(ViewModel, selection);
            }
        }
    }

    /// <summary>
    /// Wired to <c>PluginBase.VariableSet</c>: recomputes each animation's cumulative preview states
    /// when the changed variable belongs to the default state, or to a state a keyframe references.
    /// </summary>
    public void HandleVariableSet(ElementSave element, InstanceSave? save2, string arg3, object? arg4)
    {
        // This maybe a little inefficient but it should address all issues:
        // eventually this could be more targeted
        var state = _selectedState.SelectedStateSave;
        if (ViewModel == null || state == null) return;

        var isDefault =
            state == _selectedState.SelectedElement?.DefaultState;

        var stateName = state.Name;
        if (_selectedState.SelectedStateCategorySave != null)
        {
            stateName = _selectedState.SelectedStateCategorySave.Name + "/" + stateName;
        }

        foreach (var animation in ViewModel.Animations)
        {
            var shouldRefresh = isDefault;
            if (!shouldRefresh)
            {
                // see if this state is referenced:
                foreach (var keyframe in animation.Keyframes)
                {
                    if (keyframe.StateName == stateName)
                    {
                        shouldRefresh = true;
                        break;
                    }
                }
            }

            if (shouldRefresh)
            {
                animation.RefreshCumulativeStates(element);
            }
        }
    }

    /// <summary>
    /// Wired to <c>PluginBase.ElementDuplicate</c>: copies the source element's animation sidecar for
    /// the newly-duplicated element.
    /// </summary>
    public void HandleElementDuplicate(ElementSave oldElement, ElementSave newElement)
    {
        _duplicateService.HandleDuplicate(oldElement, newElement);
    }

    /// <summary>Wired to <c>PluginBase.ElementRename</c>: propagates the rename into keyframe references.</summary>
    public void HandleElementRename(ElementSave element, string oldName)
    {
        if (ViewModel == null)
        {
            CreateViewModel();
        }

        _renameManager.HandleRename(element, oldName, ViewModel!);
    }

    /// <summary>Wired to <c>PluginBase.InstanceRename</c>: propagates the rename into keyframe references.</summary>
    public void HandleInstanceRename(ElementSave element, InstanceSave instanceSave, string oldName)
    {
        if (ViewModel == null)
        {
            CreateViewModel();
        }

        if (_selectedState.SelectedElement != null)
        {
            _renameManager.HandleRename(instanceSave, oldName, ViewModel!);
        }
    }

    /// <summary>Wired to <c>PluginBase.StateRename</c>: propagates the rename into keyframe references, then repaints the tab.</summary>
    public void HandleStateRename(StateSave stateSave, string oldName)
    {
        if (ViewModel == null)
        {
            CreateViewModel();
        }

        var element = _selectedState.SelectedElement;
        if (element != null && ViewModel != null)
        {
            AnimationTabRefreshLogic.RefreshAfterStateRename(_renameManager, ViewModel, element, stateSave, oldName);
        }

        // Refresh available states / DataContext and re-broadcast. RefreshErrors runs again here,
        // but it is idempotent and rename is not a hot path.
        RefreshViewModel();
    }

    /// <summary>Wired to <c>PluginBase.CategoryRename</c>: propagates the rename into keyframe references.</summary>
    public void HandleCategoryRename(StateSaveCategory category, string oldName)
    {
        if (ViewModel == null)
        {
            CreateViewModel();
        }

        // We only care about this if we have an element. Otherwise, it could be a behavior:
        if (_selectedState.SelectedElement != null)
        {
            _renameManager.HandleRename(category, oldName, ViewModel!);
        }
    }

    /// <summary>Wired to <c>PluginBase.StateAdd</c>: repaints the tab (a new state may be selectable in a keyframe's ComboBox).</summary>
    public void HandleStateAdd(StateSave state)
    {
        RefreshViewModel();
    }

    /// <summary>Wired to <c>PluginBase.StateDelete</c>: repaints the tab (a keyframe may now reference a missing state).</summary>
    public void HandleStateDelete(StateSave state)
    {
        RefreshViewModel();
    }

    /// <summary>
    /// Wired to <c>PluginBase.CategoryDelete</c>: repaints the tab. Deleting a whole category doesn't
    /// fire the granular <see cref="HandleStateDelete"/> for its states, so this is the only signal
    /// (issue #3392).
    /// </summary>
    public void HandleCategoryDelete(StateSaveCategory category)
    {
        RefreshViewModel();
    }

    /// <summary>
    /// Wired to <c>PluginBase.AfterUndo</c>: forces a repaint from the just-restored .ganx and
    /// reselects the previously-selected animation/keyframe (issue #3406).
    /// </summary>
    public void HandleAfterUndo()
    {
        // Capture the selection before the forced rebuild below replaces the view model (and with it
        // every AnimationViewModel/keyframe instance), then reselect by identity afterward so the
        // user's animation + keyframe selection survives undo/redo - mirroring element-undo's state
        // selection restore (#3406). Index + count are captured alongside the keyframe so the reselect
        // can fall back to position when the undo reverted the selected keyframe's own value (see
        // AnimationTabRefreshLogic.RestoreAnimationSelection).
        var selection = AnimationTabRefreshLogic.CaptureAnimationSelection(ViewModel);

        // Repaint the tab from the just-restored .ganx, then re-arm undo recording. The flag spanned
        // the .ganx write (ApplyAnimations) through this repaint so neither flushed a spurious undo.
        // forceReload bypasses CreateViewModel's same-element early-out: an in-place undo leaves the
        // selected element unchanged, so without forcing it the stale view model would survive and the
        // tab would keep showing the pre-undo animations until the element was reselected (#3406).
        RefreshViewModel(forceReload: true);

        if (ViewModel != null)
        {
            AnimationTabRefreshLogic.RestoreAnimationSelection(ViewModel, selection);
        }

        _isApplyingUndo = false;
    }

    /// <summary>
    /// Recomputes the tab's view model (reloading from disk when the selected element changed or a
    /// reload is forced) and refreshes the available-states list, then notifies
    /// <see cref="ViewModelRefreshed"/> so the plugin can repaint the WPF window and re-check errors.
    /// </summary>
    public void RefreshViewModel(bool forceReload = false)
    {
        RefreshAvailableStates();

        CreateViewModel(forceReload);

        ViewModelRefreshed?.Invoke();
    }

    private void RefreshAvailableStates()
    {
        _availableStates.ReplaceWith(AnimationTabRefreshLogic.GetAvailableStates(_selectedState.SelectedElement, ViewModel));
    }

    private void CreateViewModel(bool forceReload = false)
    {
        ElementSave? currentlyReferencedElement = null;
        if (ViewModel != null)
        {
            currentlyReferencedElement = ViewModel.Element;
        }

        var element = _selectedState.SelectedElement;

        if (AnimationTabRefreshLogic.ShouldReloadViewModel(currentlyReferencedElement, element, forceReload))
        {
            if (_projectState.GumProjectSave?.FullFileName == null)
            {
                // OK to assign null, will be fixed down below
                ViewModel = null;
            }
            else
            {
                // OK to assign null, will be fixed down below
                ViewModel = _animationCollectionViewModelManager.GetAnimationCollectionViewModel(element);
            }

            if (ViewModel != null)
            {
                ViewModel.PropertyChanged += HandlePropertyChanged;
                ViewModel.AnyChange += HandleDataChange;
                ViewModel.AddStateKeyframeRequested += HandleAddStateKeyframe;

                foreach (var item in ViewModel.Animations)
                {
                    foreach (var keyframe in item.Keyframes)
                    {
                        keyframe.AvailableStates = _availableStates;
                        keyframe.PropertyChanged += HandleAnimatedKeyframePropertyChanged;
                    }
                }
            }
            currentlyReferencedElement = element;
        }

        if (ViewModel == null)
        {
            ViewModel = _animationVmFactory();
        }
        if (currentlyReferencedElement != null)
        {
            ViewModel?.RefreshErrors(currentlyReferencedElement);
        }
    }

    private void HandleAnimatedKeyframePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AnimatedKeyframeViewModel.StateName):
                // user may have changed a state that is currently being displayed so let's refresh it all!
                SetWireframeStateFromDisplayedAnimTime();
                break;
        }
    }

    private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var variableName = e.PropertyName;

        if (sender is ElementAnimationsViewModel)
        {
            if (variableName == "DisplayedAnimationTime")
            {
                SetWireframeStateFromDisplayedAnimTime();
            }
        }
    }

    private void SetWireframeStateFromDisplayedAnimTime()
    {
        //////////////////////// EARLY OUT
        if (ViewModel?.SelectedAnimation == null)
        {
            return;
        }
        ////////////////////// END EARLY OUT

        var animationTime = ViewModel.DisplayedAnimationTime;

        var animation = ViewModel.SelectedAnimation;
        var element = _selectedState.SelectedElement;

        if (element != null)
        {
            animation.SetStateAtTime(animationTime, element, defaultIfNull: true);
        }
    }

    /// <summary>
    /// Subscribed to the view model's <c>AnyChange</c>: saves the tab's animations to disk when the
    /// changed property is persisted data (not transient UI selection state), then requests an undo
    /// record and an error recheck via <see cref="DataSaved"/>.
    /// </summary>
    public void HandleDataChange(object? sender, PropertyChangedEventArgs e)
    {
        var variableName = e.PropertyName;

        bool shouldSave = true;

        if (sender is ElementAnimationsViewModel)
        {
            shouldSave = variableName == "Animations";
        }

        if (sender is AnimationViewModel)
        {
            // can this happen? I don't see anything on the view model
            if (variableName == "SelectedState" ||
                variableName == nameof(AnimationViewModel.SelectedKeyframe) ||
                variableName == nameof(AnimationViewModel.Length)
                )
            {
                shouldSave = false;
            }
        }

        if (sender is AnimatedKeyframeViewModel)
        {
            shouldSave =
                variableName == nameof(AnimatedKeyframeViewModel.StateName) ||
                variableName == nameof(AnimatedKeyframeViewModel.AnimationName) ||
                variableName == nameof(AnimatedKeyframeViewModel.EventName) ||
                variableName == nameof(AnimatedKeyframeViewModel.SubAnimationViewModel) ||
                variableName == nameof(AnimatedKeyframeViewModel.Time) ||
                variableName == nameof(AnimatedKeyframeViewModel.InterpolationType) ||
                variableName == nameof(AnimatedKeyframeViewModel.Easing);
        }

        if (shouldSave)
        {
            try
            {
                _animationCollectionViewModelManager.Save(ViewModel!);
            }
            catch (Exception exc)
            {
                _guiCommands.PrintOutput($"Could not save animations for {ViewModel?.Element}:\n{exc}");
            }

            // Fold this animation edit into the selected element's undo timeline. Opening and disposing
            // an undo lock fires the element strategy's TryRecord, whose baseline (captured on selection
            // / after the previous record) now differs from the just-saved animations, so it records the
            // change atomically with any element edit and re-baselines. One mechanism covers every
            // persisted gesture - keyframe add/delete/paste, time/interpolation/loop edits. Skipped while
            // an undo is being applied, so restoring animations doesn't record a brand-new undo (#3406).
            if (!_isApplyingUndo)
            {
                using (_undoManager.RequestLock()) { }
            }

            // The edit changed which states/animations the keyframes reference, so an animation
            // error (e.g. a keyframe pointing at a now-missing state) may have appeared or cleared.
            // No structural plugin event fires for an animation edit, so request a full error
            // refresh; the headless checker re-reads the just-saved .ganx.
            DataSaved?.Invoke();
        }
    }

    /// <summary>
    /// Wired to the "add state keyframe" request (the WPF window's button and the view model's own
    /// <c>AddStateKeyframeRequested</c>): validates the current selection, prompts for the new
    /// keyframe's state via <see cref="AddStateKeyframeDialog"/>, and inserts it in time order.
    /// </summary>
    public void HandleAddStateKeyframe(object? sender, EventArgs e)
    {
        string? whyIsntValid = GetWhyAddingTimedStateIsInvalid();

        var selectedAnimation = ViewModel?.SelectedAnimation;
        if (selectedAnimation == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(whyIsntValid))
        {
            _dialogService.ShowMessage(whyIsntValid);
            return;
        }

        AddStateKeyframeDialog dialog = new()
        {
            ElementSave = _selectedState.SelectedElement
        };

        _dialogService.Show(dialog);

        if (dialog.Result is { } newVm)
        {
            if (selectedAnimation.SelectedKeyframe != null)
            {
                // put this after the current animation
                newVm.Time = selectedAnimation.SelectedKeyframe.Time + 1f;
            }
            else if (selectedAnimation.Keyframes.Count != 0)
            {
                newVm.Time = selectedAnimation.Keyframes.Last().Time + 1f;
            }

            selectedAnimation.Keyframes.BubbleSort();

            selectedAnimation.Keyframes.Add(newVm);
            // Call this *before* setting SelectedKeyframe so the available
            // states are assigned. Otherwise
            // StateName will be nulled out.
            HandleAnimationKeyrameAdded(newVm);
            selectedAnimation.SelectedKeyframe = newVm;
        }
    }

    /// <summary>
    /// Wired to the WPF window's <c>AnimationKeyframeAdded</c> (and called directly by
    /// <see cref="HandleAddStateKeyframe"/>): wires a newly-added keyframe's available-states list and
    /// change notifications.
    /// </summary>
    public void HandleAnimationKeyrameAdded(AnimatedKeyframeViewModel newVm)
    {
        newVm.AvailableStates = _availableStates;
        newVm.PropertyChanged += HandleAnimatedKeyframePropertyChanged;
    }

    private string? GetWhyAddingTimedStateIsInvalid()
    {
        string? whyIsntValid = null;

        if (ViewModel == null)
        {
            // invalid state, but don't blow up
            return null;
        }
        if (ViewModel.SelectedAnimation == null)
        {
            whyIsntValid = "You must first select an Animation";
        }

        if (_selectedState.SelectedScreen == null && _selectedState.SelectedComponent == null)
        {
            whyIsntValid = "You must first select a Screen or Component";
        }
        return whyIsntValid;
    }

    /// <summary>
    /// Wired to <c>PluginBase.GetDeleteStateResponse</c>: warns and asks for confirmation before
    /// deleting a state that one or more animations still reference.
    /// </summary>
    public DeleteResponse HandleGetDeleteStateResponse(StateSave state, IStateContainer container)
    {
        var response = new DeleteResponse();
        response.ShouldDelete = true;

        if (container is ElementSave elementSave)
        {
            List<AnimationSave> animatedStatesReferencingState = GetAnimationsReferencingState(state, elementSave);

            if (animatedStatesReferencingState?.Count > 0)
            {
                string message = "Are you sure you want to delete this state? It is used by the following animations. Deleting this state may break the animation:\n\n";

                foreach (var animation in animatedStatesReferencingState)
                {
                    message += animation.Name;
                }

                if (!_dialogService.ShowYesNoMessage(message, "Delete state?"))
                {
                    response.ShouldDelete = false;
                    response.Message = null;
                    response.ShouldShowMessage = false; // user said 'no', no need to show a message...S
                }
            }
        }

        return response;
    }

    /// <summary>
    /// Wired to <c>PluginBase.GetDeleteStateCategoryResponse</c>: warns and asks for confirmation
    /// before deleting a category whose states one or more animations still reference.
    /// </summary>
    public DeleteResponse HandleGetDeleteStateCategoryResponse(StateSaveCategory category, IStateContainer container)
    {
        var response = new DeleteResponse();
        response.ShouldDelete = true;

        var animatedStatesReferencingState = new HashSet<AnimationSave>();

        foreach (var state in category.States)
        {
            foreach (var toAdd in GetAnimationsReferencingState(state, container as ElementSave))
            {
                animatedStatesReferencingState.Add(toAdd);
            }
        }

        if (animatedStatesReferencingState?.Count > 0)
        {
            string message = "Are you sure you want to delete this category? It is used by the following animations. Deleting this category may break the animation:\n\n";

            foreach (var animation in animatedStatesReferencingState)
            {
                message += animation.Name;
            }

            if (!_dialogService.ShowYesNoMessage(message, "Delete category?"))
            {
                response.ShouldDelete = false;
                response.Message = null;
                response.ShouldShowMessage = false; // user said 'no', no need to show a message...S
            }
        }

        return response;
    }

    private List<AnimationSave> GetAnimationsReferencingState(StateSave state, ElementSave? element)
    {
        List<AnimationSave> animatedStatesReferencingState = new List<AnimationSave>();
        if (element != null)
        {
            ElementAnimationsSave? model;
            if (element == ViewModel?.Element)
            {
                model = ViewModel.BackingData!;
            }
            else
            {
                model = _animationCollectionViewModelManager.GetElementAnimationsSave(element);
            }

            animatedStatesReferencingState = new List<AnimationSave>();

            var category = element.Categories.FirstOrDefault(item => item.States.Contains(state));

            var stateName = category != null
                ? $"{category.Name}/{state.Name}"
                : state.Name;

            if (model != null)
            {
                foreach (var animation in model.Animations)
                {
                    foreach (var animatedState in animation.States)
                    {
                        if (animatedState.StateName == stateName)
                        {
                            animatedStatesReferencingState.Add(animation);
                            break;
                        }
                    }
                }
            }
        }

        return animatedStatesReferencingState;
    }
}
