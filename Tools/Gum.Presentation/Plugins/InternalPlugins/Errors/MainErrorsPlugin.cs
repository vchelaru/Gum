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
    private AllErrorsViewModel _viewModel = null!;

    #endregion

    [ImportingConstructor]
    public MainErrorsPlugin(IErrorChecker errorChecker, IMessenger messenger, ISelectedState selectedState,
        IClipboardService clipboardService, IFileSystemRevealService fileSystemRevealService, IProjectState projectState)
    {
        _errorChecker = errorChecker;
        _messenger = messenger;
        _selectedState = selectedState;
        _clipboardService = clipboardService;
        _fileSystemRevealService = fileSystemRevealService;
        _projectState = projectState;
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
        var element = _selectedState.SelectedElement;

        /////////////////////Early Out/////////////////////
        if(element == null)
        {
            return;
        }
        ///////////////////End Early Out///////////////////

        if (message.RequestingPlugin != null)
        {
            _viewModel.Errors.RemoveAll(item => item.OwnerPlugin == message.RequestingPlugin);

            var errors = _errorChecker.GetErrorsFor(element, message.RequestingPlugin);

            _viewModel.Errors.AddRange(errors);
        }
        else
        {
            UpdateErrorsForElement(element);
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
    }

    private void HandleVariableRemovedFromCategory(string variableName, StateSaveCategory category)
    {
        // Removing a variable from a category's states can clear an error (e.g. a GUM0003
        // self-referential category state), so the Errors tab must refresh. The category
        // belongs to the currently selected element.
        UpdateErrorsForElement(_selectedState.SelectedElement);
    }

    private void HandleInstanceSelected(ElementSave element, InstanceSave instance)
    {
        UpdateErrorsForElement(element);
    }

    private void HandleBehaviorReferencesChanged(ElementSave element)
    {
        UpdateErrorsForElement(element);
    }

    private void HandleVariableSet(ElementSave element, InstanceSave? instance, string variableName, object? oldValue)
    {
        UpdateErrorsForElement(element);
    }

    private void HandleInstanceDelete(ElementSave element, InstanceSave instance)
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
    }
}
