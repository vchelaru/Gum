using CommunityToolkit.Mvvm.Messaging;
using FlatRedBall.Glue.StateInterpolation;
using Gum;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Messages;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Responses;
using Gum.Services.Dialogs;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.Services;
using StateAnimationPlugin.ViewModels;
using StateAnimationPlugin.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ToolsUtilities;


namespace StateAnimationPlugin;

[Export(typeof(PluginBase))]
public class MainStateAnimationPlugin : PluginBase, IAnimationUndoProvider
{
    private readonly ISelectedState _selectedState;
    private readonly INameVerifier _nameVerifier;
    private readonly IMessenger _messenger;
    private readonly IOutputManager _outputManager;
    private readonly IFileWatchManager _fileWatchManager;
    private readonly IUndoManager _undoManager;
    private readonly IAnimationUndoProviderRegistrar _animationUndoProviderRegistrar;
    private readonly Func<ElementAnimationsViewModel> _animationVmFactory;

    // True while an undo/redo is restoring animations to disk and the tab is repainting from it.
    // Guards HandleDataChange from flushing a *new* undo for the change the undo itself just made.
    private bool _isApplyingUndo;

    #region Fields
    private readonly DuplicateService _duplicateService;
    private readonly AnimationFilePathService _animationFilePathService;
    private readonly ElementDeleteService _elementDeleteService;
    private readonly IRenameManager _renameManager;
    private readonly ISettingsManager _settingsManager;
    private readonly IProjectState _projectState;
    private readonly IProjectManager _projectManager;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IAnimationCollectionViewModelManager _animationCollectionViewModelManager;
    ElementAnimationsViewModel? _viewModel;

    StateAnimationPlugin.Views.MainWindow? _mainWindow;
    private PluginTab? pluginTab;
    private MenuItem? menuItem;

    #endregion

    #region Properties

    public override string FriendlyName
    {
        get { return "State Animation Plugin"; }
    }

    // 0.0.0.2: Renaming Gum file now renames its animations
    public override Version Version
    {
        get { return new Version(0, 0, 0, 2); }
    }

    public override void FillTopLevelNames(ElementSave element, List<TopLevelName> names)
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

    ObservableCollection<string> AvailableStates { get; set; } = new ObservableCollection<string>();

    #endregion

    #region StartUp/ShutDown

    [ImportingConstructor]
    public MainStateAnimationPlugin(
        ISelectedState selectedState,
        INameVerifier nameVerifier,
        IMessenger messenger,
        IOutputManager outputManager,
        IFileWatchManager fileWatchManager,
        IProjectState projectState,
        IProjectManager projectManager,
        IWireframeObjectManager wireframeObjectManager,
        IUndoManager undoManager,
        IAnimationUndoProviderRegistrar animationUndoProviderRegistrar)
    {
        _selectedState = selectedState;
        _nameVerifier = nameVerifier;
        _messenger = messenger;
        _outputManager = outputManager;
        _fileWatchManager = fileWatchManager;
        _projectState = projectState;
        _projectManager = projectManager;
        _wireframeObjectManager = wireframeObjectManager;
        _undoManager = undoManager;
        _animationUndoProviderRegistrar = animationUndoProviderRegistrar;

        _animationFilePathService = new AnimationFilePathService(_selectedState);
        _duplicateService = new DuplicateService(_dialogService, _projectManager);
        _elementDeleteService = new ElementDeleteService(_animationFilePathService, _dialogService);
        _settingsManager = new SettingsManager();

        // The factory closure reads _animationCollectionViewModelManager and _renameManager lazily
        // (when invoked, after both are assigned just below), which breaks the
        // ACVMM -> ElementAnimationsViewModel -> RenameManager construction cycle without a Lazy<T>.
        // Each call gets a fresh DispatcherUiTimer: ElementAnimationsViewModel is recreated per
        // selected-element switch (see AnimationCollectionViewModelManager.GetAnimationCollectionViewModel),
        // and a shared timer would let two live view models fight over the same Tick subscription.
        _animationVmFactory = () => new ElementAnimationsViewModel(
            _nameVerifier, _dialogService, _animationCollectionViewModelManager, _renameManager,
            _selectedState, _wireframeObjectManager, _outputManager, _animationFilePathService,
            new DispatcherUiTimer());
        _animationCollectionViewModelManager = new AnimationCollectionViewModelManager(
            _selectedState, _outputManager, _fileWatchManager, _animationFilePathService, _animationVmFactory);
        _renameManager = new RenameManager(
            _selectedState, _outputManager, _animationFilePathService, _animationCollectionViewModelManager, _projectManager);
    }

