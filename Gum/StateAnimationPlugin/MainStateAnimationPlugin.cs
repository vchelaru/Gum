using CommunityToolkit.Mvvm.Messaging;
using Gum;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Extensions;
using Gum.Gui.Windows;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Messages;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Responses;
using Gum.Services;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using ToolsUtilities;


namespace StateAnimationPlugin;

/// <summary>
/// WPF-hosted plugin entry point for the Animations tab. All of the WPF-free business logic (view
/// model construction/refresh, undo/redo repaint, rename/delete/variable-set reactions, keyframe
/// dialogs) lives in <see cref="AnimationTabController"/> (issue #3866) - this class owns only the
/// real platform glue: the WPF window/menu/tab-visibility wiring, and pushing the controller's view
/// model onto the window's DataContext.
/// </summary>
[Export(typeof(PluginBase))]
public class MainStateAnimationPlugin : WpfPluginBase, IAnimationUndoProvider
{
    private readonly ISelectedState _selectedState;
    private readonly INameVerifier _nameVerifier;
    private readonly IMessenger _messenger;
    private readonly IOutputManager _outputManager;
    private readonly IFileWatchManager _fileWatchManager;
    private readonly IUndoManager _undoManager;
    private readonly IAnimationUndoProviderRegistrar _animationUndoProviderRegistrar;
    private readonly Func<ElementAnimationsViewModel> _animationVmFactory;

    #region Fields
    private readonly IDuplicateService _duplicateService;
    private readonly AnimationFilePathService _animationFilePathService;
    private readonly ElementDeleteService _elementDeleteService;
    private readonly IRenameManager _renameManager;
    private readonly ISettingsManager _settingsManager;
    private readonly IProjectState _projectState;
    private readonly IProjectManager _projectManager;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IAnimationCollectionViewModelManager _animationCollectionViewModelManager;

    // Constructed in StartUp, not the ctor: it needs _dialogService/_guiCommands, which are
    // PluginBase [Import] properties satisfied by MEF *after* this constructor returns.
    private AnimationTabController _controller = null!;

    StateAnimationPlugin.Views.MainWindow? _mainWindow;
    private IPluginTab? pluginTab;
    private MenuItem? menuItem;

