using CommunityToolkit.Mvvm.Messaging;
using Gum;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Menus;
using Gum.Messages;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Responses;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.Models;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;

namespace StateAnimationPlugin;

/// <summary>
/// The Animations tab, complete except for what a UI framework must supply: the tab's view
/// (<see cref="CreateAnimationTabContent"/>), pushing the current view model into it
/// (<see cref="ShowViewModel"/>), and a UI-thread timer for playback (<see cref="CreateUiTimer"/>).
/// The business logic lives in <see cref="AnimationTabController"/> (issue #3866); this class wires it
/// to plugin events, the menu, the tab, undo, and the delete confirmation. Each head derives an
/// exported plugin from it, like <c>EditorTabPluginBase</c>.
/// </summary>
public abstract class StateAnimationPluginBase : PluginBase, IAnimationUndoProvider
{
    #region Fields

    private readonly ISelectedState _selectedState;
    private readonly INameVerifier _nameVerifier;
    private readonly IMessenger _messenger;
    private readonly IOutputManager _outputManager;
    private readonly IFileWatchManager _fileWatchManager;
    private readonly IUndoManager _undoManager;
    private readonly IAnimationUndoProviderRegistrar _animationUndoProviderRegistrar;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly Func<ElementAnimationsViewModel> _animationVmFactory;
    private readonly AnimationFilePathService _animationFilePathService;
    private readonly IRenameManager _renameManager;
    private readonly ISettingsManager _settingsManager;
    private readonly IProjectState _projectState;
    private readonly IProjectManager _projectManager;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IAnimationCollectionViewModelManager _animationCollectionViewModelManager;

    // Built in StartUp, not the ctor: they need _dialogService/_guiCommands, which are PluginBase
    // [Import] properties that MEF sets after this constructor returns.
    private AnimationTabController _controller = null!;
    private IDuplicateService _duplicateService = null!;
    private ElementDeleteService _elementDeleteService = null!;

    private IPluginTab? _pluginTab;
    private MenuItemModel? _menuItem;

    // The "delete the animation file" option this plugin added to the current delete confirmation,
    // if any. Set by HandleDeleteOptionsShow, read back by HandleDeleteConfirmed, then cleared.
    private DeleteOptionCheckboxViewModel? _deleteAnimationFileOption;

    #endregion

    #region Properties

    /// <inheritdoc/>
    public override string FriendlyName => "State Animation Plugin";

    // 0.0.0.2: Renaming Gum file now renames its animations
    /// <inheritdoc/>
    public override Version Version => new Version(0, 0, 0, 2);

    /// <summary>
    /// The tab's list hotkeys (reorder, delete, copy, paste), shared by both heads' views. Available
    /// from <see cref="CreateAnimationTabContent"/> on.
    /// </summary>
    protected AnimationTabKeyHandler KeyHandler { get; private set; } = null!;

    #endregion

    /// <summary>Creates the plugin; the head's exported subclass passes its MEF imports through.</summary>
    protected StateAnimationPluginBase(
        ISelectedState selectedState,
        INameVerifier nameVerifier,
        IMessenger messenger,
        IOutputManager outputManager,
        IFileWatchManager fileWatchManager,
        IFileCommands fileCommands,
        IProjectState projectState,
        IProjectManager projectManager,
        IWireframeObjectManager wireframeObjectManager,
        IUndoManager undoManager,
        IAnimationUndoProviderRegistrar animationUndoProviderRegistrar,
        IHotkeyManager hotkeyManager)
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
        _hotkeyManager = hotkeyManager;

        _animationFilePathService = new AnimationFilePathService(_selectedState, fileCommands, _projectManager);
        _settingsManager = new SettingsManager();