    public override void StartUp()
    {
        // Register as the live animation provider so the element undo strategy can fold this
        // element's animations into its snapshot (#3406). UndoManager was constructed at DI time,
        // before any plugin existed, so it holds a relay that this call now points at us.
        _animationUndoProviderRegistrar.Register(this);

        CreateMenuItems();
        CreateAnimationWindow();
        AssignEvents();
    }

    #region IAnimationUndoProvider

    ElementAnimationsSave? IAnimationUndoProvider.GetCurrentAnimations(ElementSave element)
    {
        // Prefer the live tab contents when this element is the one loaded; otherwise read the .ganx.
        ElementAnimationsSave? save = _viewModel?.Element == element
            ? _viewModel!.ToSave()
            : _animationCollectionViewModelManager.GetElementAnimationsSave(element);

        // Normalize "no animations" to null so a null-vs-null diff compares equal (no spurious undo).
        if (save == null || save.Animations.Count == 0)
        {
            return null;
        }

        return save;
    }

    void IAnimationUndoProvider.ApplyAnimations(ElementSave element, ElementAnimationsSave animations)
    {
        // Suppress undo recording for the write itself and the after-undo tab repaint that follows
        // (HandleAfterUndo clears the flag once the repaint is done).
        _isApplyingUndo = true;
        _animationCollectionViewModelManager.SaveElementAnimations(element, animations);
    }

    #endregion

    private void CreateMenuItems()
    {
        menuItem = AddMenuItem("View", "View Animations");

        menuItem.Click += HandleToggleTabVisibility;
    }

    private void AssignEvents()
    {
        this.ElementSelected += HandleElementSelected;

        this.InstanceSelected += (_, _) => RefreshViewModel();

        this.InstanceRename += HandleInstanceRename;
        this.StateRename += HandleStateRename;

        this.StateAdd += HandleStateAdd;
        this.StateDelete += HandleStateDelete;

        this.VariableSet += HandleVariableSet;

        this.CategoryRename += HandleCategoryRename;

        // Deleting a whole category (and its states) doesn't fire the granular StateDelete event, so
        // recompute the view model afterward — otherwise a keyframe that referenced a state in the
        // deleted category keeps its non-error icon until the element is reselected (issue #3392).
        this.CategoryDelete += HandleCategoryDelete;

        this.ElementRename += HandleElementRename;
        this.ElementDuplicate += HandleElementDuplicate;

        // Live-reload the tab when the selected element's .ganx is edited on disk (issue #3410).
        // FileChangeReactionLogic owns no animation data (the .ganx is a per-element sidecar loaded
        // on demand here, not part of GumProjectSave), so the reload lives in this plugin rather than
        // in the core dispatch.
        this.ReactToFileChanged += HandleFileChanged;

        this.GetDeleteStateResponse = HandleGetDeleteStateResponse;
        this.GetDeleteStateCategoryResponse = HandleGetDeleteStateCategoryResponse;

        this.DeleteOptionsWindowShow += _elementDeleteService.HandleDeleteOptionsWindowShow;
        this.DeleteConfirmed += _elementDeleteService.HandleConfirmDelete;

        // Undo/redo restore element state without firing the granular StateAdd/StateDelete events, so
        // recompute the view model (and its keyframe error state) afterward — otherwise a broken
        // keyframe's error icon stays stale until the element is reselected (issue #3386).
        this.AfterUndo += HandleAfterUndo;

        // Animation "keyframe references a missing state" errors are now detected per-element by
        // the headless AnimationKeyframeErrorSource (issue #3293), which reads the .ganx so it
        // works on project open and on edits regardless of selection. Contributing them here too
        // (from this plugin's per-selection view model) would duplicate those errors for the
        // selected element, so this plugin no longer subscribes to GetAllErrors.
    }

