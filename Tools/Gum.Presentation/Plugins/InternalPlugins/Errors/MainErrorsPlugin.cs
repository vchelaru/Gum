using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Messages;
using Gum.Plugins.BaseClasses;
using Gum.Reflection;
using Gum.Services;
using Gum.ToolStates;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

namespace Gum.Plugins.Errors;

/// <summary>
/// The Errors tab, shared by both heads: keeps <see cref="AllErrorsViewModel"/> in step with the
/// selected element. Each head supplies the tab's view and its error-count header (TabViewRegistry).
/// </summary>
[Export(typeof(PluginBase))]
public class MainErrorsPlugin : CorePriorityPlugin
{
    #region Fields/Properties

    private readonly IErrorChecker _errorChecker;
    private readonly IMessenger _messenger;
    private readonly ISelectedState _selectedState;
    private readonly IClipboardService _clipboardService;
    private readonly IFileSystemRevealService _fileSystemRevealService;
    private readonly IProjectState _projectState;
    private readonly IDispatcher _dispatcher;
    private AllErrorsViewModel _viewModel = null!;

    // The project-level rows (#5262), listed whatever is selected. Finding them walks the whole
    // project, so they are cached here and refreshed only on load, save and file changes.
    private ErrorViewModel[] _projectErrors = [];
    private bool _isProjectErrorRefreshPending;

    #endregion

    [ImportingConstructor]
    public MainErrorsPlugin(IErrorChecker errorChecker, IMessenger messenger, ISelectedState selectedState,
        IClipboardService clipboardService, IFileSystemRevealService fileSystemRevealService, IProjectState projectState,
        IDispatcher dispatcher)
    {
        _errorChecker = errorChecker;
        _messenger = messenger;
        _selectedState = selectedState;
        _clipboardService = clipboardService;
        _fileSystemRevealService = fileSystemRevealService;
        _projectState = projectState;
        _dispatcher = dispatcher;
    }

    public override void StartUp()
    {
        _viewModel = new AllErrorsViewModel(_clipboardService, _fileSystemRevealService);

        _messenger.Register<RequestErrorRefreshMessage>(
            this,
            (_, message) => HandleErrorRefreshRequest(message));

        // Each head resolves the view model to its own view and count header (TabViewRegistry).
        _tabManager.AddControl(_viewModel, "Errors", TabLocation.RightBottom);

        AssignEvents();
    }

    private void HandleErrorRefreshRequest(RequestErrorRefreshMessage message)
    {
        if (message.RequestingPlugin != null)
        {
            // Plugin rows are project-wide, so they refresh whatever is selected (#5272).
            _viewModel.Errors.RemoveAll(item => item.OwnerPlugin == message.RequestingPlugin);
            _viewModel.Errors.AddRange(_errorChecker.GetPluginErrors(message.RequestingPlugin));
        }
        else
        {
            UpdateErrorsForElement(_selectedState.SelectedElement);
        }
    }

    private void AssignEvents()
    {
        this.ElementSelected += HandleElementSelected;
        this.ElementReloaded += HandleElementReloaded;
        this.ElementImported += HandleElementImported;
        this.InstanceSelected += HandleInstanceSelected;

        this.InstanceAdd += HandleInstanceAdd;
        this.InstanceDelete += HandleInstanceDelete;
        this.VariableSet += HandleVariableSet;
        this.VariableRemovedFromCategory += HandleVariableRemovedFromCategory;
        this.BehaviorReferencesChanged += HandleBehaviorReferencesChanged;

        this.ProjectLoad += HandleProjectLoad;
        this.AfterProjectSave += _ => ScheduleProjectErrorRefresh();
        this.ReactToFileChanged += _ => ScheduleProjectErrorRefresh();
    }

    private void HandleProjectLoad(GumProjectSave project)
    {
        // The previous project's rows must not show while the new project's pass is queued.
        _projectErrors = [];
        ScheduleProjectErrorRefresh();
    }

    /// <summary>
    /// Queues one project-level pass on the UI thread; a burst of notifications (a branch switch
    /// changes many files in one flush) runs it once. Element edits don't queue one: a pass walks
    /// the whole project on the UI thread (hundreds of milliseconds for an 80-element project), so a
    /// row an element edit adds or clears (a font's page files, an .achx's frames) updates on the
    /// next project save, file change or load. That row is rare enough not to justify the cost.
    /// </summary>
    private void ScheduleProjectErrorRefresh()
    {
        if (_isProjectErrorRefreshPending)
        {
            return;
        }
        _isProjectErrorRefreshPending = true;
        _dispatcher.Post(RefreshProjectErrors);
    }

    private void RefreshProjectErrors()
    {
        _isProjectErrorRefreshPending = false;
        _projectErrors = _projectState.GumProjectSave is { } project
            ? _errorChecker.GetProjectErrors(project)
            : [];
        UpdateErrorsForElement(_selectedState.SelectedElement);
    }

    private void HandleVariableRemovedFromCategory(string variableName, StateSaveCategory category)
    {
        // Removing a variable from a category's states can clear an error (e.g. a GUM0003
        // self-referential category state), so the Errors tab must refresh. The category
        // belongs to the currently selected element.
        UpdateErrorsForElement(_selectedState.SelectedElement);
    }

    private void HandleInstanceSelected(ElementSave? element, InstanceSave? instance)
    {
        UpdateErrorsForElement(element);
    }

    private void HandleBehaviorReferencesChanged(ElementSave element)
    {
        UpdateErrorsForElement(element);
    }

    private void HandleVariableSet(ElementSave? element, InstanceSave? instance, string variableName, object? oldValue,
        bool isFullCommit)
    {
        // Checking an element walks its file references and reads the disk, so it waits for a
        // committed value rather than running on every tick of a drag (issue #4946).
        if (!isFullCommit)
        {
            return;
        }

        UpdateErrorsForElement(element);
    }

    private void HandleInstanceDelete(ElementSave? element, InstanceSave instance)
    {
        UpdateErrorsForElement(element);
    }

    private void HandleInstanceAdd(ElementSave element, InstanceSave instance)
    {
        UpdateErrorsForElement(element);
    }

    private void HandleElementReloaded(ElementSave element)
    {
        UpdateErrorsForElement(element);
    }

    private void HandleElementImported(ElementSave element)
    {
        UpdateErrorsForElement(element);
    }

    private void HandleElementSelected(ElementSave? element)
    {
        UpdateErrorsForElement(element);
    }

    private void UpdateErrorsForElement(ElementSave? element)
    {
        _viewModel.Errors.Clear();

        // Nothing to check before a project is loaded.
        if (_projectState.GumProjectSave is not { } project)
        {
            return;
        }

        var errors = _errorChecker.GetErrorsFor(element, project);

        foreach (var item in errors)
        {
            _viewModel.Errors.Add(item);
        }
        foreach (var item in _projectErrors)
        {
            _viewModel.Errors.Add(item);
        }
        foreach (var item in _errorChecker.GetPluginErrors())
        {
            _viewModel.Errors.Add(item);
        }
    }
}
