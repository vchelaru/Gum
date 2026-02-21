using CommunityToolkit.Mvvm.Messaging;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using Gum.Wireframe;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.DataTypes.Variables;
using Gum.Services;
using RenderingLibrary;

namespace Gum.Plugins.InternalPlugins.TreeView;

[Export(typeof(PluginBase))]
internal class MainTreeViewPlugin : InternalPlugin, IRecipient<ApplicationTeardownMessage>, IRecipient<UiBaseFontSizeChangedMessage>
{
    private readonly ISelectedState _selectedState;
    private readonly ElementTreeViewManager _elementTreeViewManager;
    private readonly IUserProjectSettingsManager _userProjectSettingsManager;
    private readonly ITreeViewStateService _treeViewStateService;
    private readonly IMessenger _messenger;
    private readonly IErrorChecker _errorChecker;
    private readonly IProjectState _projectState;

    public MainTreeViewPlugin()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _elementTreeViewManager = ElementTreeViewManager.Self;
        _userProjectSettingsManager = Locator.GetRequiredService<IUserProjectSettingsManager>();
        _messenger = Locator.GetRequiredService<IMessenger>();

        // Create plugin-specific service with required dependencies
        var outputManager = Locator.GetRequiredService<IOutputManager>();
        _treeViewStateService = new TreeViewStateService(_userProjectSettingsManager, outputManager);

        _errorChecker = Locator.GetRequiredService<IErrorChecker>();
        _projectState = Locator.GetRequiredService<IProjectState>();