    // Owned here (not by ElementDeleteService) because materializing a WPF CheckBox from the
    // framework-neutral DeleteOptionCheckboxViewModel is a view concern (ADR-0005). Set by
    // HandleDeleteOptionsWindowShow, read back by HandleDeleteConfirmed, then cleared.
    private CheckBox? _deleteAnimationFileCheckBox;

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
        _controller.FillTopLevelNames(element, names);
    }

    #endregion

    #region StartUp/ShutDown

    [ImportingConstructor]
    public MainStateAnimationPlugin(
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

        _animationFilePathService = new AnimationFilePathService(_selectedState, fileCommands);
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
        // _dialogService/_guiCommands (PluginBase [Import] properties) are only guaranteed set once
        // MEF finishes composing this part, which happens before StartUp runs - so the controller is
        // built here, not in the constructor above.
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

        CreateMenuItems();
        CreateAnimationWindow();
        AssignEvents();
    }

    /// <summary>
    /// Pushes the controller's (possibly-same-instance) view model onto the WPF window's DataContext
    /// and requests a scoped error recheck - both real platform/host concerns the headless controller
    /// has no seam for (the message type carries a <see cref="PluginBase"/> identity).
    /// </summary>
    private void HandleControllerViewModelRefreshed()
    {
        _messenger.Send(new RequestErrorRefreshMessage { RequestingPlugin = this });

        if (_mainWindow != null)
        {
            _mainWindow.DataContext = _controller.ViewModel;
        }
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

    private void CreateMenuItems()
    {
        menuItem = AddMenuItem("View", "View Animations");

        menuItem.Click += HandleToggleTabVisibility;
    }

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
        // FileChangeReactionLogic owns no animation data (the .ganx is a per-element sidecar loaded
        // on demand here, not part of GumProjectSave), so the reload lives in the controller rather
        // than in the core dispatch.
        this.ReactToFileChanged += _controller.HandleFileChanged;

        this.GetDeleteStateResponse = _controller.HandleGetDeleteStateResponse;
        this.GetDeleteStateCategoryResponse = _controller.HandleGetDeleteStateCategoryResponse;

        this.DeleteOptionsWindowShow += HandleDeleteOptionsWindowShow;
        this.DeleteConfirmed += HandleDeleteConfirmed;

        // Undo/redo restore element state without firing the granular StateAdd/StateDelete events, so
        // recompute the view model (and its keyframe error state) afterward - otherwise a broken
        // keyframe's error icon stays stale until the element is reselected (issue #3386).
        this.AfterUndo += _controller.HandleAfterUndo;

        // Animation "keyframe references a missing state" errors are now detected per-element by
        // the headless AnimationKeyframeErrorSource (issue #3293), which reads the .ganx so it
        // works on project open and on edits regardless of selection. Contributing them here too
        // (from this plugin's per-selection view model) would duplicate those errors for the
        // selected element, so this plugin no longer subscribes to GetAllErrors.
    }

    /// <summary>
    /// Materializes <see cref="ElementDeleteService.HandleDeleteOptionsWindowShow"/>'s
    /// framework-neutral checkbox request into a real WPF control and adds it to the
    /// DeleteOptionsWindow, if one is requested.
    /// </summary>
    private void HandleDeleteOptionsWindowShow(DeleteOptionsWindow deleteWindow, Array objectsToDelete)
    {
        var checkboxViewModel = _elementDeleteService.HandleDeleteOptionsWindowShow(objectsToDelete);

        if (checkboxViewModel != null)
        {
            _deleteAnimationFileCheckBox = checkboxViewModel.ToCheckBox();
            deleteWindow.MainStackPanel.Children.Add(_deleteAnimationFileCheckBox);
        }
    }

    /// <summary>
    /// Reads back the checkbox added by <see cref="HandleDeleteOptionsWindowShow"/> (if any) and
    /// hands its final checked state to <see cref="ElementDeleteService.HandleConfirmDelete"/>,
    /// then removes the checkbox from the window.
    /// </summary>
    private void HandleDeleteConfirmed(DeleteOptionsWindow deleteOptionsWindow, Array deletedObjects)
    {
        bool isChecked = _deleteAnimationFileCheckBox?.IsChecked == true;

        _elementDeleteService.HandleConfirmDelete(deletedObjects, isChecked);

        if (_deleteAnimationFileCheckBox != null)
        {
            if (deleteOptionsWindow.MainStackPanel.Children.Contains(_deleteAnimationFileCheckBox))
            {
                deleteOptionsWindow.MainStackPanel.Children.Remove(_deleteAnimationFileCheckBox);
            }
            _deleteAnimationFileCheckBox = null;
        }
    }

    /// <summary>
    /// Refreshes the tab and, if it's hidden, auto-shows it the first time the newly-selected element
    /// turns out to have an animation file. <see cref="pluginTab"/> is exposed only through the
    /// headless <see cref="IPluginTab"/> seam, but the plugin still owns the animation-window
    /// construction (a real WPF <c>Window</c>), so this stays plugin-side glue even though the
    /// refresh decision itself is now the controller's.
    /// </summary>
    private void HandleElementSelected(ElementSave? element)
    {
        _controller.RefreshViewModel();

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

    #endregion

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
            _mainWindow.AddStateKeyframeClicked += _controller.HandleAddStateKeyframe;
            _mainWindow.AnimationKeyframeAdded += _controller.HandleAnimationKeyrameAdded;
            _mainWindow.AnimationColumnsResized += HandleAnimationColumnsResized;

            pluginTab = _tabManager.AddControl(_mainWindow, "Animations",
                TabLocation.RightBottom);

            pluginTab.TabShown += HandleTabShown;
            pluginTab.TabHidden += HandleTabHidden;
            pluginTab.CanClose = true;
            pluginTab.Hide();
        }

        // forces a refresh:
        _controller.CreateInitialViewModel();

        _controller.RefreshViewModel();
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
}