    private void HandleFileChanged(FilePath filePath)
    {
        var selectedElement = _selectedState.SelectedElement;
        var selectedElementAnimationFile = selectedElement != null
            ? _animationFilePathService.GetAbsoluteAnimationFileNameFor(selectedElement)
            : null;

        if (ShouldReloadAnimationsForChangedFile(filePath, selectedElementAnimationFile))
        {
            // Capture the selection before the rebuild replaces every animation/keyframe instance, then
            // reapply it so the external edit doesn't deselect the user's animation + keyframe (#3410).
            var selection = CaptureAnimationSelection(_viewModel);

            // forceReload bypasses CreateViewModel's same-element early-out: the external edit doesn't
            // change the selection, so without forcing it the stale view model would survive and the
            // tab would keep showing the pre-edit animations until the element was reselected. Mirrors
            // the after-undo repaint (#3406).
            RefreshViewModel(forceReload: true);

            if (_viewModel != null)
            {
                RestoreAnimationSelection(_viewModel, selection);
            }
        }
    }

    /// <summary>
    /// Decides whether an on-disk file change should live-reload the Animations tab (issue #3410):
    /// true only when <paramref name="changedFile"/> is a <c>.ganx</c> and is the selected element's
    /// own animation sidecar (<paramref name="selectedElementAnimationFile"/>). Other elements' .ganx
    /// files reload lazily when that element is next selected, so they are ignored here.
    /// </summary>
    internal static bool ShouldReloadAnimationsForChangedFile(FilePath changedFile,
        FilePath? selectedElementAnimationFile)
    {
        if (changedFile.Extension != "ganx")
        {
            return false;
        }

        return selectedElementAnimationFile != null && changedFile == selectedElementAnimationFile;
    }

    private void HandleElementSelected(ElementSave? element)
    {
        RefreshViewModel();

        if (element != null && !pluginTab!.IsVisible)
        {
            var fileName = _animationFilePathService.GetAbsoluteAnimationFileNameFor(element);

            if (fileName?.Exists() == true)
            {
                pluginTab.Show();
            }
        }
    }

    public override bool ShutDown(PluginShutDownReason shutDownReason)
    {
        return true;
    }

