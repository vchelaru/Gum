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
using Gum.Messages;
using Gum.Services;
using RenderingLibrary;

namespace Gum.Plugins.InternalPlugins.TreeView;

[Export(typeof(PluginBase))]
internal class MainTreeViewPlugin : PriorityPlugin, IRecipient<ApplicationTeardownMessage>, IRecipient<UiBaseFontSizeChangedMessage>, IRecipient<RequestErrorRefreshMessage>, IRecipient<StandardsPaletteSettingChangedMessage>
{
    private readonly ISelectedState _selectedState;
    private readonly ElementTreeViewManager _elementTreeViewManager;
    private readonly IUserProjectSettingsManager _userProjectSettingsManager;
    private readonly ITreeViewStateService _treeViewStateService;
    private readonly IMessenger _messenger;
    private readonly IErrorChecker _errorChecker;
    private readonly IProjectState _projectState;

    [ImportingConstructor]
    public MainTreeViewPlugin(
        ISelectedState selectedState,
        ElementTreeViewManager elementTreeViewManager,
        IUserProjectSettingsManager userProjectSettingsManager,
        IMessenger messenger,
        IOutputManager outputManager,
        IErrorChecker errorChecker,
        IProjectState projectState)
    {
        _selectedState = selectedState;
        _elementTreeViewManager = elementTreeViewManager;
        _userProjectSettingsManager = userProjectSettingsManager;
        _messenger = messenger;

        // Create plugin-specific service with required dependencies
        _treeViewStateService = new TreeViewStateService(_userProjectSettingsManager, outputManager);

        _errorChecker = errorChecker;
        _projectState = projectState;

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

        this.VariableSet += HandleVariableSet;
        this.VariableRemovedFromCategory += HandleVariableRemovedFromCategory;
        this.HighlightTreeNode += HandleHighlightTreeNode;
        this.AfterUndo += HandleAfterUndo;
        this.InstanceDelete += HandleInstanceDelete;
        this.StateAdd += HandleStateAdd;
        this.StateDelete += HandleStateDelete;
        this.CategoryDelete += HandleCategoryDelete;
        this.BehaviorReferencesChanged += HandleBehaviorReferencesChanged;
        this.BehaviorInstanceAdd += HandleBehaviorInstanceAdd;
        this.BehaviorInstanceDelete += HandleBehaviorInstanceDelete;
        this.BehaviorInstanceRename += HandleBehaviorInstanceRename;
        this.ElementImported += HandleElementImported;
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

        // Repopulate the Standards chip palette for the newly-loaded project's standard types.
        _elementTreeViewManager.ApplyStandardsPaletteMode();
    }

    private void HandleElementAdd(ElementSave save)
    {
        _elementTreeViewManager.RefreshUi();
        RefreshErrorIndicatorsForAllElements();
    }

    private void HandleElementImported(ElementSave save)
    {
        _elementTreeViewManager.RefreshUi();
        RefreshErrorIndicatorsForAllElements();
    }

    private void HandleBehaviorCreated(BehaviorSave save)
    {
        _elementTreeViewManager.RefreshUi();
    }

    private void HandleElementDeleted(ElementSave save)
    {
        _elementTreeViewManager.RefreshUi();
        RefreshErrorIndicatorsForAllElements();
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
            _elementTreeViewManager.SuppressCallAfterClickSelect = true;
            try
            {
                _elementTreeViewManager.Select(save);
            }
            finally
            {
                _elementTreeViewManager.SuppressCallAfterClickSelect = false;
            }
        }
    }

    private void HandleElementSelected(ElementSave save)
    {
        _elementTreeViewManager.HighlightStandardInPalette(save);

        _elementTreeViewManager.SuppressCallAfterClickSelect = true;
        try
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
        finally
        {
            _elementTreeViewManager.SuppressCallAfterClickSelect = false;
        }
    }

    private void MainTreeViewPlugin_InstanceSelected(DataTypes.ElementSave element, DataTypes.InstanceSave instance)
    {
        // Selecting an instance means a standard's defaults are no longer the edit target.
        if (instance != null)
        {
            _elementTreeViewManager.HighlightStandardInPalette(null);
        }

        if(element != null || instance != null)
        {
            // The selection already happened and plugin events already fired.
            // We just need to sync the tree view's visual state — don't re-fire
            // CallAfterClickSelect which would cause a redundant plugin cascade.
            _elementTreeViewManager.SuppressCallAfterClickSelect = true;
            try
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
            finally
            {
                _elementTreeViewManager.SuppressCallAfterClickSelect = false;
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

    void IRecipient<StandardsPaletteSettingChangedMessage>.Receive(StandardsPaletteSettingChangedMessage message)
    {
        _elementTreeViewManager.ApplyStandardsPaletteMode();
    }

    void IRecipient<RequestErrorRefreshMessage>.Receive(RequestErrorRefreshMessage message)
    {
        // A full error refresh (RequestingPlugin == null) is requested after an edit changes an
        // element's error set but fires no structural plugin event — notably an animation keyframe
        // edit (MainStateAnimationPlugin.HandleDataChange), which only saves the .ganx. Re-check the
        // selected element's "!" indicator (detection itself is selection-independent and headless;
        // this is purely the refresh trigger). Plugin-scoped requests (RequestingPlugin != null,
        // sent on view-model refresh / selection) are ignored so selection is NOT a refresh trigger.
        if (message.RequestingPlugin == null)
        {
            RefreshErrorIndicatorsForElement(_selectedState.SelectedElement);
        }
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

    private void HandleVariableSet(ElementSave element, InstanceSave? instance, string variableName, object? oldValue)
    {
        RefreshErrorIndicatorsForElement(element);

        if(instance != null && variableName == nameof(instance.Locked))
        {
            _elementTreeViewManager.RefreshUi(instance);
        }
    }

    private void HandleVariableRemovedFromCategory(string variableName, StateSaveCategory category)
    {
        // Removing a variable from a category's states can clear an error (e.g. a GUM0003
        // self-referential category state), so the "!" tree indicator must refresh. The
        // category belongs to the currently selected element.
        RefreshErrorIndicatorsForElement(_selectedState.SelectedElement);
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

    private void HandleBehaviorReferencesChanged(ElementSave elementSave)
    {
        RefreshErrorIndicatorsForElement(elementSave);
    }

    private void HandleBehaviorInstanceAdd(BehaviorSave behavior, BehaviorInstanceSave instance)
    {
        RefreshErrorIndicatorsForElementsReferencingBehavior(behavior);
    }

    private void HandleBehaviorInstanceDelete(BehaviorSave behavior, BehaviorInstanceSave instance)
    {
        RefreshErrorIndicatorsForElementsReferencingBehavior(behavior);
    }

    private void HandleBehaviorInstanceRename(BehaviorSave behavior, BehaviorInstanceSave instance)
    {
        RefreshErrorIndicatorsForElementsReferencingBehavior(behavior);
    }

    private void RefreshErrorIndicatorsForElementsReferencingBehavior(BehaviorSave behavior)
    {
        var project = _projectState.GumProjectSave;
        if (project == null) return;
        var referencingElements = project.Components
            .Where(c => c.Behaviors.Any(b => b.BehaviorName == behavior.Name));
        foreach (var element in referencingElements)
        {
            RefreshErrorIndicatorsForElement(element);
        }
    }

    private void SaveTreeViewState()
    {
        _treeViewStateService.CaptureAndSaveState(_elementTreeViewManager.ObjectTreeView);
        _userProjectSettingsManager.Save();
    }
}