        // The factory closure reads _animationCollectionViewModelManager and _renameManager lazily
        // (when invoked, after both are assigned just below, hence the !), which breaks the
        // ACVMM -> ElementAnimationsViewModel -> RenameManager construction cycle without a Lazy<T>.
        // Each call gets a fresh timer: ElementAnimationsViewModel is recreated per selected-element
        // switch (see AnimationCollectionViewModelManager.GetAnimationCollectionViewModel), and a
        // shared timer would let two live view models fight over the same Tick subscription.
        _animationVmFactory = () => new ElementAnimationsViewModel(
            _nameVerifier, _dialogService, _animationCollectionViewModelManager!, _renameManager!,
            _selectedState, _wireframeObjectManager, _outputManager, _animationFilePathService,
            CreateUiTimer());
        _animationCollectionViewModelManager = new AnimationCollectionViewModelManager(
            _selectedState, _outputManager, _fileWatchManager, _animationFilePathService, _animationVmFactory);
        _renameManager = new RenameManager(
            _selectedState, _outputManager, _animationFilePathService, _animationCollectionViewModelManager, _projectManager);
    }

    #region Head hooks

    /// <summary>
    /// Builds the tab's view. It shows the view model <see cref="ShowViewModel"/> hands it, and calls
    /// <see cref="AddStateKeyframe"/>, <see cref="AddPastedKeyframe"/> and <see cref="SaveColumnRatio"/>.
    /// Called once, from <see cref="StartUp"/>.
    /// </summary>
    protected abstract object CreateAnimationTabContent(AnimationPluginSettings settings);

    /// <summary>Shows <paramref name="viewModel"/> (the selected element's animations, or null) in the tab's view.</summary>
    protected abstract void ShowViewModel(ElementAnimationsViewModel? viewModel);

    /// <summary>A UI-thread timer for playback; each call returns a new one.</summary>
    protected abstract IUiTimer CreateUiTimer();

    #endregion

    #region Called by the head's view

    /// <summary>Starts adding a state keyframe to the selected animation (the tab's Add &gt; State).</summary>
    protected void AddStateKeyframe() => _controller.HandleAddStateKeyframe(this, EventArgs.Empty);

    /// <summary>Records a keyframe the user pasted into the keyframe list.</summary>
    protected void AddPastedKeyframe(AnimatedKeyframeViewModel keyframe) => _controller.HandleAnimationKeyrameAdded(keyframe);

    /// <summary>Remembers the ratio of the animation column to the keyframe column across sessions.</summary>
    protected void SaveColumnRatio(double animationColumnWidth, double keyframeColumnWidth)
    {
        if (keyframeColumnWidth > 0)
        {
            _settingsManager.GlobalSettings.FirstToSecondColumnRatio = (decimal)(animationColumnWidth / keyframeColumnWidth);
            _settingsManager.SaveSettings();
        }
    }

    #endregion

    /// <inheritdoc/>
    public override void FillTopLevelNames(ElementSave element, List<TopLevelName> names)
    {
        _controller.FillTopLevelNames(element, names);
    }

    /// <inheritdoc/>
    public override void StartUp()
    {
        _duplicateService = new DuplicateService(_dialogService, _projectManager);
        _elementDeleteService = new ElementDeleteService(_animationFilePathService, _dialogService, _fileCommands);
        KeyHandler = new AnimationTabKeyHandler(_hotkeyManager, _dialogService);

        _controller = new AnimationTabController(
            _selectedState,
            _undoManager,
            _guiCommands,
            _dialogService,
            _projectState,
            _animationCollectionViewModelManager,
            _renameManager,
            _duplicateService,
            _animationFilePathService,
            _animationVmFactory);

        _controller.ViewModelRefreshed += HandleControllerViewModelRefreshed;
        _controller.DataSaved += HandleControllerDataSaved;

        // Register as the live animation provider so the element undo strategy can fold this
        // element's animations into its snapshot (#3406). UndoManager was constructed at DI time,
        // before any plugin existed, so it holds a relay that this call now points at us.
        _animationUndoProviderRegistrar.Register(this);

        _menuItem = AddMenuEntry(HandleToggleTabVisibility, "View", "View Animations");
        CreateAnimationTab();
        AssignEvents();
    }

    /// <inheritdoc/>
    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    /// <summary>
    /// Pushes the controller's (possibly-same-instance) view model into the head's view and requests
    /// a scoped error recheck (the message carries this plugin's identity).
    /// </summary>
    private void HandleControllerViewModelRefreshed()
    {
        _messenger.Send(new RequestErrorRefreshMessage { RequestingPlugin = this });
        ShowViewModel(_controller.ViewModel);
    }

    /// <summary>
    /// Requests a full (unscoped) error recheck after the controller persists an edit - the headless
    /// checker re-reads the just-saved .ganx. RequestingPlugin is left null (full refresh) so both the
    /// Errors tab and the tree "!" indicator re-check.
    /// </summary>
    private void HandleControllerDataSaved()
    {
        _messenger.Send(new RequestErrorRefreshMessage());
    }

    #region IAnimationUndoProvider

    ElementAnimationsSave? IAnimationUndoProvider.GetCurrentAnimations(ElementSave element) =>
        _controller.GetCurrentAnimations(element);

    void IAnimationUndoProvider.ApplyAnimations(ElementSave element, ElementAnimationsSave animations) =>
        _controller.ApplyAnimations(element, animations);

    #endregion

    private void AssignEvents()
    {
        this.ElementSelected += HandleElementSelected;

        this.InstanceSelected += (_, _) => _controller.RefreshViewModel();

        this.InstanceRename += _controller.HandleInstanceRename;
        this.StateRename += _controller.HandleStateRename;

        this.StateAdd += _controller.HandleStateAdd;
        this.StateDelete += _controller.HandleStateDelete;

        this.VariableSet += _controller.HandleVariableSet;

        this.CategoryRename += _controller.HandleCategoryRename;

        // Deleting a whole category (and its states) doesn't fire the granular StateDelete event, so
        // recompute the view model afterward - otherwise a keyframe that referenced a state in the
        // deleted category keeps its non-error icon until the element is reselected (issue #3392).
        this.CategoryDelete += _controller.HandleCategoryDelete;

        this.ElementRename += _controller.HandleElementRename;
        this.ElementDuplicate += _controller.HandleElementDuplicate;

        // Live-reload the tab when the selected element's .ganx is edited on disk (issue #3410).
        this.ReactToFileChanged += _controller.HandleFileChanged;

        this.GetDeleteStateResponse = _controller.HandleGetDeleteStateResponse;
        this.GetDeleteStateCategoryResponse = _controller.HandleGetDeleteStateCategoryResponse;

        this.DeleteOptionsShow += HandleDeleteOptionsShow;
        this.DeleteOptionsConfirmed += HandleDeleteConfirmed;

        // Undo/redo restore element state without firing the granular StateAdd/StateDelete events, so
        // recompute the view model (and its keyframe error state) afterward - otherwise a broken
        // keyframe's error icon stays stale until the element is reselected (issue #3386).
        this.AfterUndo += _controller.HandleAfterUndo;

        // Animation "keyframe references a missing state" errors are detected per-element by the
        // headless AnimationKeyframeErrorSource (issue #3293), so this plugin does not subscribe to
        // GetAllErrors (that would duplicate them for the selected element).
    }

    /// <summary>
    /// Adds <see cref="ElementDeleteService.HandleDeleteOptionsWindowShow"/>'s "delete the animation
    /// file" option to the delete confirmation, if one is requested. Each head renders it.
    /// </summary>
    private void HandleDeleteOptionsShow(DeleteOptionsDialogViewModel dialog, Array objectsToDelete)
    {
        // A cancelled delete never fires DeleteOptionsConfirmed, so clear the previous dialog's option
        // here - otherwise a later delete that adds no option would read the stale checked state.
        _deleteAnimationFileOption = _elementDeleteService.HandleDeleteOptionsWindowShow(objectsToDelete);

        if (_deleteAnimationFileOption != null)
        {
            dialog.CheckBoxes.Add(_deleteAnimationFileOption);
        }
    }

    /// <summary>
    /// Hands the final state of the option added by <see cref="HandleDeleteOptionsShow"/> (if any) to
    /// <see cref="ElementDeleteService.HandleConfirmDelete"/>.
    /// </summary>
    private void HandleDeleteConfirmed(DeleteOptionsDialogViewModel dialog, Array deletedObjects)
    {
        bool isChecked = _deleteAnimationFileOption?.IsChecked == true;
        _deleteAnimationFileOption = null;

        _elementDeleteService.HandleConfirmDelete(deletedObjects, isChecked);
    }

    /// <summary>
    /// Refreshes the tab and, if it's hidden, auto-shows it the first time the newly-selected element
    /// turns out to have an animation file.
    /// </summary>
    private void HandleElementSelected(ElementSave? element)
    {
        _controller.RefreshViewModel();

        if (element != null && _pluginTab is { IsVisible: false })
        {
            var fileName = _animationFilePathService.GetAbsoluteAnimationFileNameFor(element);

            if (fileName?.Exists() == true)
            {
                _pluginTab.Show();
            }
        }
    }

    private void HandleToggleTabVisibility()
    {
        if (_pluginTab != null)
        {
            _pluginTab.IsVisible = !_pluginTab.IsVisible;

            if (_pluginTab.IsVisible)
            {
                _pluginTab.IsSelected = true;
            }
        }
    }

    private void CreateAnimationTab()
    {
        if (_pluginTab == null)
        {
            _settingsManager.LoadOrCreateSettings();

            object content = CreateAnimationTabContent(_settingsManager.GlobalSettings);
            _pluginTab = _tabManager.AddControl(content, "Animations", TabLocation.RightBottom);

            _pluginTab.TabShown += HandleTabShown;
            _pluginTab.TabHidden += HandleTabHidden;
            _pluginTab.CanClose = true;
            _pluginTab.Hide();
        }

        // forces a refresh:
        _controller.CreateInitialViewModel();

        _controller.RefreshViewModel();
    }

    private void HandleTabShown()
    {
        if (_menuItem != null)
        {
            _menuItem.Header = "Hide Animations";
        }
    }

    private void HandleTabHidden()
    {
        if (_menuItem != null)
        {
            _menuItem.Header = "View Animations";
        }
    }
}