    private void HandleVariableSet(ElementSave element, InstanceSave? save2, string arg3, object? arg4)
    {
        // This maybe a little inefficient but it should address all issues:
        // eventually this could be more targeted
        var state = _selectedState.SelectedStateSave;
        if (_viewModel == null || state == null) return;

        var isDefault =
            state == _selectedState.SelectedElement?.DefaultState;

        var stateName = state.Name;
        if (_selectedState.SelectedStateCategorySave != null)
        {
            stateName = _selectedState.SelectedStateCategorySave.Name + "/" + stateName;
        }

        foreach (var animation in _viewModel.Animations)
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


    #endregion

    private void HandleElementDuplicate(ElementSave oldElement, ElementSave newElement)
    {
        _duplicateService.HandleDuplicate(oldElement, newElement);
    }

    private void HandleElementRename(ElementSave element, string oldName)
    {
        if (_viewModel == null)
        {
            CreateViewModel();
        }

        _renameManager.HandleRename(element, oldName, _viewModel!);
    }

    private void HandleInstanceRename(ElementSave element, InstanceSave instanceSave, string oldName)
    {
        if (_viewModel == null)
        {
            CreateViewModel();
        }

        if (_selectedState.SelectedElement != null)
        {
            _renameManager.HandleRename(instanceSave, oldName, _viewModel!);
        }
    }

    private void HandleStateRename(StateSave stateSave, string oldName)
    {
        if (_viewModel == null)
        {
            CreateViewModel();
        }

        var element = _selectedState.SelectedElement;
        if (element != null && _viewModel != null)
        {
            RefreshAfterStateRename(_renameManager, _viewModel, element, stateSave, oldName);
        }

        // Refresh available states / DataContext and re-broadcast. RefreshErrors runs again here,
        // but it is idempotent and rename is not a hot path.
        RefreshViewModel();
    }

    /// <summary>
    /// Applies a state rename to <paramref name="viewModel"/>'s keyframe references and then
    /// recomputes errors, in that order. The order matters: recomputing before the rewrite leaves a
    /// stale "references a missing state" error on the renamed keyframe (issue #3383).
    /// </summary>
    internal static void RefreshAfterStateRename(IRenameManager renameManager,
        ElementAnimationsViewModel viewModel, ElementSave element, StateSave stateSave, string oldName)
    {
        renameManager.HandleRename(stateSave, oldName, viewModel);
        viewModel.RefreshErrors(element);
    }

    private void HandleAfterUndo()
    {
        // Capture the selection before the forced rebuild below replaces the view model (and with it
        // every AnimationViewModel/keyframe instance), then reselect by identity afterward so the
        // user's animation + keyframe selection survives undo/redo — mirroring element-undo's state
        // selection restore (#3406). Index + count are captured alongside the keyframe so the reselect
        // can fall back to position when the undo reverted the selected keyframe's own value (see
        // RestoreAnimationSelection).
        var selection = CaptureAnimationSelection(_viewModel);

        // Repaint the tab from the just-restored .ganx, then re-arm undo recording. The flag spanned
        // the .ganx write (ApplyAnimations) through this repaint so neither flushed a spurious undo.
        // forceReload bypasses CreateViewModel's same-element early-out: an in-place undo leaves the
        // selected element unchanged, so without forcing it the stale view model would survive and the
        // tab would keep showing the pre-undo animations until the element was reselected (#3406).
        RefreshViewModel(forceReload: true);

        if (_viewModel != null)
        {
            RestoreAnimationSelection(_viewModel, selection);
        }

        _isApplyingUndo = false;
    }

    /// <summary>
    /// The animation/keyframe selection captured before a forced view-model rebuild (undo/redo or an
    /// external .ganx reload). Carries both the identity (animation name + keyframe content) and the
    /// position (animation/keyframe index + sibling counts) so <see cref="RestoreAnimationSelection"/>
    /// can fall back to the slot when the identity changed — e.g. the selected animation was renamed.
    /// </summary>
    internal readonly record struct AnimationSelectionState(
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
    internal static AnimationSelectionState CaptureAnimationSelection(ElementAnimationsViewModel? viewModel)
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
    internal static AnimatedKeyframeViewModel? RestoreAnimationSelection(ElementAnimationsViewModel viewModel,
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

    private void HandleStateAdd(StateSave state)
    {
        RefreshViewModel();
    }

    private void HandleStateDelete(StateSave state)
    {
        RefreshViewModel();
    }

    private void HandleCategoryDelete(StateSaveCategory category)
    {
        RefreshViewModel();
    }

    private void HandleCategoryRename(StateSaveCategory category, string oldName)
    {
        if (_viewModel == null)
        {
            CreateViewModel();
        }

        // We only care about this if we have an element. Otherwise, it could be a behavior:
        if (_selectedState.SelectedElement != null)
        {
            _renameManager.HandleRename(category, oldName, _viewModel!);
        }

    }

    private void HandleToggleTabVisibility(object? sender, System.Windows.RoutedEventArgs e)
    {
        if(pluginTab != null)
        {
            pluginTab.IsVisible = !pluginTab.IsVisible;

            if(pluginTab.IsVisible)
            {
                pluginTab.IsSelected = true;
            }
        }
    }

    private void CreateAnimationWindow()
    {
        if (_mainWindow == null)
        {
            _settingsManager.LoadOrCreateSettings();

            _mainWindow = new StateAnimationPlugin.Views.MainWindow();

            var settings = _settingsManager.GlobalSettings;

            _mainWindow.FirstRowWidth = new GridLength((double)settings.FirstToSecondColumnRatio, GridUnitType.Star);
            _mainWindow.SecondRowWidth = new GridLength(1, GridUnitType.Star);
            _mainWindow.AddStateKeyframeClicked += HandleAddStateKeyframe;
            _mainWindow.AnimationKeyframeAdded += HandleAnimationKeyrameAdded;
            _mainWindow.AnimationColumnsResized += HandleAnimationColumnsResized;
            
            pluginTab = _tabManager.AddControl(_mainWindow, "Animations",
                TabLocation.RightBottom);

            pluginTab.TabShown += HandleTabShown;
            pluginTab.TabHidden += HandleTabHidden;
            pluginTab.CanClose = true;
            pluginTab.Hide();
        }

        // forces a refresh:
        _viewModel = _animationVmFactory();

        RefreshViewModel();
    }

    private void HandleAnimationColumnsResized()
    {
        if (_mainWindow?.SecondRowWidth.Value > 0)
        {
            var ratio = _mainWindow.FirstRowWidth.Value / _mainWindow.SecondRowWidth.Value;

            _settingsManager.GlobalSettings.FirstToSecondColumnRatio = (decimal)ratio;

            _settingsManager.SaveSettings();
        }
    }

    private void HandleAddStateKeyframe(object? sender, EventArgs e)
    {
        string? whyIsntValid = GetWhyAddingTimedStateIsInvalid();

        var selectedAnimation = _viewModel?.SelectedAnimation;
        if(selectedAnimation == null)
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

    private void HandleAnimationKeyrameAdded(AnimatedKeyframeViewModel newVm)
    {
        newVm.AvailableStates = this.AvailableStates;
        newVm.PropertyChanged += HandleAnimatedKeyframePropertyChanged;
    }

    private string? GetWhyAddingTimedStateIsInvalid()
    {
        string? whyIsntValid = null;

        if(_viewModel == null)
        {
            // invalid state, but don't blow up
            return null;
        }
        if (_viewModel.SelectedAnimation == null)
        {
            whyIsntValid = "You must first select an Animation";
        }

        if (_selectedState.SelectedScreen == null && _selectedState.SelectedComponent == null)
        {
            whyIsntValid = "You must first select a Screen or Component";
        }
        return whyIsntValid;
    }

    private void HandleTabShown()
    {
        if (menuItem != null)
        {
            menuItem.Header = "Hide Animations";
        }
    }

    private void HandleTabHidden()
    {
        if(menuItem != null)
        {
            menuItem.Header = "View Animations";
        }
    }

    private void RefreshViewModel(bool forceReload = false)
    {
        RefreshAvailableStates();

        CreateViewModel(forceReload);

        if (_mainWindow != null)
        {
            _mainWindow.DataContext = _viewModel;
        }
    }

    private void RefreshAvailableStates()
    {
        AvailableStates.ReplaceWith(GetAvailableStates(_selectedState.SelectedElement, _viewModel));
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
    internal static List<string> GetAvailableStates(ElementSave? element, ElementAnimationsViewModel? viewModel)
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
    /// Decides whether <see cref="CreateViewModel"/> should rebuild the view model from the element's
    /// .ganx. Normally the view model is only reloaded when the selected element changes; an in-place
    /// undo/redo restores the .ganx without changing the selection, so the after-undo path passes
    /// <paramref name="forceReload"/> to repaint the tab immediately rather than keeping the stale view
    /// model until the element is reselected (#3406).
    /// </summary>
    internal static bool ShouldReloadViewModel(ElementSave? currentlyReferencedElement,
        ElementSave? selectedElement, bool forceReload)
    {
        return currentlyReferencedElement != selectedElement || forceReload;
    }

    private void CreateViewModel(bool forceReload = false)
    {
        ElementSave? currentlyReferencedElement = null;
        if (_viewModel != null)
        {
            currentlyReferencedElement = _viewModel.Element;
        }

        var element = _selectedState.SelectedElement;

        if (ShouldReloadViewModel(currentlyReferencedElement, element, forceReload))
        {
            if (_projectState.GumProjectSave?.FullFileName == null)
            {
                // OK to assign null, will be fixed down below
                _viewModel = null!;
            }
            else
            {
                // OK to assign null, will be fixed down below
                _viewModel = _animationCollectionViewModelManager.GetAnimationCollectionViewModel(element)!;
            }

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += HandlePropertyChanged;
                _viewModel.AnyChange += HandleDataChange;
                _viewModel.AddStateKeyframeRequested += HandleAddStateKeyframe;

                foreach (var item in _viewModel.Animations)
                {
                    foreach (var keyframe in item.Keyframes)
                    {
                        keyframe.AvailableStates = this.AvailableStates;
                        keyframe.PropertyChanged += HandleAnimatedKeyframePropertyChanged;

                    }
                }
            }
            currentlyReferencedElement = element;
        }
        
        if (_viewModel == null)
        {
            _viewModel = _animationVmFactory();
        }
        if(currentlyReferencedElement != null)
        {
            _viewModel?.RefreshErrors(currentlyReferencedElement);
        }

        _messenger.Send(new RequestErrorRefreshMessage { RequestingPlugin = this });

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

    private void HandlePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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
        if (_viewModel?.SelectedAnimation == null)
        {
            return;
        }
        ////////////////////// END EARLY OUT

        var animationTime = _viewModel.DisplayedAnimationTime;

        var animation = _viewModel.SelectedAnimation;
        var element = _selectedState.SelectedElement;

        if(element != null)
        {
            animation.SetStateAtTime(animationTime, element, defaultIfNull: true);
        }
    }

    private void HandleDataChange(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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
                _animationCollectionViewModelManager.Save(_viewModel!);
            }
            catch (Exception exc)
            {
                _guiCommands.PrintOutput($"Could not save animations for {_viewModel?.Element}:\n{exc}");
            }

            // Fold this animation edit into the selected element's undo timeline. Opening and disposing
            // an undo lock fires the element strategy's TryRecord, whose baseline (captured on selection
            // / after the previous record) now differs from the just-saved animations, so it records the
            // change atomically with any element edit and re-baselines. One mechanism covers every
            // persisted gesture — keyframe add/delete/paste, time/interpolation/loop edits. Skipped while
            // an undo is being applied, so restoring animations doesn't record a brand-new undo (#3406).
            if (!_isApplyingUndo)
            {
                using (_undoManager.RequestLock()) { }
            }

            // The edit changed which states/animations the keyframes reference, so an animation
            // error (e.g. a keyframe pointing at a now-missing state) may have appeared or cleared.
            // No structural plugin event fires for an animation edit, so request a full error
            // refresh; the headless checker re-reads the just-saved .ganx. RequestingPlugin is left
            // null (full refresh) so both the Errors tab and the tree "!" indicator re-check.
            _messenger.Send(new RequestErrorRefreshMessage());
        }
    }


    private DeleteResponse HandleGetDeleteStateResponse(StateSave state, IStateContainer container)
    {
        var response = new DeleteResponse();
        response.ShouldDelete = true;

        if(container is ElementSave elementSave)
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

    private DeleteResponse HandleGetDeleteStateCategoryResponse(StateSaveCategory category, IStateContainer container)
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
            global::Gum.StateAnimation.SaveClasses.ElementAnimationsSave? model;
            if (element == _viewModel?.Element)
            {
                model = _viewModel.BackingData!;
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
