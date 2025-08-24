using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
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

namespace Gum.Plugins.InternalPlugins.TreeView;

[Export(typeof(PluginBase))]
internal class MainTreeViewPlugin : InternalPlugin
{
    private readonly ISelectedState _selectedState;
    private readonly ElementTreeViewManager _elementTreeViewManager;
    
    public MainTreeViewPlugin()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _elementTreeViewManager = ElementTreeViewManager.Self;
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

        this.RefreshElementTreeView += HandleRefreshElementTreeView;

        this.BehaviorCreated += HandleBehaviorCreated;

        this.ProjectLoad += HandleProjectLoad;

        this.GetIfShouldSuppressRemoveEditorHighlight += HandleGetIfShouldSuppressRemoveEditorHighlight;

        this.FocusSearch += HandleFocusSearch;

        this.GetTreeNodeOver += HandleGetTreeNodeOver;
        this.GetSelectedNodes += HandleGetSelectedNodes;
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

    private bool HandleGetIfShouldSuppressRemoveEditorHighlight()
    {
        // If the mouse is over the element tree view, we don't want to force unhlighlights since they can highlight when over the tree view items
        return _elementTreeViewManager.HasMouseOver;
    }

    private void HandleInstanceAdd(ElementSave save1, InstanceSave save2)
    {
        _elementTreeViewManager.RefreshUi();
    }

    private void HandleBehaviorDeleted(BehaviorSave save)
    {
        _elementTreeViewManager.RefreshUi();
    }

    private void HandleProjectLoad(GumProjectSave save)
    {
        _elementTreeViewManager.RefreshUi();
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
}