        // Register to receive ApplicationTeardownMessage
        _messenger.RegisterAll(this);
    }
    
    public override void StartUp()
    {
        AssignEvents();
    }

    private void AssignEvents()
    {
        this.InstanceSelected += MainTreeViewPlugin_InstanceSelected;
        this.InstanceAdd += HandleInstanceAdd;

        this.CategoryAdd += HandleCategoryAdd;

        this.BehaviorSelected += HandleBehaviorSelected;
        this.BehaviorDeleted += HandleBehaviorDeleted;

        this.ElementSelected += HandleElementSelected;
        this.ElementDelete += HandleElementDeleted;
        this.ElementAdd += HandleElementAdd;
        this.ElementDuplicate += HandleElementDuplicate;
        this.ElementReloaded += HandleElementReloaded;

        this.RefreshElementTreeView += HandleRefreshElementTreeView;

        this.BehaviorCreated += HandleBehaviorCreated;

        this.ProjectLoad += HandleProjectLoad;

        this.GetIfShouldSuppressRemoveEditorHighlight += HandleGetIfShouldSuppressRemoveEditorHighlight;

        this.FocusSearch += HandleFocusSearch;

        this.GetTreeNodeOver += HandleGetTreeNodeOver;
        this.GetSelectedNodes += HandleGetSelectedNodes;

        this.HighlightTreeNode += HandleHighlightTreeNode;

        this.VariableSet += HandleVariableSetForErrors;
        this.AfterUndo += HandleAfterUndo;
        this.InstanceDelete += HandleInstanceDelete;
        this.StateAdd += HandleStateAdd;
        this.StateDelete += HandleStateDelete;
        this.CategoryDelete += HandleCategoryDelete;
    }

    private void HandleElementReloaded(ElementSave save)
    {
        RefreshErrorIndicatorsForElement(save);
    }

    private IEnumerable<ITreeNode> HandleGetSelectedNodes()
    {
        return _elementTreeViewManager.SelectedNodes;

    }

    private ITreeNode? HandleGetTreeNodeOver()
    {
        return _elementTreeViewManager.GetTreeNodeOver();
    }

    private void HandleCategoryAdd(StateSaveCategory category)
    {
        _elementTreeViewManager.RefreshUi(_selectedState.SelectedStateContainer);
        RefreshErrorIndicatorsForElement(_selectedState.SelectedElement);
    }

    private void HandleFocusSearch()
    {
        _elementTreeViewManager.FocusSearch();
    }

    private void HandleRefreshElementTreeView(IInstanceContainer? instanceContainer = null)
    {
        if(instanceContainer != null)
        {
            _elementTreeViewManager.RefreshUi(instanceContainer);
        }
        else
        {
            _elementTreeViewManager.RefreshUi();
        }
    }

    private void HandleElementDuplicate(ElementSave save1, ElementSave save2)
    {
        _elementTreeViewManager.RefreshUi();
    }

    private void HandleHighlightTreeNode(IPositionedSizedObject? ipso)
    {
        // If the mouse is over the treeview, it manages its own hot node via MouseMove - skip to prevent loop
        if (_elementTreeViewManager.HasMouseOver)
        {
            return;
        }

        _elementTreeViewManager.HighlightTreeNodeForIpso(ipso);
    }

    private bool HandleGetIfShouldSuppressRemoveEditorHighlight()
    {
        // If the mouse is over the element tree view, we don't want to force unhlighlights since they can highlight when over the tree view items
        return _elementTreeViewManager.HasMouseOver;
    }

    private void HandleInstanceAdd(ElementSave save1, InstanceSave save2)
    {
        _elementTreeViewManager.RefreshUi();
        RefreshErrorIndicatorsForElement(save1);
    }

    private void HandleBehaviorDeleted(BehaviorSave save)
    {
        _elementTreeViewManager.RefreshUi();
    }

    private void HandleProjectLoad(GumProjectSave save)
    {
        _elementTreeViewManager.RefreshUi();

        // Load user settings and apply tree view state
        _userProjectSettingsManager.LoadForProject(save.FullFileName);
        _treeViewStateService.LoadAndApplyState(_elementTreeViewManager.ObjectTreeView);
        RefreshErrorIndicatorsForAllElements();
    }

    private void HandleElementAdd(ElementSave save)
    {
        _elementTreeViewManager.RefreshUi();
    }

    private void HandleBehaviorCreated(BehaviorSave save)
    {
        _elementTreeViewManager.RefreshUi();
    }

    private void HandleElementDeleted(ElementSave save)
    {
        _elementTreeViewManager.RefreshUi();
    }

    private void HandleBehaviorSelected(BehaviorSave save)
    {
        if(save != null)
        {
            // If an instance within this behavior is already selected (e.g. the BehaviorSelected
            // event was fired directly by undo/redo logic rather than from a user click), preserve
            // the instance selection. In the normal click flow, HandleBehaviorsSelected clears
            // SelectedInstance before firing this event, so the check is a no-op in that case.
            if (_selectedState.SelectedInstance != null && _selectedState.SelectedBehavior == save)
            {
                return;
            }
            _elementTreeViewManager.Select(save);
        }
    }

    private void HandleElementSelected(ElementSave save)
    {
        if(save != null)
        {
            _elementTreeViewManager.Select(save);
        }
        else if(save == null && _elementTreeViewManager.SelectedNode?.Tag is ElementSave)
        {
            _elementTreeViewManager.SelectedNode = null;
        }
    }

    private void MainTreeViewPlugin_InstanceSelected(DataTypes.ElementSave element, DataTypes.InstanceSave instance)
    {
        if(element != null || instance != null)
        {
            if(instance != null)
            {

                _elementTreeViewManager.Select(_selectedState.SelectedInstances);
            }

            if(instance == null && element != null)
            {
                _elementTreeViewManager.Select(element);
            }
        }

    }

    void IRecipient<ApplicationTeardownMessage>.Receive(ApplicationTeardownMessage message)
    {
        message.OnTearDown(SaveTreeViewState);
    }

    void IRecipient<UiBaseFontSizeChangedMessage>.Receive(UiBaseFontSizeChangedMessage message)
    {
        _elementTreeViewManager.UpdateCollapseButtonSizes(message.Size);
    }

    private void RefreshErrorIndicatorsForElement(ElementSave? element)
    {
        if (element == null) return;
        var project = _projectState.GumProjectSave;
        if (project == null) return;
        bool hasErrors = _errorChecker.GetErrorsFor(element, project).Length > 0;
        _elementTreeViewManager.UpdateErrorIndicatorsForElement(element, hasErrors);
    }

    private void RefreshErrorIndicatorsForAllElements()
    {
        var project = _projectState.GumProjectSave;
        if (project == null) return;
        var allElements = project.Screens.Cast<ElementSave>()
            .Concat(project.Components)
            .Concat(project.StandardElements);
        foreach (var element in allElements)
        {
            bool hasErrors = _errorChecker.GetErrorsFor(element, project).Length > 0;
            _elementTreeViewManager.UpdateErrorIndicatorsForElement(element, hasErrors);
        }
    }

    private void HandleVariableSetForErrors(ElementSave element, InstanceSave? instance, string variableName, object? oldValue)
    {
        RefreshErrorIndicatorsForElement(element);
    }

    private void HandleAfterUndo()
    {
        if(_selectedState.SelectedBehavior != null)
        {
            _elementTreeViewManager.RefreshUi((IInstanceContainer)_selectedState.SelectedBehavior);
        }
        if(_selectedState.SelectedElement != null)
        {
            RefreshErrorIndicatorsForElement(_selectedState.SelectedElement);
        }
    }

    private void HandleInstanceDelete(ElementSave element, InstanceSave instance)
    {
        _elementTreeViewManager.RefreshUi();
        RefreshErrorIndicatorsForElement(element);
    }

    private void HandleStateAdd(StateSave state)
    {
        RefreshErrorIndicatorsForElement(_selectedState.SelectedElement);
    }

    private void HandleStateDelete(StateSave state)
    {
        RefreshErrorIndicatorsForElement(_selectedState.SelectedElement);
    }

    private void HandleCategoryDelete(StateSaveCategory category)
    {
        _elementTreeViewManager.RefreshUi();
        RefreshErrorIndicatorsForElement(_selectedState.SelectedElement);
    }

    private void SaveTreeViewState()
    {
        _treeViewStateService.CaptureAndSaveState(_elementTreeViewManager.ObjectTreeView);
        _userProjectSettingsManager.Save();
    }
}
